using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cambiar de escena
using System.Collections;

public class InteraccionComputador : MonoBehaviour
{
    [Header("Interfaz y Audio")]
    public GameObject textoInteraccion; // Arrastra aquí tu texto de UI
    public AudioSource audioSource;
    public AudioClip clipAudio;

    [Header("Escena Destino")]
    public string nombreSiguienteEscena = "NombreDeTuNuevaEscena"; // Pon el nombre exacto de la escena del minijuego

    private bool jugadorCerca = false;
    private bool yaInteractuo = false;

    void Start()
    {
        // Asegurarnos de que el texto esté apagado al inicio
        if (textoInteraccion != null)
        {
            textoInteraccion.SetActive(false);
        }
    }

    void Update()
    {
        // Si el jugador está en la zona, presiona F y no ha interactuado antes
        if (jugadorCerca && !yaInteractuo && Input.GetKeyDown(KeyCode.F))
        {
            StartCoroutine(ProcesarInteraccion());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verifica que sea el jugador el que se acerca
        if (other.CompareTag("Player") && !yaInteractuo)
        {
            jugadorCerca = true;
            if (textoInteraccion != null) textoInteraccion.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && !yaInteractuo)
        {
            jugadorCerca = false;
            if (textoInteraccion != null) textoInteraccion.SetActive(false);
        }
    }

    IEnumerator ProcesarInteraccion()
    {
        yaInteractuo = true; // Bloqueamos para que no presione F mil veces

        // Apagamos el texto de "Presione F"
        if (textoInteraccion != null) textoInteraccion.SetActive(false);

        // Reproducimos el audio
        if (audioSource != null && clipAudio != null)
        {
            audioSource.PlayOneShot(clipAudio);
            // Esperamos exactamente lo que dura el audio
            yield return new WaitForSeconds(clipAudio.length);
        }
        else
        {
            // Si por algún error no hay audio, esperamos 2 segundos por defecto
            yield return new WaitForSeconds(2f);
        }

        // Cargamos la siguiente escena
        SceneManager.LoadScene(nombreSiguienteEscena);
    }
}