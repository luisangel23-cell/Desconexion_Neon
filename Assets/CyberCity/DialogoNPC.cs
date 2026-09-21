using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DialogoNPC : MonoBehaviour
{
    [Header("Referencias de UI")]
    public GameObject panelDialogo;    
    public Button botonOpcion1;        
    public Button botonOpcion2;        

    [Header("Audios del NPC")]
    public AudioSource audioSource;    
    public AudioClip vozBienvenida;    
    public AudioClip sonidoBoton;      
    public AudioClip vozVictoria;      

    [Header("Audio General del Mapa (Post-Victoria)")]
    public AudioSource audioSourceGlobal; 
    public AudioClip audioAdicional;      

    [Header("Barra de Estrés")]
    public Slider barraEstres;          

    [Header("Referencias del Árbol (Desconexión Neón)")]
    public GameObject fondoPantalla;    
    public GameObject contenedorArbol;  

    private Animator animNPC;
    private bool conversacionIniciada = false;
    
    [HideInInspector]
    public bool puedeIniciarDialogo = false;

    void Start()
    {
        animNPC = GetComponent<Animator>();

        if (panelDialogo != null)
        {
            panelDialogo.SetActive(false);
        }

        if (barraEstres != null)
        {
            barraEstres.gameObject.SetActive(false);
            barraEstres.value = 1f; 
        }

        if (fondoPantalla != null) fondoPantalla.SetActive(false);
        if (contenedorArbol != null) contenedorArbol.SetActive(false);

        if (botonOpcion1 != null) botonOpcion1.onClick.AddListener(Opcion1Elegida);
        if (botonOpcion2 != null) botonOpcion2.onClick.AddListener(Opcion2Elegida);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && puedeIniciarDialogo && !conversacionIniciada)
        {
            IniciarDialogo();
        }
    }

    public void HabilitarDialogoPorProximidad()
    {
        puedeIniciarDialogo = true;
        Debug.Log("¡Diálogo habilitado! Acércate al hombre de negro.");
    }

    public void IniciarDialogo()
    {
        if (conversacionIniciada) return;
        conversacionIniciada = true;

        if (animNPC != null)
        {
            animNPC.SetTrigger("Hablar");
        }

        if (audioSource != null && vozBienvenida != null && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(vozBienvenida);
        }

        if (panelDialogo != null)
        {
            panelDialogo.SetActive(true);
            if (botonOpcion1 != null) botonOpcion1.gameObject.SetActive(true);
            if (botonOpcion2 != null) botonOpcion2.gameObject.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Opcion1Elegida()
    {
        if (audioSource != null && sonidoBoton != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(sonidoBoton);
        }

        if (barraEstres != null)
        {
            barraEstres.gameObject.SetActive(true); 
            barraEstres.value -= 0.4f;              
        }

        OcultarBotones();
        StartCoroutine(CerrarDialogoYBarra(3f, false));
    }

    void Opcion2Elegida()
    {
        OcultarBotones();

        if (fondoPantalla != null) fondoPantalla.SetActive(true);
        if (contenedorArbol != null) contenedorArbol.SetActive(true);

        if (ClienteRed.instancia != null)
        {
            ClienteRed.instancia.IniciarConexionServidor();
        }

        StartCoroutine(CerrarDialogoYBarra(1f, true));
    }

    public void ReproducirFraseVictoria()
    {
        StartCoroutine(RutinaVictoriaYSalida());
    }

    IEnumerator RutinaVictoriaYSalida()
    {
        // 1. El NPC dice su frase de victoria
        if (audioSource != null && vozVictoria != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(vozVictoria);
            Debug.Log("[NPC] ¡Reproduciendo frase de victoria y marchándose!");

            yield return new WaitForSeconds(vozVictoria.length);
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }

        // 2. El NPC se va caminando hacia el punto de salida de inmediato
        MovimientoNPC movimiento = GetComponent<MovimientoNPC>();
        if (movimiento != null)
        {
            movimiento.IniciarCaminoSalida();
        }

        // 3. Contamos exactamente los 8 segundos
        Debug.Log("[MAPA] Empieza la cuenta de los 8 segundos...");
        yield return new WaitForSeconds(8f);
        Debug.Log("[MAPA] ¡Los 8 segundos han terminado! Intentando reproducir audio global...");

        // 4. Verificación de referencias y reproducción
        if (audioSourceGlobal != null && audioAdicional != null)
        {
            audioSourceGlobal.Stop();
            audioSourceGlobal.PlayOneShot(audioAdicional);
            Debug.Log("[MAPA] ¡Audio general reproducido con éxito por todo el mapa!");
        }
        else
        {
            if (audioSourceGlobal == null) Debug.LogError("[ERROR] ¡Falta arrastrar el AudioSourceGlobal en el Inspector del NPC!");
            if (audioAdicional == null) Debug.LogError("[ERROR] ¡Falta arrastrar el AudioClip 'Audio Adicional' en el Inspector del NPC!");
        }
    }

    void OcultarBotones()
    {
        if (botonOpcion1 != null) botonOpcion1.gameObject.SetActive(false);
        if (botonOpcion2 != null) botonOpcion2.gameObject.SetActive(false);
    }

    IEnumerator CerrarDialogoYBarra(float tiempoEspera, bool activarJuegoArbol)
    {
        if (panelDialogo != null)
        {
            panelDialogo.SetActive(false);
        }

        yield return new WaitForSeconds(tiempoEspera);

        if (barraEstres != null)
        {
            barraEstres.gameObject.SetActive(false); 
        }

        if (activarJuegoArbol)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("[JUEGO] Interfaz del árbol activada y red conectada con éxito.");
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            MovimientoNPC movimiento = GetComponent<MovimientoNPC>();
            if (movimiento != null)
            {
                movimiento.IniciarCaminoSalida();
            }
        }
    }
}