using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement; // NUEVO: Librería necesaria para cambiar de escenas

public class ControladorCinematica2 : MonoBehaviour
{
    [Header("Referencias Principales")]
    [Tooltip("El objeto que reproduce tu audio inicial.")]
    [SerializeField] private AudioSource audioInicial;
    
    [Tooltip("El director del Timeline que contiene tu cinemática.")]
    [SerializeField] private PlayableDirector directorCinematica;

    [Header("Objetos a Prender (Inicio Cinematica)")]
    [Tooltip("Arrastra aquí a 'camarch', los NPCs y tu Cámara Principal.")]
    [SerializeField] private GameObject[] objetosParaActivar;

    [Header("Final de la Cinematica")]
    [Tooltip("El Panel (UI) negro que contiene el texto final.")]
    [SerializeField] private GameObject pantallaNegraUI;
    
    [Tooltip("El AudioSource con la música final.")]
    [SerializeField] private AudioSource musicaFinal;

    // NUEVO: Variables para el cambio de escena
    [Header("Cambio de Escena")]
    [Tooltip("Escribe el nombre EXACTO de la escena a la que quieres saltar (ej: MenuPrincipal, Nivel2).")]
    [SerializeField] private string nombreSiguienteEscena;

    private void Start()
    {
        if (directorCinematica != null)
        {
            directorCinematica.playOnAwake = false;
        }

        // Asegurarnos de que la pantalla negra empiece apagada por si se te olvida
        if (pantallaNegraUI != null)
        {
            pantallaNegraUI.SetActive(false);
        }

        if (audioInicial != null && audioInicial.clip != null)
        {
            StartCoroutine(EsperarAudioYArrancar());
        }
        else
        {
            Debug.LogWarning("Falta asignar el Audio o el AudioSource no tiene un Clip de sonido asignado.");
        }
    }

    private IEnumerator EsperarAudioYArrancar()
    {
        // 1. Esperamos lo que dure el audio inicial
        float duracionAudio = audioInicial.clip.length;
        yield return new WaitForSeconds(duracionAudio);

        // 2. Prendemos las cámaras y NPCs
        foreach (GameObject obj in objetosParaActivar)
        {
            if (obj != null) obj.SetActive(true);
        }

        // 3. Le damos Play al Timeline
        if (directorCinematica != null)
        {
            directorCinematica.Play();
            
            // 4. Obtenemos cuánto dura la cinemática y la esperamos
            float duracionCinematica = (float)directorCinematica.duration;
            yield return new WaitForSeconds(duracionCinematica);
        }

        // 5. ¡Terminó la cinemática! Mostramos la pantalla negra y suena la música
        if (pantallaNegraUI != null)
        {
            pantallaNegraUI.SetActive(true);
        }

        if (musicaFinal != null)
        {
            musicaFinal.Play();
        }

        Debug.Log("<color=#00FFFF>[Final]</color> Cinematica terminada. Pantalla negra activada. Esperando 5 segundos...");

        // 6. NUEVO: Esperar 5 segundos antes de cambiar de escena
        yield return new WaitForSeconds(5f);

        // Apagar la música justo al momento de hacer el cambio
        if (musicaFinal != null)
        {
            musicaFinal.Stop();
        }

        // Cambiar a la escena indicada
        if (!string.IsNullOrEmpty(nombreSiguienteEscena))
        {
            SceneManager.LoadScene(nombreSiguienteEscena);
        }
        else
        {
            Debug.LogError("No escribiste el nombre de la siguiente escena en el Inspector del script.");
        }
    }
}