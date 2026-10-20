using UnityEngine;

public class AudioProximidadNPC : MonoBehaviour
{
    private AudioSource audioSource;

    void Start()
    {
        // Obtenemos el componente AudioSource que está en el mismo NPC
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Si el jugador entra al radio de proximidad y no está sonando, lo reproducimos
        if (other.CompareTag("Player"))
        {
            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Si el jugador se aleja, apagamos el audio
        if (other.CompareTag("Player"))
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }
}