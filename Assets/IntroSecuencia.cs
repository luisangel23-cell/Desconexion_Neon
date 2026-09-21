using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class IntroSecuencia : MonoBehaviour
{
    [Header("Referencias de Video WebM")]
    public VideoPlayer videoPlayer;      
    public VideoClip videoEsperaZ;       
    public VideoClip videoTransicion;    
    
    [Header("Referencias de Audio separadas")]
    public AudioSource audioIntro;      
    public AudioSource audioTransicion; 
    
    [Header("Configuración de Escena")]
    public string nombreEscenaJuego = "Escena 01"; 

    private bool zPresionada = false;

    void Start()
    {
        // Aseguramos volúmenes iniciales limpios
        if (audioIntro != null) audioIntro.volume = 1f;
        if (audioTransicion != null) audioTransicion.volume = 0f;

        if (videoPlayer != null && videoEsperaZ != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.clip = videoEsperaZ;
            videoPlayer.isLooping = true;
            
            videoPlayer.Prepare();
            videoPlayer.prepareCompleted += (vp) => {
                videoPlayer.Play();
                if (audioIntro != null) audioIntro.Play();
                Debug.Log("[INTRO] Reproduciendo video y audio inicial.");
            };
        }
    }

    void Update()
    {
        if (!zPresionada && Input.GetKeyDown(KeyCode.Z))
        {
            zPresionada = true;
            CambiarAlSegundoVideoWebM();
        }
    }

    void CambiarAlSegundoVideoWebM()
    {
        // Apagamos totalmente el volumen y detenemos el audio 1
        if (audioIntro != null) 
        {
            audioIntro.volume = 0f;
            audioIntro.Stop();
        }

        // Activamos al 100% el volumen y reproducimos el audio 2 de transición
        if (audioTransicion != null) 
        {
            audioTransicion.volume = 1f;
            audioTransicion.Play();
        }

        if (videoPlayer != null && videoTransicion != null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = videoTransicion;
            videoPlayer.isLooping = false;
            
            videoPlayer.Prepare();
            videoPlayer.prepareCompleted += (vp) => {
                videoPlayer.Play();
            };
            
            videoPlayer.loopPointReached += AlTerminarTransicion;
            Debug.Log("[INTRO] Transición activada, audio 1 silenciado y detenido.");
        }
    }

    void AlTerminarTransicion(VideoPlayer vp)
    {
        Debug.Log("[INTRO] Transición finalizada. Cargando el juego...");
        SceneManager.LoadScene(nombreEscenaJuego);
    }
}