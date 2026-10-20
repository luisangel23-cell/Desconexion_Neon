using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalTransicion2 : MonoBehaviour
{
    [Header("Configuración de Escena")]
    [Tooltip("Nombre exacto de la escena a la que quieres viajar")]
    [SerializeField] private string nombreEscenaSiguiente;

    [Header("Configuración de Sonido")]
    [SerializeField] private AudioSource audioSourcePortal;
    [Tooltip("Distancia en metros a la que empezará a sonar el portal")]
    [SerializeField] private float distanciaActivacionSonido = 8f;

    private Transform jugador;

    private void Start()
    {
        // Busca automáticamente al jugador por su Tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            jugador = playerObj.transform;
        }

        // Asegura que el audio esté configurado correctamente
        if (audioSourcePortal != null)
        {
            audioSourcePortal.loop = true;
            audioSourcePortal.playOnAwake = false;
        }
    }

    private void Update()
    {
        if (jugador == null || audioSourcePortal == null) return;

        // Calcula la distancia entre el jugador y el portal
        float distancia = Vector3.Distance(transform.position, jugador.position);

        if (distancia <= distanciaActivacionSonido)
        {
            if (!audioSourcePortal.isPlaying)
            {
                audioSourcePortal.Play();
            }
        }
        else
        {
            if (audioSourcePortal.isPlaying)
            {
                audioSourcePortal.Stop();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Cambia de escena cuando el jugador entra al centro del portal
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene(nombreEscenaSiguiente);
        }
    }

    // Dibuja una esfera en la vista de escena para visualizar la distancia del sonido
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, distanciaActivacionSonido);
    }
}