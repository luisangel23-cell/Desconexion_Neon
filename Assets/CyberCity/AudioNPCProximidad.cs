using UnityEngine;

public class AudioNPCProximidad : MonoBehaviour
{
    [Header("Configuración de Audio 3D")]
    public AudioSource audioSource3D; 
    public AudioClip audioProximidad; 

    void Start()
    {
        if (audioSource3D != null)
        {
            // Asignamos las propiedades de forma segura dentro de Start
            audioSource3D.spatialBlend = 1.0f; 
            audioSource3D.playOnAwake = false;
            audioSource3D.clip = audioProximidad;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (audioSource3D != null && audioProximidad != null && !audioSource3D.isPlaying)
            {
                audioSource3D.Play();
                Debug.Log("[AUDIO 3D] ¡Jugador cerca del NPC nuevo, reproduciendo audio espacial!");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (audioSource3D != null && audioSource3D.isPlaying)
            {
                audioSource3D.Stop();
                Debug.Log("[AUDIO 3D] ¡Jugador se alejó, deteniendo audio!");
            }
        }
    }
}