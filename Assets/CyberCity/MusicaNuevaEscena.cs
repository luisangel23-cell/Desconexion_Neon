using UnityEngine;

public class MusicaNuevaEscena : MonoBehaviour
{
    [Header("Audio para la Nueva Escena")]
    public AudioClip audioDeLaNuevaEscena;
    [Range(0f, 1f)] public float volumen = 1f;

    void Start()
    {
        // 1. Buscamos y destruimos CUALQUIER música vieja que haya venido de la escena pasada
        GameObject musicaGlobalVieja = GameObject.Find("MusicaFondoGlobal");
        if (musicaGlobalVieja != null)
        {
            Destroy(musicaGlobalVieja);
        }

        GameObject reproductorCartaViejo = GameObject.Find("AudioCarta2D");
        if (reproductorCartaViejo != null)
        {
            Destroy(reproductorCartaViejo);
        }

        // 2. Reproducimos el nuevo audio una sola vez (sin bucle)
        if (audioDeLaNuevaEscena != null)
        {
            GameObject objetoAudio = new GameObject("AudioReproductorNuevaEscena");
            AudioSource source = objetoAudio.AddComponent<AudioSource>();
            source.clip = audioDeLaNuevaEscena;
            source.spatialBlend = 0f; // 100% 2D
            source.volume = volumen;
            source.loop = false; // <-- Aquí quitamos el bucle para que suene solo una vez
            source.Play();
        }
    }
}