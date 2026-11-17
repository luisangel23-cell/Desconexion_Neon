using UnityEngine;
using UnityEngine.Playables;

public class MoverAlFinalizarTimeline : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El PlayableDirector que contiene el Timeline")]
    public PlayableDirector directorTimeline;

    [Tooltip("El personaje que se moverá (Ch45_nonPBR o First Person Controller)")]
    public GameObject personaje;

    [Tooltip("El punto vacío hacia donde se moverá")]
    public Transform puntoDestino;

    private void OnEnable()
    {
        if (directorTimeline != null)
        {
            // Nos suscribimos al evento de cuando finaliza el Timeline
            directorTimeline.stopped += OnTimelineFinalizado;
        }
    }

    private void OnDisable()
    {
        if (directorTimeline != null)
        {
            directorTimeline.stopped -= OnTimelineFinalizado;
        }
    }

    private void OnTimelineFinalizado(PlayableDirector director)
    {
        if (director == directorTimeline && personaje != null && puntoDestino != null)
        {
            // Si el personaje tiene CharacterController, debemos desactivarlo un milisegundo para permitir la teletransportación
            CharacterController controller = personaje.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            // Cambiamos posición y rotación
            personaje.transform.position = puntoDestino.position;
            personaje.transform.rotation = puntoDestino.rotation;

            if (controller != null)
            {
                controller.enabled = true;
            }

            Debug.Log("Timeline finalizado: Personaje movido con éxito.");
        }
    }
}