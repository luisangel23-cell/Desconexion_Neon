using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class IntroSecuencia : MonoBehaviour
{
    [Header("Referencias de Video WebM")]
    public VideoPlayer videoPlayer;
    public VideoClip videoTransicion; // Aquí va "despertar2"

    [Header("Referencia de Audio")]
    public AudioSource audioTransicion;

    [Header("Configuración de Escena")]
    public string nombreEscenaJuego = "Escena 01";

    void Start()
    {
        Time.timeScale = 1f;

        if (audioTransicion != null)
        {
            audioTransicion.volume = 1f;
        }

        if (videoPlayer != null && videoTransicion != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.clip = videoTransicion;
            videoPlayer.isLooping = false;

            videoPlayer.prepareCompleted += AlPrepararVideo;
            videoPlayer.loopPointReached += AlTerminarTransicion;
            videoPlayer.Prepare();
        }
        else
        {
            // Respaldo por si falta asignar el video
            SceneManager.LoadScene(nombreEscenaJuego);
        }
    }

    void AlPrepararVideo(VideoPlayer vp)
    {
        vp.prepareCompleted -= AlPrepararVideo;
        vp.Play();
        if (audioTransicion != null) audioTransicion.Play();
        Debug.Log("[INTRO] Reproduciendo video despertar2 y su audio.");
    }

    void AlTerminarTransicion(VideoPlayer vp)
    {
        vp.loopPointReached -= AlTerminarTransicion;
        Debug.Log("[INTRO] Video finalizado. Cargando Escena 01...");
        SceneManager.LoadScene(nombreEscenaJuego);
    }
}