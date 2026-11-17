using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class ControladorFinPelea : MonoBehaviour
{
    public static ControladorFinPelea Instancia;

    [Header("Elementos de UI")]
    public GameObject panelFinJuego;
    public Text textoResultado;
    public Button botonReiniciar;

    [Header("Audios de Fin de Partida")]
    public AudioClip sonidoVictoria;
    public AudioClip sonidoDerrota;

    private AudioSource audioSource;
    private bool combateTerminado = false;

    void Awake()
    {
        Instancia = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null) audioSource.playOnAwake = false;

        // Buscador automático por si se olvida arrastrar en el Inspector
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

            if (botonReiniciar == null)
                botonReiniciar = panelFinJuego.GetComponentInChildren<Button>(true);

            panelFinJuego.SetActive(false);
        }

        if (botonReiniciar != null)
        {
            botonReiniciar.onClick.RemoveAllListeners();
            botonReiniciar.onClick.AddListener(ReiniciarCombate);
        }
    }

    public void MostrarResultado(bool ganoJugador)
    {
        if (combateTerminado) return;
        combateTerminado = true;

        if (panelFinJuego != null)
        {
            panelFinJuego.SetActive(true);
        }

        if (textoResultado != null)
        {
            if (ganoJugador)
            {
                textoResultado.text = "¡VICTORIA!";
                textoResultado.color = Color.green; // O el color neón que prefieras
            }
            else
            {
                textoResultado.text = "DERROTA";
                textoResultado.color = Color.red;
            }
        }

        if (audioSource != null)
        {
            AudioClip clip = ganoJugador ? sonidoVictoria : sonidoDerrota;
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }

    public void ReiniciarCombate()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}