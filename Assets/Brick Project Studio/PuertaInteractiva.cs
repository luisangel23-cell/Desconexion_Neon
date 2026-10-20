using UnityEngine;
using TMPro; // Asegúrate de tener esto si usas TextMeshPro

public class PuertaInteractiva : MonoBehaviour
{
    [Header("UI de Interacción")]
    [Tooltip("Arrastra aquí el texto del Canvas que dice 'Presiona F para abrir'")]
    public GameObject textoInteraccion; 
    
    [Header("Opcional: Sonido al abrir")]
    public AudioClip sonidoAbrir;

    private bool jugadorCerca = false;

    void Update()
    {
        // Si el jugador está en la zona y presiona la F
        if (jugadorCerca && Input.GetKeyDown(KeyCode.F))
        {
            AbrirPuerta();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Cuando el jugador se acerca a la puerta, encendemos el texto
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
        // Si el jugador se aleja sin abrirla, apagamos el texto
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;
            if (textoInteraccion != null)
            {
                textoInteraccion.SetActive(false);
            }
        }
    }

    void AbrirPuerta()
    {
        // 1. Apagamos el texto de la pantalla para que no se quede pegado
        if (textoInteraccion != null)
        {
            textoInteraccion.SetActive(false);
        }

        // 2. Reproducimos el sonido en 2D (si le pusiste uno)
        if (sonidoAbrir != null)
        {
            GameObject tempAudio = new GameObject("SonidoPuerta2D");
            AudioSource src = tempAudio.AddComponent<AudioSource>();
            src.clip = sonidoAbrir;
            src.spatialBlend = 0f; // 100% 2D
            src.Play();
            Destroy(tempAudio, sonidoAbrir.length);
        }

        // 3. ¡Eliminamos la puerta de la escena!
        Destroy(gameObject);
    }
}