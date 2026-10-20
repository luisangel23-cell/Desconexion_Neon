using UnityEngine;
using TMPro;

public class SistemaLeerCarta : MonoBehaviour
{
    [Header("UI de Interacción (Texto 'Presiona F')")]
    public GameObject textoAviso; 

    [Header("UI del Panel de Lectura")]
    public GameObject panelCarta; 
    public TMP_Text textoCarta;    
    [TextArea(3, 10)]
    public string mensajeDeLaCarta = "Aquí va el contenido secreto de la carta...";

    [Header("Sonido 2D al Leer")]
    public AudioClip musicaCarta; 
    private static AudioSource audioSourceGlobal; 

    private bool jugadorCerca = false;
    private bool cartaAbierta = false;

    void Start()
    {
        if (panelCarta != null) panelCarta.SetActive(false);
        if (textoAviso != null) textoAviso.SetActive(false);
    }

    void Update()
    {
        if (jugadorCerca && Input.GetKeyDown(KeyCode.F))
        {
            if (!cartaAbierta)
            {
                AbrirCarta();
            }
            else
            {
                CerrarCarta();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
            if (textoAviso != null && !cartaAbierta)
            {
                textoAviso.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;
            if (textoAviso != null)
            {
                textoAviso.SetActive(false);
            }
            if (cartaAbierta)
            {
                CerrarCarta();
            }
        }
    }

    void AbrirCarta()
    {
        cartaAbierta = true;
        
        if (textoAviso != null) textoAviso.SetActive(false);

        if (panelCarta != null)
        {
            panelCarta.SetActive(true);
            if (textoCarta != null)
            {
                textoCarta.text = mensajeDeLaCarta; 
            }
        }

        if (musicaCarta != null && audioSourceGlobal == null)
        {
            GameObject tempAudio = new GameObject("MusicaFondoGlobal");
            audioSourceGlobal = tempAudio.AddComponent<AudioSource>();
            audioSourceGlobal.clip = musicaCarta;
            audioSourceGlobal.spatialBlend = 0f; 
            audioSourceGlobal.loop = true;      
            audioSourceGlobal.Play();
            DontDestroyOnLoad(tempAudio); 
        }
    }

    void CerrarCarta()
    {
        cartaAbierta = false;

        if (panelCarta != null) panelCarta.SetActive(false);

        if (jugadorCerca && textoAviso != null)
        {
            textoAviso.SetActive(true);
        }
    }
}