using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement; // <--- Importante para cambiar de escena
using System.Collections;
using System.Collections.Generic;

public class InteraccionComputadora : MonoBehaviour
{
    [Header("¡REFERENCIAS OBLIGATORIAS! (Arrastra aquí)")]
    public GameObject objetoJugador; 
    public Camera camaraJugador;    
    public Transform puertaSalida;      // <--- Arrastra aquí "door 3"
    public Transform materaLlave;       // <--- Arrastra aquí "palm" (la matera)

    [Header("Configuración de Escenas")]
    public string nombreEscenaCalle = "escena02"; // <--- Escribe aquí el nombre exacto de la escena de la calle

    [Header("Configuración de Interacción")]
    public float tiempoBloqueoInicial = 20f;

    [Header("Configuración de Video y Audio")]
    public VideoClip videoParaReproducir;    
    public AudioClip audioDelVideoMp3;        
    public AudioSource audioSourceJugador;    
    public AudioClip audioVozDespuesDelVideo; 
    public AudioClip audioPuertaCerrada;      
    public AudioClip audioPuertaAbierta;      
    public AudioClip audioCogerLlave;         

    private GameObject panelE;
    private Text textoE;
    private bool estaCerca = false;
    private bool jugadorEnZona = false;
    private bool videoReproduciendose = false;

    private VideoPlayer videoPlayer;
    private AudioSource audioSourceVideoTemporal;

    private Canvas canvasGlobal;
    private List<GameObject> notificacionesActivas = new List<GameObject>();
    private GameObject contenedorEstres;
    private Image barraEstresRelleno;

    private GameObject panelObjetivo;
    private Text textoObjetivo;
    private GameObject panelPromptF;
    private Text textoPromptF;
    
    // Estados de la misión
    private bool objetivoActivo = false;
    private bool puertaIntentada = false; 
    private bool tieneLlave = false;
    private bool puertaAbierta = false;
    private bool estaBloqueadoPorAudioVoz = false;

    void Start()
    {
        CrearCanvasGlobal();
        CrearUIPromptE();
        ConfigurarVideoDirecto();
        CrearUIEstres(); 
        CrearUIObjetivoYPuerta();
    }

    void Update()
    {
        if (videoReproduciendose || estaBloqueadoPorAudioVoz) return;

        if (Time.timeSinceLevelLoad < tiempoBloqueoInicial)
        {
            if (panelE != null && panelE.activeSelf)
                panelE.SetActive(false);
            return;
        }

        // Interacción con la computadora (Tecla E)
        if (jugadorEnZona && !objetivoActivo)
        {
            if (!estaCerca)
            {
                estaCerca = true;
                if (panelE != null) panelE.SetActive(true);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                AccionComputadora();
            }
        }
        else
        {
            if (estaCerca)
            {
                estaCerca = false;
                if (panelE != null) panelE.SetActive(false);
            }
        }

        // Detección por distancia cuando el objetivo de salida está activo
        if (objetivoActivo && objetoJugador != null && !puertaAbierta)
        {
            float distanciaPuerta = puertaSalida != null ? Vector3.Distance(objetoJugador.transform.position, puertaSalida.position) : 999f;
            float distanciaMatera = materaLlave != null ? Vector3.Distance(objetoJugador.transform.position, materaLlave.position) : 999f;

            // 1. Cerca de la puerta
            if (distanciaPuerta < 3.5f)
            {
                if (panelPromptF != null && !panelPromptF.activeSelf)
                {
                    panelPromptF.SetActive(true);
                }

                if (!puertaIntentada)
                {
                    textoPromptF.text = "Presiona [ F ] para abrir";
                }
                else if (puertaIntentada && !tieneLlave)
                {
                    textoPromptF.text = "Buscar llave en la matera";
                }
                else if (tieneLlave)
                {
                    textoPromptF.text = "Presiona [ F ] para abrir con llave";
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    if (!puertaIntentada)
                    {
                        IntentarAbrirPuerta();
                    }
                    else if (tieneLlave)
                    {
                        AbrirPuertaConLlave();
                    }
                }
                return;
            }

            // 2. Cerca de la matera (solo si ya intentamos abrir la puerta y aún no tenemos la llave)
            if (puertaIntentada && !tieneLlave && distanciaMatera < 2.5f)
            {
                if (panelPromptF != null && !panelPromptF.activeSelf)
                {
                    panelPromptF.SetActive(true);
                }

                textoPromptF.text = "Presiona [ F ] para recoger llave";

                if (Input.GetKeyDown(KeyCode.F))
                {
                    AgarrarLlave();
                }
                return;
            }

            // Si no estamos cerca de ninguno, apagamos el prompt
            if (panelPromptF != null && panelPromptF.activeSelf)
            {
                if (distanciaPuerta >= 3.5f && distanciaMatera >= 2.5f)
                {
                    panelPromptF.SetActive(false);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (objetoJugador != null && (other.gameObject == objetoJugador || other.transform.IsChildOf(objetoJugador.transform)))
        {
            if (!objetivoActivo)
            {
                jugadorEnZona = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (objetoJugador != null && (other.gameObject == objetoJugador || other.transform.IsChildOf(objetoJugador.transform)))
        {
            jugadorEnZona = false;
        }
    }

    void CrearCanvasGlobal()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas_Global");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        canvasGlobal = canvas;
    }

    void CrearUIPromptE()
    {
        GameObject panelObj = new GameObject("Prompt_Tecla_E");
        panelObj.transform.SetParent(canvasGlobal.transform, false);

        Image panelImg = panelObj.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.85f);

        RectTransform rect = panelObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -60); 
        rect.sizeDelta = new Vector2(110, 40);      

        GameObject textoObj = new GameObject("Texto_E");
        textoObj.transform.SetParent(panelObj.transform, false);

        textoE = textoObj.AddComponent<Text>();
        textoE.text = "Presiona [ E ]";
        textoE.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textoE.fontSize = 12;
        textoE.alignment = TextAnchor.MiddleCenter;
        textoE.color = Color.yellow;

        RectTransform rectTexto = textoObj.GetComponent<RectTransform>();
        rectTexto.anchorMin = Vector2.zero;
        rectTexto.anchorMax = Vector2.one;
        rectTexto.sizeDelta = Vector2.zero;

        panelObj.SetActive(false);
        panelE = panelObj;
    }

    void ConfigurarVideoDirecto()
    {
        GameObject videoObj = new GameObject("Reproductor_Cinematica");
        videoPlayer = videoObj.AddComponent<VideoPlayer>();
        
        videoPlayer.playOnAwake = false;
        videoPlayer.clip = videoParaReproducir;
        videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
        
        if (camaraJugador != null)
        {
            videoPlayer.targetCamera = camaraJugador;
        }

        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

        audioSourceVideoTemporal = videoObj.AddComponent<AudioSource>();
        audioSourceVideoTemporal.playOnAwake = false;
        audioSourceVideoTemporal.clip = audioDelVideoMp3;
        audioSourceVideoTemporal.volume = 1f;

        videoPlayer.prepareCompleted += AlCompletarPreparacionVideo;
        videoPlayer.loopPointReached += AlTerminarElVideo;
    }

    void AccionComputadora()
    {
        if (videoParaReproducir == null || camaraJugador == null) return;
        
        videoReproduciendose = true;

        if (panelE != null) panelE.SetActive(false);
        estaCerca = false;
        jugadorEnZona = false;

        BloquearJugador(true);
        videoPlayer.Prepare();
    }

    void AlCompletarPreparacionVideo(VideoPlayer vp)
    {
        vp.Play();
        if (audioSourceVideoTemporal != null && audioDelVideoMp3 != null)
        {
            audioSourceVideoTemporal.Play();
        }
    }

    void AlTerminarElVideo(VideoPlayer vp)
    {
        videoPlayer.Stop();
        if (audioSourceVideoTemporal != null) audioSourceVideoTemporal.Stop();

        if (audioSourceJugador != null && audioVozDespuesDelVideo != null)
        {
            AudioSource sourceReal = audioSourceJugador;
            if (audioSourceJugador.gameObject != gameObject && audioSourceJugador.GetComponent<AudioSource>() != null)
            {
                sourceReal = audioSourceJugador.GetComponent<AudioSource>();
            }
            
            sourceReal.PlayOneShot(audioVozDespuesDelVideo);
            estaBloqueadoPorAudioVoz = true;
            StartCoroutine(EsperarVozYMostrarNotificaciones(audioVozDespuesDelVideo.length));
        }
        else
        {
            StartCoroutine(EsperarVozYMostrarNotificaciones(0f));
        }
    }

    IEnumerator EsperarVozYMostrarNotificaciones(float duracionVoz)
    {
        yield return new WaitForSeconds(duracionVoz);
        estaBloqueadoPorAudioVoz = false;
        LanzarNotificacionesRedesSociales();
    }

    void LanzarNotificacionesRedesSociales()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        string[] mensajesRedes = new string[] {
            "Nueva app 'ChatLink': Tienes 1 solicitud de amistad pendiente.",
            "Juan Pérez te ha enviado una solicitud de amistad.",
            "¡Instala la nueva app de mensajería 'Vibe' ahora!",
            "Tienes un nuevo mensaje directo de un usuario desconocido.",
            "Alerta social: 3 personas quieren conectar contigo."
        };

        Vector2[] posiciones = new Vector2[] {
            new Vector2(0, 70),
            new Vector2(0, 25),
            new Vector2(0, -20),
            new Vector2(0, -65),
            new Vector2(0, -110)
        };

        notificacionesActivas.Clear();

        for (int i = 0; i < 5; i++)
        {
            GameObject notif = CrearNotificacionUI(mensajesRedes[i], posiciones[i], i);
            notificacionesActivas.Add(notif);
        }
    }

    GameObject CrearNotificacionUI(string mensaje, Vector2 posicion, int index)
    {
        GameObject notifObj = new GameObject("Notificacion_" + index);
        notifObj.transform.SetParent(canvasGlobal.transform, false);

        Image bg = notifObj.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f); 

        RectTransform rect = notifObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = new Vector2(240, 38);

        GameObject textObj = new GameObject("Texto");
        textObj.transform.SetParent(notifObj.transform, false);
        Text txt = textObj.AddComponent<Text>();
        txt.text = mensaje;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 10;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleLeft;

        RectTransform rectTxt = textObj.GetComponent<RectTransform>();
        rectTxt.anchorMin = Vector2.zero;
        rectTxt.anchorMax = Vector2.one;
        rectTxt.offsetMin = new Vector2(8, 4);
        rectTxt.offsetMax = new Vector2(-25, -4);

        GameObject btnObj = new GameObject("BotonCerrar");
        btnObj.transform.SetParent(notifObj.transform, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.85f, 0.2f, 0.2f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        RectTransform rectBtn = btnObj.GetComponent<RectTransform>();
        rectBtn.anchorMin = new Vector2(1, 0.5f);
        rectBtn.anchorMax = new Vector2(1, 0.5f);
        rectBtn.pivot = new Vector2(1, 0.5f);
        rectBtn.anchoredPosition = new Vector2(-6, 0);
        rectBtn.sizeDelta = new Vector2(16, 16);

        GameObject btnTextObj = new GameObject("XText");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        Text txtX = btnTextObj.AddComponent<Text>();
        txtX.text = "X";
        txtX.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txtX.fontSize = 9;
        txtX.color = Color.white;
        txtX.alignment = TextAnchor.MiddleCenter;
        RectTransform rectTxtX = btnTextObj.GetComponent<RectTransform>();
        rectTxtX.anchorMin = Vector2.zero;
        rectTxtX.anchorMax = Vector2.one;
        rectTxtX.sizeDelta = Vector2.zero;

        btn.onClick.AddListener(() => {
            DestruirNotificacion(notifObj);
        });

        return notifObj;
    }

    void DestruirNotificacion(GameObject notif)
    {
        if (notificacionesActivas.Contains(notif))
        {
            notificacionesActivas.Remove(notif);
            Destroy(notif);
        }

        if (notificacionesActivas.Count == 0)
        {
            ActivarBarraEstres();
            BloquearJugador(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            videoReproduciendose = false;

            StartCoroutine(SecuenciaSalirDeHabitacion());
        }
    }

    IEnumerator SecuenciaSalirDeHabitacion()
    {
        yield return new WaitForSeconds(2f);
        
        if (panelObjetivo != null)
        {
            panelObjetivo.SetActive(true);
            textoObjetivo.text = "¡Objetivo: Sal de la habitación!";
            
            yield return new WaitForSeconds(3.5f);
            panelObjetivo.SetActive(false);
        }

        objetivoActivo = true; 
    }

    void CrearUIEstres()
    {
        GameObject contenedor = new GameObject("Panel_Estres");
        contenedor.transform.SetParent(canvasGlobal.transform, false);

        Image fondoPanel = contenedor.AddComponent<Image>();
        fondoPanel.color = new Color(0.1f, 0.1f, 0.1f, 0.7f); 

        RectTransform rectContenedor = contenedor.GetComponent<RectTransform>();
        rectContenedor.anchorMin = new Vector2(0.5f, 1f);
        rectContenedor.anchorMax = new Vector2(0.5f, 1f);
        rectContenedor.pivot = new Vector2(0.5f, 1f);
        rectContenedor.anchoredPosition = new Vector2(0, -65); 
        rectContenedor.sizeDelta = new Vector2(190, 48); 

        GameObject textoObj = new GameObject("Texto_Estres");
        textoObj.transform.SetParent(contenedor.transform, false);
        Text txt = textoObj.AddComponent<Text>();
        txt.text = "Medidor de estrés";
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 11; 
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;

        RectTransform rectTxt = textoObj.GetComponent<RectTransform>();
        rectTxt.anchorMin = new Vector2(0, 0.5f);
        rectTxt.anchorMax = new Vector2(1, 1f);
        rectTxt.anchoredPosition = new Vector2(0, -2);
        rectTxt.sizeDelta = Vector2.zero;

        GameObject fondoBarra = new GameObject("FondoBarra");
        fondoBarra.transform.SetParent(contenedor.transform, false);
        Image imgFondo = fondoBarra.AddComponent<Image>();
        imgFondo.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        RectTransform rectFondo = fondoBarra.GetComponent<RectTransform>();
        rectFondo.anchorMin = new Vector2(0.5f, 0f);
        rectFondo.anchorMax = new Vector2(0.5f, 0f);
        rectFondo.pivot = new Vector2(0.5f, 0f);
        rectFondo.anchoredPosition = new Vector2(0, 6);
        rectFondo.sizeDelta = new Vector2(160, 9); 

        GameObject rellenoBarra = new GameObject("RellenoBarra");
        rellenoBarra.transform.SetParent(fondoBarra.transform, false);
        barraEstresRelleno = rellenoBarra.AddComponent<Image>();
        barraEstresRelleno.color = Color.red;
        barraEstresRelleno.type = Image.Type.Filled;
        barraEstresRelleno.fillMethod = Image.FillMethod.Horizontal;
        barraEstresRelleno.fillAmount = 1f; 

        RectTransform rectRelleno = rellenoBarra.GetComponent<RectTransform>();
        rectRelleno.anchorMin = Vector2.zero;
        rectRelleno.anchorMax = Vector2.one;
        rectRelleno.sizeDelta = Vector2.zero;

        contenedor.SetActive(false);
        contenedorEstres = contenedor;
    }

    void CrearUIObjetivoYPuerta()
    {
        GameObject objPanel = new GameObject("Panel_Objetivo");
        objPanel.transform.SetParent(canvasGlobal.transform, false);
        Image imgObj = objPanel.AddComponent<Image>();
        imgObj.color = new Color(0, 0, 0, 0.8f);
        RectTransform rectObj = objPanel.GetComponent<RectTransform>();
        rectObj.anchorMin = new Vector2(0.5f, 0.5f);
        rectObj.anchorMax = new Vector2(0.5f, 0.5f);
        rectObj.pivot = new Vector2(0.5f, 0.5f);
        rectObj.anchoredPosition = new Vector2(0, 40); 
        rectObj.sizeDelta = new Vector2(220, 35); 

        GameObject txtObj = new GameObject("Texto_Objetivo");
        txtObj.transform.SetParent(objPanel.transform, false);
        textoObjetivo = txtObj.AddComponent<Text>();
        textoObjetivo.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textoObjetivo.fontSize = 11;
        textoObjetivo.color = Color.yellow;
        textoObjetivo.alignment = TextAnchor.MiddleCenter;
        RectTransform rectTxtObj = txtObj.GetComponent<RectTransform>();
        rectTxtObj.anchorMin = Vector2.zero;
        rectTxtObj.anchorMax = Vector2.one;
        rectTxtObj.sizeDelta = Vector2.zero;
        objPanel.SetActive(false);
        panelObjetivo = objPanel;

        GameObject panelF = new GameObject("Prompt_Tecla_F");
        panelF.transform.SetParent(canvasGlobal.transform, false);
        Image imgF = panelF.AddComponent<Image>();
        imgF.color = new Color(0, 0, 0, 0.85f);
        RectTransform rectF = panelF.GetComponent<RectTransform>();
        rectF.anchorMin = new Vector2(0.5f, 0.5f);
        rectF.anchorMax = new Vector2(0.5f, 0.5f);
        rectF.pivot = new Vector2(0.5f, 0.5f);
        rectF.anchoredPosition = new Vector2(0, -60);
        rectF.sizeDelta = new Vector2(220, 40);

        GameObject txtFObj = new GameObject("Texto_F");
        txtFObj.transform.SetParent(panelF.transform, false);
        textoPromptF = txtFObj.AddComponent<Text>();
        textoPromptF.text = "Presiona [ F ] para abrir";
        textoPromptF.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textoPromptF.fontSize = 11;
        textoPromptF.alignment = TextAnchor.MiddleCenter;
        textoPromptF.color = Color.cyan;
        RectTransform rectTxtF = txtFObj.GetComponent<RectTransform>();
        rectTxtF.anchorMin = Vector2.zero;
        rectTxtF.anchorMax = Vector2.one;
        rectTxtF.sizeDelta = Vector2.zero;
        panelF.SetActive(false);
        panelPromptF = panelF;
    }

    void IntentarAbrirPuerta()
    {
        if (audioSourceJugador != null && audioPuertaCerrada != null)
        {
            AudioSource sourceReal = audioSourceJugador;
            if (audioSourceJugador.gameObject != gameObject && audioSourceJugador.GetComponent<AudioSource>() != null)
            {
                sourceReal = audioSourceJugador.GetComponent<AudioSource>();
            }
            sourceReal.PlayOneShot(audioPuertaCerrada);
        }

        StartCoroutine(MostrarMensajeCerrada());
    }

    IEnumerator MostrarMensajeCerrada()
    {
        if (textoPromptF != null)
        {
            textoPromptF.text = "Está cerrada";
            textoPromptF.color = Color.red;
            yield return new WaitForSeconds(2f);
            
            puertaIntentada = true; 
            panelPromptF.SetActive(false);
        }
    }

    void AgarrarLlave()
    {
        tieneLlave = true;

        if (audioSourceJugador != null && audioCogerLlave != null)
        {
            AudioSource sourceReal = audioSourceJugador;
            if (audioSourceJugador.gameObject != gameObject && audioSourceJugador.GetComponent<AudioSource>() != null)
            {
                sourceReal = audioSourceJugador.GetComponent<AudioSource>();
            }
            sourceReal.PlayOneShot(audioCogerLlave);
        }

        StartCoroutine(MostrarMensajeLlaveCogida());
    }

    IEnumerator MostrarMensajeLlaveCogida()
    {
        if (textoPromptF != null)
        {
            textoPromptF.text = "¡Llave encontrada!";
            textoPromptF.color = Color.green;
            yield return new WaitForSeconds(2f);
            panelPromptF.SetActive(false);
        }
    }

    void AbrirPuertaConLlave()
    {
        puertaAbierta = true;

        if (audioSourceJugador != null && audioPuertaAbierta != null)
        {
            AudioSource sourceReal = audioSourceJugador;
            if (audioSourceJugador.gameObject != gameObject && audioSourceJugador.GetComponent<AudioSource>() != null)
            {
                sourceReal = audioSourceJugador.GetComponent<AudioSource>();
            }
            sourceReal.PlayOneShot(audioPuertaAbierta);
        }

        StartCoroutine(SecuenciaPuertaAbiertaExitosa());
    }

    IEnumerator SecuenciaPuertaAbiertaExitosa()
    {
        if (textoPromptF != null)
        {
            textoPromptF.text = "¡Abriendo puerta...";
            textoPromptF.color = Color.green;
            panelPromptF.SetActive(true);

            yield return new WaitForSeconds(1.5f);

            // AQUí CAMBIA DE ESCENA REALMENTE A LA CALLE
            SceneManager.LoadScene(nombreEscenaCalle);
        }
    }

    void ActivarBarraEstres()
    {
        if (contenedorEstres != null)
        {
            contenedorEstres.SetActive(true);
        }
    }

    void BloquearJugador(bool bloquear)
    {
        if (objetoJugador == null) return;

        CharacterController cc = objetoJugador.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = !bloquear;

        MonoBehaviour[] scripts = objetoJugador.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            string nombreScript = script.GetType().Name;
            if (nombreScript.Contains("FirstPersonController") || nombreScript.Contains("MouseLook") || nombreScript.Contains("PlayerMovement") || nombreScript.Contains("Camera"))
            {
                script.enabled = !bloquear;
            }
        }
    }
}