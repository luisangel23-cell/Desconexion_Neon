using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class ControladorFinMultijugador : MonoBehaviour
{
    public static ControladorFinMultijugador Instancia;

    [Header("Elementos de UI")]
    public GameObject panelFinJuego;
    public Text textoResultado;
    public Button botonReiniciar;
    public Button botonSeleccionarPeleadores;

    [Header("Escena de Selección")]
    public string nombreEscenaSeleccion = "SeleccionPersonajes";

    [Header("Audio de Victoria")]
    public AudioClip sonidoVictoria;

    private AudioSource audioSource;
    private bool combateTerminado = false;

    void Awake()
    {
        Instancia = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null) audioSource.playOnAwake = false;

        if (panelFinJuego == null)
        {
            Transform[] objs = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (Transform t in objs)
            {
                if (t.name == "Panel_FinJuego") { panelFinJuego = t.gameObject; break; }
            }
        }

        if (panelFinJuego != null)
        {
            if (textoResultado == null)
                textoResultado = panelFinJuego.GetComponentInChildren<Text>(true);

            Button[] botones = panelFinJuego.GetComponentsInChildren<Button>(true);
            foreach (Button btn in botones)
            {
                if (btn.name == "Boton_Reiniciar" && botonReiniciar == null)
                    botonReiniciar = btn;
                else if (btn.name == "Boton_SeleccionarPeleadores" && botonSeleccionarPeleadores == null)
                    botonSeleccionarPeleadores = btn;
            }

            panelFinJuego.SetActive(false);
        }

        if (botonReiniciar != null)
        {
            botonReiniciar.onClick.RemoveAllListeners();
            botonReiniciar.onClick.AddListener(ReiniciarCombate);
        }

        if (botonSeleccionarPeleadores != null)
        {
            botonSeleccionarPeleadores.onClick.RemoveAllListeners();
            botonSeleccionarPeleadores.onClick.AddListener(IrASeleccionPeleadores);
        }
    }

    public void MostrarGanador(int idGanador)
    {
        if (combateTerminado) return;
        combateTerminado = true;

        if (panelFinJuego != null) panelFinJuego.SetActive(true);

        if (textoResultado != null)
        {
            textoResultado.text = "¡JUGADOR " + idGanador + " GANA!";
            textoResultado.color = (idGanador == 1) ? Color.cyan : Color.yellow;
        }

        if (audioSource != null && sonidoVictoria != null)
        {
            audioSource.PlayOneShot(sonidoVictoria);
        }
    }

    public void ReiniciarCombate()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void IrASeleccionPeleadores()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nombreEscenaSeleccion);
    }
}