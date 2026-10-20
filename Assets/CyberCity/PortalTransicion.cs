using UnityEngine;
using UnityEngine.SceneManagement; 

public class PortalTransicion : MonoBehaviour
{
    [Header("Configuración del Portal")]
    [Tooltip("Escribe el nombre exacto de la escena a la que vas a viajar")]
    public string nombreSiguienteEscena; 

    [Header("Audio a Detener")]
    [Tooltip("Arrastra aquí el objeto que tiene la música de fondo")]
    public AudioSource musicaFondo; 

    private bool portalActivado = false;

    private void OnTriggerEnter(Collider other)
    {
        // Detecta si el que entró al portal es el jugador (debe tener el Tag "Player")
        if (other.CompareTag("Player") && !portalActivado)
        {
            portalActivado = true;
            Debug.Log("[PORTAL] Jugador detectado. Apagando música y viajando...");

            // 1. Detener la música
            if (musicaFondo != null)
            {
                musicaFondo.Stop();
            }

            // 2. Cargar la siguiente escena
            if (!string.IsNullOrEmpty(nombreSiguienteEscena))
            {
                SceneManager.LoadScene(nombreSiguienteEscena);
            }
            else
            {
                Debug.LogError("[ERROR] No has escrito el nombre de la siguiente escena en el Inspector.");
            }
        }
    }
}