using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class ControladorNodoUI : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public string idNodo;
    [HideInInspector] public int toxicidad;
    [HideInInspector] public string estado;
    [HideInInspector] public bool encriptado;

    private TextMeshProUGUI textoUI;
    private Image panelFondo;
    private bool estaDesencriptando = false;

    public static GameObject panelRotacionGlobal;
    public static TextMeshProUGUI txtTemporizadorGlobal;
    public static ControladorNodoUI nodoSeleccionadoActual;

    private Coroutine rutinaTemporizadorPropia; 

    void Awake()
    {
        textoUI = GetComponentInChildren<TextMeshProUGUI>();
        panelFondo = GetComponent<Image>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        nodoSeleccionadoActual = this;

        if (encriptado && !estaDesencriptando)
        {
            StartCoroutine(DesencriptarNodoRoutine());
            return;
        }

        if (encriptado) return;

        if (nodoSeleccionadoActual != null && nodoSeleccionadoActual != this)
        {
            nodoSeleccionadoActual.DetenerTemporizador();
        }

        AbrirPanelRotacion();
    }

    public void DetenerTemporizador()
    {
        if (rutinaTemporizadorPropia != null)
        {
            StopCoroutine(rutinaTemporizadorPropia);
            rutinaTemporizadorPropia = null;
        }
    }

    IEnumerator DesencriptarNodoRoutine()
    {
        estaDesencriptando = true;
        float tiempoRestante = 3.0f;

        while (tiempoRestante > 0)
        {
            if (textoUI != null)
            {
                textoUI.text = $"DESCIFRANDO...\n{tiempoRestante:F1}s";
            }
            yield return new WaitForSeconds(0.1f);
            tiempoRestante -= 0.1f;
        }

        encriptado = false;
        estaDesencriptando = false;
        
        if (textoUI != null)
        {
            textoUI.text = "Tox: " + toxicidad + "\nID: " + idNodo;
        }
    }

    void AbrirPanelRotacion()
    {
        if (panelRotacionGlobal != null)
        {
            panelRotacionGlobal.SetActive(true);
            DetenerTemporizador();
            rutinaTemporizadorPropia = StartCoroutine(TemporizadorRotacionRoutine());
        }
    }

    IEnumerator TemporizadorRotacionRoutine()
    {
        float tiempo = 10.0f;
        while (tiempo > 0)
        {
            yield return new WaitForSeconds(0.1f);
            tiempo -= 0.1f;
        }

        CerrarPanelRotacionInmediato();
    }

    public static void IntentarRotacion(string tipoRotacionRequerida)
    {
        if (nodoSeleccionadoActual == null) return;

        string mensajeTCP = "RESOLVER_ROTACION:" + nodoSeleccionadoActual.idNodo + ":" + tipoRotacionRequerida;
        
        if (ClienteRed.instancia != null)
        {
            ClienteRed.instancia.EnviarComando(mensajeTCP);
        }

        // Si acierta al botón correcto de rotación
        if (tipoRotacionRequerida == "LL" || tipoRotacionRequerida == "RR" || tipoRotacionRequerida == "SIMPLE_DERECHA")
        {
            if (ClienteRed.instancia != null)
            {
                ClienteRed.instancia.StartCoroutine(CerrarPanelYLiberarMouseSeguro());
            }
            else
            {
                if (panelRotacionGlobal != null) panelRotacionGlobal.SetActive(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        else
        {
            if (nodoSeleccionadoActual != null)
            {
                nodoSeleccionadoActual.DetenerTemporizador();
            }
        }
    }

private static IEnumerator CerrarPanelYLiberarMouseSeguro()
    {
        yield return new WaitForSeconds(3.0f);

        // 1. Cerramos el panel de rotación
        if (panelRotacionGlobal != null)
        {
            panelRotacionGlobal.SetActive(false);
        }

        // 2. Apagamos la imagen de fondo (FondoPantalla)
        GameObject fondo = GameObject.Find("FondoPantalla");
        if (fondo != null)
        {
            fondo.SetActive(false);
        }

        // 3. Destruimos absolutamente todos los nodos y líneas del árbol en pantalla
        foreach (GameObject obj in Object.FindObjectsOfType<GameObject>())
        {
            if (obj.name.StartsWith("Nodo_") || obj.name.StartsWith("Linea_"))
            {
                Object.Destroy(obj);
            }
        }
        
        // 4. ¡REPRODUCIMOS TU AUDIO DE VICTORIA PREPARADO!
        // Buscamos al NPC en la escena para usar su AudioSource y su voz de éxito
        DialogoNPC npc = Object.FindObjectOfType<DialogoNPC>();
        if (npc != null)
        {
            npc.ReproducirFraseVictoria();
        }

        // 5. Bloqueamos y ocultamos el mouse otra vez
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Debug.Log("[JUEGO] ¡Nivel superado! Todo apagado, frase reproducida y mouse bloqueado nuevamente.");

        if (nodoSeleccionadoActual != null)
        {
            nodoSeleccionadoActual.DetenerTemporizador();
            nodoSeleccionadoActual = null;
        }
    }
    static void CerrarPanelRotacionInmediato()
    {
        if (panelRotacionGlobal != null)
        {
            panelRotacionGlobal.SetActive(false);
        }
        
        if (nodoSeleccionadoActual != null)
        {
            nodoSeleccionadoActual.DetenerTemporizador();
            nodoSeleccionadoActual = null;
        }
    }
}