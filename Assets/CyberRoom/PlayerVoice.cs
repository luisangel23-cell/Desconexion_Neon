using UnityEngine;
using UnityEngine.UI;

public class PlayerVoice : MonoBehaviour
{
    public AudioSource audioSource;

    [System.Serializable]
    public struct Dialogo
    {
        public AudioClip clipAudio;
        public float segundoDeInicio;
    }

    [Header("Sistema de Voces")]
    public Dialogo[] voces;
    private bool[] vozReproducida;

    [Header("Notificación 1: Controles (WASD)")]
    public AudioClip sonidoNotificacion; 
    public float segundoAparicionWASD = 15f; 
    public float duracionPantallaWASD = 3f;  
    private bool uiWASDMostrada = false;

    [Header("Notificación 2: Computador")]
    public AudioClip sonidoComputador;
    public float segundoAparicionPc = 20f;
    public float duracionPantallaPc = 4f;
    private bool uiPcMostrada = false;

    void Start()
    {
        if (voces != null)
            vozReproducida = new bool[voces.Length];
    }

    void Update()
    {
        if (audioSource == null) return;

        for (int i = 0; i < voces.Length; i++)
        {
            if (!vozReproducida[i] && voces[i].clipAudio != null)
            {
                if (Time.timeSinceLevelLoad >= voces[i].segundoDeInicio)
                {
                    audioSource.PlayOneShot(voces[i].clipAudio);
                    vozReproducida[i] = true;
                }
            }
        }

        if (!uiWASDMostrada && Time.timeSinceLevelLoad >= segundoAparicionWASD)
        {
            StartCoroutine(MostrarAvisoEnPantalla("MOVIMIENTO: [W][A][S][D]"));
            if (sonidoNotificacion != null) audioSource.PlayOneShot(sonidoNotificacion);
            uiWASDMostrada = true;
        }

        if (!uiPcMostrada && Time.timeSinceLevelLoad >= segundoAparicionPc)
        {
            StartCoroutine(MostrarAvisoEnPantalla("Acercate al computador"));
            if (sonidoComputador != null) audioSource.PlayOneShot(sonidoComputador);
            uiPcMostrada = true;
        }
    }

    System.Collections.IEnumerator MostrarAvisoEnPantalla(string mensaje)
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas_Generado");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        GameObject panelObj = new GameObject("Aviso_HUD");
        panelObj.transform.SetParent(canvas.transform, false);

        Image panelImg = panelObj.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.75f);

        RectTransform rectPanel = panelObj.GetComponent<RectTransform>();
        rectPanel.anchorMin = new Vector2(0.5f, 1f);
        rectPanel.anchorMax = new Vector2(0.5f, 1f);
        rectPanel.pivot = new Vector2(0.5f, 1f);
        rectPanel.anchoredPosition = new Vector2(0, -90);
        rectPanel.sizeDelta = new Vector2(250, 45);

        GameObject textoObj = new GameObject("Texto_Aviso");
        textoObj.transform.SetParent(panelObj.transform, false);

        Text textoUI = textoObj.AddComponent<Text>();
        textoUI.text = mensaje;
        textoUI.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textoUI.fontSize = 12;
        textoUI.alignment = TextAnchor.MiddleCenter;
        textoUI.color = Color.white;

        RectTransform rectTexto = textoObj.GetComponent<RectTransform>();
        rectTexto.anchorMin = Vector2.zero;
        rectTexto.anchorMax = Vector2.one;
        rectTexto.sizeDelta = Vector2.zero;

        yield return new WaitForSeconds(duracionPantallaPc);
        Destroy(panelObj);
    }
}