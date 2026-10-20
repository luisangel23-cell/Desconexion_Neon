using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement; // ¡NUEVO! Librería necesaria para cambiar de escena

public class ControladorCinematica : MonoBehaviour
{
    [Header("Cinemática y Cámaras")]
    public GameObject objetoTimeline;              // Tu objeto "Camera" que tiene la Timeline
    public PlayableDirector timelineDirector;      // El Playable Director dentro de ese objeto
    public GameObject objetoJugadorPrimeraPersona; // Tu "First Person Controller"

    [Header("Elementos a Mostrar en la Cinematica")]
    public GameObject[] npcsYPortal;               // NPCs y portal

    [Header("Transición de Escena")]
    [Tooltip("Nombre exacto de la escena a la que quieres ir al terminar la cinemática.")]
    public string nombreEscenaSiguiente = "NombreDeTuEscenaSiguiente";

    private bool yaSeReprodujo = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!yaSeReprodujo && other.CompareTag("Player"))
        {
            yaSeReprodujo = true;
            StartCoroutine(EjecutarCinematica());
        }
    }

    System.Collections.IEnumerator EjecutarCinematica()
    {
        // 1. Apagamos el control y la cámara de primera persona del jugador
        if (objetoJugadorPrimeraPersona != null) 
            objetoJugadorPrimeraPersona.SetActive(false);

        // 2. ¡APAGAMOS TODOS LOS AUDIOS DE LA ESCENA QUE ESTÉN SONANDO!
        AudioSource[] todosLosAudios = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (AudioSource audioSource in todosLosAudios)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        // 3. Encendemos el objeto de la Timeline (cámaras)
        if (objetoTimeline != null)
        {
            objetoTimeline.SetActive(true);
        }

        // 4. Encendemos los NPCs y el portal
        foreach (GameObject obj in npcsYPortal)
        {
            if (obj != null)
                obj.SetActive(true);
        }

        // 5. Esperamos un fotograma para que Unity despierte todo bien
        yield return null; 

        // 6. Reproducimos la cinemática
        if (timelineDirector != null)
        {
            timelineDirector.Play();
            
            // Esperamos exactamente lo que dura la Timeline
            yield return new WaitForSeconds((float)timelineDirector.duration);
        }
        else
        {
            yield return new WaitForSeconds(5f);
        }

        // 7. ¡TERMINÓ EL TIMELINE! Cambiamos de escena directamente
        if (!string.IsNullOrEmpty(nombreEscenaSiguiente))
        {
            SceneManager.LoadScene(nombreEscenaSiguiente);
        }
        else
        {
            Debug.LogWarning("[ControladorCinematica] No se especificó el nombre de la escena siguiente en el Inspector.");
        }
    }
}