using UnityEngine;
using UnityEngine.Playables;

public class TransicionTimeline : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayableDirector directorTimeline;
    [SerializeField] private GameObject camarasCinematica; // Tu contenedor 'camch' o la cámara de la historia
    [SerializeField] private GameObject jugadorFPS;         // Tu First Person Controller

    private bool cinematicaTerminada = false;

    private void Awake()
    {
        // 1. FORZAMOS EL ESTADO INICIAL APENAS ARRANCA EL JUEGO
        if (jugadorFPS != null)
            jugadorFPS.SetActive(false); // Apagamos al jugador FPS de inmediato

        if (camarasCinematica != null)
            camarasCinematica.SetActive(true); // Encendemos la cinemática
    }

    private void Start()
    {
        if (directorTimeline != null)
        {
            directorTimeline.stopped += AlTerminarCinematica;
            directorTimeline.Play(); // Forzamos inicio del Timeline
        }
    }

    private void Update()
    {
        // Respaldo de seguridad: si el tiempo del Timeline llega al final
        if (!cinematicaTerminada && directorTimeline != null && directorTimeline.duration > 0)
        {
            if (directorTimeline.time >= directorTimeline.duration - 0.05f)
            {
                HacerTransicion();
            }
        }
    }

    private void AlTerminarCinematica(PlayableDirector pd)
    {
        HacerTransicion();
    }

    private void HacerTransicion()
    {
        if (cinematicaTerminada) return; // Evitamos que se ejecute dos veces
        cinematicaTerminada = true;

        // 1. Apagamos la cinemática y sus cámaras
        if (camarasCinematica != null)
            camarasCinematica.SetActive(false);

        // 2. Encendemos al First Person Controller
        if (jugadorFPS != null)
            jugadorFPS.SetActive(true);

        // 3. Capturamos y ocultamos el ratón para tomar el control del personaje
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDestroy()
    {
        if (directorTimeline != null)
            directorTimeline.stopped -= AlTerminarCinematica;
    }
}