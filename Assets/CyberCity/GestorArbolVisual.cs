using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GestorArbolVisual : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject prefabNodo; 
    public GameObject prefabLinea; 
    public Transform contenedorCanvas; 
    
    [Header("Panel de Rotacion (ARRASTRA TU PANEL AQUI)")]
    public GameObject panelRotacion; 

    void Start()
    {
        // Conectamos el panel globalmente y lo ocultamos al arrancar el juego
        if (panelRotacion != null)
        {
            ControladorNodoUI.panelRotacionGlobal = panelRotacion;
            ControladorNodoUI.txtTemporizadorGlobal = panelRotacion.GetComponentInChildren<TextMeshProUGUI>(true);
            panelRotacion.SetActive(false);
        }
    }

    void OnEnable()
    {
        // Al activarse el contenedor del árbol (al presionar la opción 2 del NPC), 
        // le indicamos al cliente de red que comience la escucha activa si está disponible.
        if (ClienteRed.instancia != null)
        {
            // Verificamos si existe el método o simplemente permitimos la recepción
            Debug.Log("[JUEGO] Contenedor del árbol activado. Iniciando sincronización de red.");
        }
    }

    public void DibujarArbol(NeuronaDTO raiz)
    {
        if (raiz == null || string.IsNullOrEmpty(raiz.id)) return;

        // PROTECCIÓN ANTI-ERRORES: Si por alguna razón el contenedor quedó apuntando a un panel o botón, 
        // lo redirigimos automáticamente al Canvas principal de la escena.
        if (contenedorCanvas == null || contenedorCanvas.name.Contains("Btn") || contenedorCanvas.name.Contains("Panel"))
        {
            Canvas canvasPrincipal = FindObjectOfType<Canvas>();
            if (canvasPrincipal != null)
            {
                contenedorCanvas = canvasPrincipal.transform;
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        foreach (Transform hijo in contenedorCanvas)
        {
            if (hijo.name.StartsWith("Nodo_") || hijo.name.StartsWith("Linea_"))
            {
                Destroy(hijo.gameObject);
            }
        }

        float separacionInicialX = 250f;
        ConstruirNodoRecursivo(raiz, 0f, 150f, separacionInicialX, null);
    }

    void ConstruirNodoRecursivo(NeuronaDTO nodo, float x, float y, float separacionX, Vector2? posPadre)
    {
        if (nodo == null) return;

        float anchoMedioNodo = 95f;
        float limiteMaxX = (Screen.width / 2f) - anchoMedioNodo;
        if (limiteMaxX <= 100f) limiteMaxX = 750f; 

        float xRestringido = Mathf.Clamp(x, -limiteMaxX, limiteMaxX);
        Vector2 posActual = new Vector2(xRestringido, y);

        if (posPadre.HasValue && prefabLinea != null)
        {
            GameObject lineaObj = Instantiate(prefabLinea, contenedorCanvas);
            lineaObj.name = "Linea_" + nodo.id;
            lineaObj.transform.SetAsFirstSibling();
            RectTransform rectLinea = lineaObj.GetComponent<RectTransform>();
            rectLinea.anchoredPosition = posPadre.Value;
            Vector2 direccion = posActual - posPadre.Value;
            float distancia = direccion.magnitude;
            float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
            rectLinea.sizeDelta = new Vector2(distancia, 4f);
            rectLinea.rotation = Quaternion.Euler(0, 0, angulo);
        }

        GameObject objNodo = Instantiate(prefabNodo, contenedorCanvas);
        objNodo.name = "Nodo_" + nodo.id;
        
        RectTransform rect = objNodo.GetComponent<RectTransform>();
        rect.anchoredPosition = posActual;

        ControladorNodoUI controlador = objNodo.GetComponent<ControladorNodoUI>();
        if (controlador != null)
        {
            controlador.idNodo = nodo.id;
            controlador.toxicidad = nodo.toxicidad;
            controlador.estado = nodo.estado;
            controlador.encriptado = nodo.encriptado;
        }

        TextMeshProUGUI textoToxicidad = objNodo.GetComponentInChildren<TextMeshProUGUI>();
        Image panelFondo = objNodo.GetComponent<Image>();

        if (textoToxicidad != null)
        {
            if (nodo.encriptado)
                textoToxicidad.text = "🔒 [ENCRIPTADO]";
            else
                textoToxicidad.text = "Tox: " + nodo.toxicidad + "\nID: " + nodo.id;
        }

        if (panelFondo != null)
        {
            if (!string.IsNullOrEmpty(nodo.estado) && nodo.estado.Contains("PELIGRO"))
                panelFondo.color = Color.red; 
            else
                panelFondo.color = new Color(0.1f, 0.2f, 0.3f); 
        }

        float offsetY = 110f; 
        float nuevaSeparacionX = Mathf.Max(separacionX * 0.7f, 140f); 

        if (nodo.izq != null) ConstruirNodoRecursivo(nodo.izq, x - separacionX, y - offsetY, nuevaSeparacionX, posActual);
        if (nodo.der != null) ConstruirNodoRecursivo(nodo.der, x + separacionX, y - offsetY, nuevaSeparacionX, posActual);
    }
}