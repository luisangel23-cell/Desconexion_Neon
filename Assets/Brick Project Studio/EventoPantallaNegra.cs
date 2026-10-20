using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class EventoPantallaNegra : MonoBehaviour
{
    [Header("Configuración de Audio")]
    public AudioClip sonidoSuspenso; 
    private AudioSource audioSource;

    [Header("Pantalla Negra y Texto")]
    public GameObject pantallaNegra; 
    public TMP_Text textoFinal;       
    [TextArea(2, 5)]
    public string mensajeEnNegro = "El horror apenas comienza...";

    [Header("Escena Limpia de Destino")]
    public string nombreEscenaLimpia = "EscenaDespuesDelEvento"; 

    private bool activado = false;

    void Start()
    {
        if (pantallaNegra != null)
            pantallaNegra.SetActive(false);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; 
        audioSource.playOnAwake = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!activado && other.CompareTag("Player"))
        {
            activado = true;
            StartCoroutine(SecuenciaDeSuspenso());
        }
    }

    IEnumerator SecuenciaDeSuspenso()
    {
        // 1. Reproducimos el sonido 2D de suspenso
        if (sonidoSuspenso != null)
        {
            audioSource.clip = sonidoSuspenso;
            audioSource.Play();
            yield return new WaitForSeconds(sonidoSuspenso.length);
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }

        // 2. Activamos la pantalla negra y el texto (la música anterior sigue sonando aquí de fondo)
        if (pantallaNegra != null)
        {
            pantallaNegra.SetActive(true);
            if (textoFinal != null)
            {
                textoFinal.text = mensajeEnNegro;
            }
        }

        // 3. Esperamos los 5 segundos exactos con la pantalla negra y la música sonando
        yield return new WaitForSeconds(5f);

        // 4. Cargamos la escena limpia (aquí ocurre el cambio)
        SceneManager.LoadScene(nombreEscenaLimpia);
    }
}