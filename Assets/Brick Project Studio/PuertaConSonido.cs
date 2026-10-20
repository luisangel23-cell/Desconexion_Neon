using UnityEngine;

public class PuertaConSonido : MonoBehaviour
{
    [Header("UI de Interacción")]
    public GameObject textoInteraccion; 
    
    [Header("Sonido Exclusivo de esta Puerta (2D)")]
    public AudioClip sonidoEspecial;
    [Range(0f, 1f)] public float volumen = 1f;

    private bool jugadorCerca = false;

    void Update()
    {
        if (jugadorCerca && Input.GetKeyDown(KeyCode.F))
        {
            AbrirPuertaConSonido();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
            if (textoInteraccion != null)
            {
                textoInteraccion.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;
            if (textoInteraccion != null)
            {
                textoInteraccion.SetActive(false);
            }
        }
    }

    void AbrirPuertaConSonido()
    {
        if (textoInteraccion != null)
        {
            textoInteraccion.SetActive(false);
        }

        // Reproduce el sonido en 2D (se escucha igual en todas partes)
        if (sonidoEspecial != null)
        {
            GameObject objetoAudioTemp = new GameObject("AudioPuerta2D");
            AudioSource source = objetoAudioTemp.AddComponent<AudioSource>();
            source.clip = sonidoEspecial;
            source.spatialBlend = 0f; // 0f significa 100% 2D
            source.volume = volumen;
            source.Play();

            // Destruimos este objeto temporalmente cuando termine de sonar el audio para no gastar memoria
            Destroy(objetoAudioTemp, sonidoEspecial.length);
        }

        // Elimina solo esta puerta
        Destroy(gameObject);
    }
}