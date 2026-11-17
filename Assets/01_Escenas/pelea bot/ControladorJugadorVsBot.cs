using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class ControladorJugadorVsBot : MonoBehaviour
{
    private Animator anim;
    private CharacterController controller;
    private AudioSource audioSource;

    [Header("Identificación de Mando")]
    public int idMando = 1;

    [Header("Ajustes de Vida y UI")]
    public float vidaMaxima = 100f;
    private float vidaActual;
    public Image barraVidaUI; 
    public Text textoVidaUI; 

    [Header("Ajustes de Movimiento y Giro")]
    public float velocidadCaminar = 4f;
    public float fuerzaSalto = 8f;
    public float gravedad = -22f;
    private float velocidadY = 0f;
    public float velocidadGiro = 15f;

    [Header("Ajustes de Combate")]
    public float rangoAtaque = 4.0f;
    public float danoAtaque = 5f; 
    private ControladorBotIA oponenteBot;

    [Header("Efectos de Sonido")]
    public AudioClip sonidoSalto;
    public AudioClip sonidoPuño;
    public AudioClip sonidoPatada;
    public AudioClip sonidoRecibirGolpe;
    public AudioClip sonidoDerrotaFinal; 
    public AudioClip sonidoInicioPelea;

    [Header("Mecánicas Avanzadas (Dash, Stun, Combos)")]
    public Text textoComboUI;
    public float fuerzaDash = 7f; 
    public float duracionDash = 0.25f;
    public string animacionDashAtras = "Quickstep_B";
    public string animacionDashAdelante = "caminar adelante";
    public float duracionHitStun = 0.4f;

    [Header("Configuración de Intro")]
    public float duracionIntroSegundos = 3.0f; 
    private static float tiempoInicioJuego = 0f;
    private static bool sonidoGlobalReproducido = false;

    [Header("Configuración de Animaciones")]
    public string animacionDerrotaFinal = "Falling Back Death";
    public string[] ataquesPuño = { "puño", "hp_straight_A", "hp_upper_right_A", "bp_upper_left_A", "bp_hook_right_A" };
    public string[] ataquesPatada = { "hk_rh_right_A", "lk_rh_right_A", "lk_rh_left_A" };

    [Header("Ataques Aéreos")]
    public string[] ataquesPuñoAire = { "jump_punch" };
    public string[] ataquesPatadaAire = { "jump_kick" };
    private bool atacoEnElAire = false;

    private bool saltoRegistrado = false;
    private bool estaDerrotado = false;
    
    private float tiempoFinHitStun = 0f;
    private bool haciendoDash = false;
    private float tiempoUltimoInputDer = -1f;
    private float tiempoUltimoInputIzq = -1f;
    private float ultimoMovimientoH = 0f;
    
    private int contadorCombo = 0;
    private float tiempoUltimoImpactoRecibido = 0f;

    public static bool controlesBloqueados = true; 
    public static bool juegoTerminado = false;

    void Start()
    {
        estaDerrotado = false;
        juegoTerminado = false;
        controlesBloqueados = true;
        Time.timeScale = 1f;

        if (tiempoInicioJuego == 0f)
        {
            tiempoInicioJuego = Time.time;
            sonidoGlobalReproducido = false;
        }

        anim = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null) audioSource.playOnAwake = false;

        vidaActual = vidaMaxima;
        ActualizarBarraVida();
        AlternarVisibilidadUI(false);
        BuscarOponente();
        ReproducirAnimacionSegura("pose entrada", 0.1f);
    }

    void OnDestroy()
    {
        tiempoInicioJuego = 0f;
        sonidoGlobalReproducido = false;
        controlesBloqueados = true;
        Time.timeScale = 1f;
    }

    void BuscarOponente()
    {
        if (oponenteBot == null)
        {
            oponenteBot = FindObjectOfType<ControladorBotIA>();
        }
    }

    void Update()
    {
        // Si ya murió o terminó la pelea, congelar cualquier input o desplazamiento
        if (estaDerrotado || juegoTerminado)
        {
            return;
        }

        if (oponenteBot == null) BuscarOponente();

        if (Time.time - tiempoInicioJuego < duracionIntroSegundos)
        {
            MoverPersonaje(0f);
            return;
        }
        else
        {
            if (controlesBloqueados)
            {
                controlesBloqueados = false;
                AlternarVisibilidadUI(true);

                if (oponenteBot != null) 
                {
                    if (oponenteBot.barraVidaUI != null) oponenteBot.barraVidaUI.gameObject.SetActive(true);
                    if (oponenteBot.textoVidaUI != null) oponenteBot.textoVidaUI.gameObject.SetActive(true);
                }

                if (!sonidoGlobalReproducido && audioSource != null && sonidoInicioPelea != null)
                {
                    audioSource.PlayOneShot(sonidoInicioPelea);
                    sonidoGlobalReproducido = true;
                }
            }
        }

        if (controlesBloqueados)
        {
            MoverPersonaje(0f);
            return;
        }

        if (oponenteBot != null && !haciendoDash)
        {
            MirarAlBot();
        }

        if (vidaActual <= 0 || haciendoDash) 
        { 
            if (!haciendoDash) MoverPersonaje(0f); 
            return; 
        }

        if (Time.time < tiempoFinHitStun)
        {
            MoverPersonaje(0f); 
            return;
        }

        if (contadorCombo > 0 && Time.time - tiempoUltimoImpactoRecibido > 1.2f)
        {
            contadorCombo = 0; 
            if (textoComboUI != null) textoComboUI.text = "";
        }

        bool enSuelo = controller.isGrounded;
        if (enSuelo)
        {
            if (velocidadY < 0) velocidadY = -5f;
            atacoEnElAire = false; 
        }

        AnimatorStateInfo estadoActual = anim.GetCurrentAnimatorStateInfo(0);
        if (estadoActual.IsName("recibir_golpe")) { MoverPersonaje(0f); return; }

        float movimientoH = ObtenerMovimientoHorizontal();

        bool tapDer = movimientoH > 0.5f && ultimoMovimientoH <= 0.5f;
        bool tapIzq = movimientoH < -0.5f && ultimoMovimientoH >= -0.5f;
        ultimoMovimientoH = movimientoH;

        if (enSuelo)
        {
            if (tapDer)
            {
                if (Time.time - tiempoUltimoInputDer < 0.3f) 
                {
                    float dirDash = 1f;
                    if (oponenteBot != null) dirDash = (oponenteBot.transform.position.x > transform.position.x) ? 1f : -1f;
                    StartCoroutine(EjecutarDash(1f, dirDash));
                }
                tiempoUltimoInputDer = Time.time;
            }
            if (tapIzq)
            {
                if (Time.time - tiempoUltimoInputIzq < 0.3f) 
                {
                    float dirDash = -1f;
                    if (oponenteBot != null) dirDash = (oponenteBot.transform.position.x > transform.position.x) ? -1f : 1f;
                    StartCoroutine(EjecutarDash(-1f, dirDash));
                }
                tiempoUltimoInputIzq = Time.time;
            }
        }

        bool estaAtacandoTierra = false;
        foreach (string p in ataquesPuño) { if (estadoActual.IsName(p)) estaAtacandoTierra = true; }
        foreach (string k in ataquesPatada) { if (estadoActual.IsName(k)) estaAtacandoTierra = true; }

        if (estaAtacandoTierra && enSuelo) 
        { 
            MoverPersonaje(0f); 
            return; 
        }

        bool presionPuño = DetectarPuño();
        bool presionPatada = DetectarPatada();
        bool presionSalto = DetectarSalto();
        bool presionAbajo = DetectarAbajo();

        if (enSuelo) 
        {
            if (presionPuño)
            {
                ReproducirAnimacionSegura(ObtenerAnimacionAleatoria(ataquesPuño), 0.1f);
                if (sonidoPuño != null) audioSource.PlayOneShot(sonidoPuño);
                ProcesarImpactoAtaque();
                return;
            }
            else if (presionPatada)
            {
                ReproducirAnimacionSegura(ObtenerAnimacionAleatoria(ataquesPatada), 0.1f);
                if (sonidoPatada != null) audioSource.PlayOneShot(sonidoPatada);
                ProcesarImpactoAtaque();
                return;
            }
        }
        else 
        {
            if (!atacoEnElAire)
            {
                if (presionPuño)
                {
                    ReproducirAnimacionSegura(ObtenerAnimacionAleatoria(ataquesPuñoAire), 0.1f);
                    if (sonidoPuño != null) audioSource.PlayOneShot(sonidoPuño);
                    ProcesarImpactoAtaque();
                    atacoEnElAire = true;
                }
                else if (presionPatada)
                {
                    ReproducirAnimacionSegura(ObtenerAnimacionAleatoria(ataquesPatadaAire), 0.1f);
                    if (sonidoPatada != null) audioSource.PlayOneShot(sonidoPatada);
                    ProcesarImpactoAtaque();
                    atacoEnElAire = true;
                }
            }
        }

        string animacionDeseada = "idle_A";
        if (enSuelo)
        {
            if (presionAbajo) animacionDeseada = "bb_front_A"; 
            else
            {
                if (movimientoH > 0.1f) animacionDeseada = "caminar adelante";
                else if (movimientoH < -0.1f) animacionDeseada = "caminar atras";
            }

            if (presionSalto)
            {
                velocidadY = fuerzaSalto;
                ReproducirAnimacionSegura("saltar", 0.1f);
                if (sonidoSalto != null) audioSource.PlayOneShot(sonidoSalto);
                return; 
            }
        }
        else 
        {
            bool estaAtacandoAire = false;
            foreach (string p in ataquesPuñoAire) { if (estadoActual.IsName(p)) estaAtacandoAire = true; }
            foreach (string k in ataquesPatadaAire) { if (estadoActual.IsName(k)) estaAtacandoAire = true; }
            
            if (!estaAtacandoAire) animacionDeseada = "saltar";
            else animacionDeseada = ""; 
        }

        MoverPersonaje(movimientoH);

        if (animacionDeseada != "" && !estadoActual.IsName(animacionDeseada) && !anim.IsInTransition(0)) 
        {
            ReproducirAnimacionSegura(animacionDeseada, 0.2f);
        }
    }

    void MirarAlBot()
    {
        Vector3 direccion = (oponenteBot.transform.position - transform.position).normalized;
        direccion.y = 0;

        if (direccion != Vector3.zero)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadGiro * Time.deltaTime);
        }
    }

    public void AlternarVisibilidadUI(bool mostrar)
    {
        if (barraVidaUI != null) barraVidaUI.gameObject.SetActive(mostrar);
        if (textoVidaUI != null) textoVidaUI.gameObject.SetActive(mostrar);
    }

    IEnumerator EjecutarDash(float direccionEntrada, float direccionRelativaAlEnemigo)
    {
        haciendoDash = true;

        if (direccionRelativaAlEnemigo < 0) ReproducirAnimacionSegura(animacionDashAtras, 0.05f); 
        else ReproducirAnimacionSegura(animacionDashAdelante, 0.05f); 

        float elapsed = 0f;
        while(elapsed < duracionDash)
        {
            if (controller != null && controller.enabled && !estaDerrotado)
            {
                controller.Move(new Vector3(direccionEntrada * fuerzaDash * Time.deltaTime, -5f * Time.deltaTime, 0));
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (!estaDerrotado) ReproducirAnimacionSegura("idle_A", 0.02f);
        haciendoDash = false;
    }

    void MoverPersonaje(float movX)
    {
        if (controller != null && controller.enabled && !estaDerrotado)
        {
            velocidadY += gravedad * Time.deltaTime;
            Vector3 movimientoFinal = new Vector3(movX * velocidadCaminar, velocidadY, 0);
            controller.Move(movimientoFinal * Time.deltaTime);
        }
    }

    void ProcesarImpactoAtaque()
    {
        if (oponenteBot == null) BuscarOponente();
        
        if (oponenteBot != null)
        {
            float distancia = Vector3.Distance(transform.position, oponenteBot.transform.position);
            if (distancia <= rangoAtaque)
            {
                oponenteBot.RecibirGolpe(danoAtaque); 
            }
        }
    }

    public void RecibirGolpe(float cantidadDano)
    {
        if (vidaActual <= 0f || estaDerrotado || controlesBloqueados || juegoTerminado) return;

        vidaActual -= cantidadDano;
        if (vidaActual < 0f) vidaActual = 0f;
        ActualizarBarraVida();

        tiempoFinHitStun = Time.time + duracionHitStun;

        if (Time.time - tiempoUltimoImpactoRecibido <= 1.2f) contadorCombo++;
        else contadorCombo = 1;
        
        tiempoUltimoImpactoRecibido = Time.time;

        if (textoComboUI != null)
        {
            textoComboUI.text = contadorCombo >= 2 ? contadorCombo + " HITS!" : "";
        }

        if (vidaActual > 0f)
        {
            StartCoroutine(EfectoImpactoVisual());
            ReproducirAnimacionSegura("recibir_golpe", 0.05f);
            if (audioSource != null && sonidoRecibirGolpe != null) audioSource.PlayOneShot(sonidoRecibirGolpe);
        }
        else
        {
            // Muerte directa en la primera ronda
            Time.timeScale = 1f;
            haciendoDash = false;
            StopAllCoroutines();
            StartCoroutine(SecuenciaDerrotaFinal());
        }
    }

    IEnumerator EfectoImpactoVisual()
    {
        Time.timeScale = 0.1f;
        if (Camera.main != null)
        {
            Vector3 posOriginal = Camera.main.transform.position;
            float elapsed = 0f;
            while(elapsed < 0.05f) 
            {
                Camera.main.transform.position = posOriginal + (Vector3)Random.insideUnitCircle * 0.4f;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Camera.main.transform.position = posOriginal;
        }
        else yield return new WaitForSecondsRealtime(0.05f);
        Time.timeScale = 1f;
    }

    IEnumerator SecuenciaDerrotaFinal()
    {
        estaDerrotado = true; 
        juegoTerminado = true; 
        controlesBloqueados = true;
        Time.timeScale = 1f;

        if (controller != null) controller.enabled = false;
        
        if (anim != null)
        {
            anim.applyRootMotion = false;
            if (!ReproducirAnimacionSegura(animacionDerrotaFinal, 0.05f))
            {
                Debug.LogWarning("OJO: No se encontró la animación '" + animacionDerrotaFinal + "' en el Animator del Jugador.");
            }
        }
        
        if (audioSource != null && sonidoDerrotaFinal != null) 
        {
            audioSource.PlayOneShot(sonidoDerrotaFinal);
        }

        // Esperamos 1.5s para ver caer al jugador antes de mostrar el cartel de DERROTA
        yield return new WaitForSeconds(1.5f);

        if (ControladorFinPelea.Instancia != null)
        {
            ControladorFinPelea.Instancia.MostrarResultado(false);
        }
    }

    public void CurarAlMaximo()
    {
        vidaActual = vidaMaxima;
        ActualizarBarraVida();
        ReproducirAnimacionSegura("idle_A", 0.1f);
    }

    void ConectarUICanvas()
    {
        if (barraVidaUI == null)
        {
            Image[] imagenes = Resources.FindObjectsOfTypeAll<Image>();
            foreach (Image img in imagenes) { if (img.name == "Relleno_P1") barraVidaUI = img; }
        }
        if (textoVidaUI == null)
        {
            Text[] textos = Resources.FindObjectsOfTypeAll<Text>();
            foreach (Text txt in textos) { if (txt.name == "Texto_P1") textoVidaUI = txt; }
        }
    }

    void ActualizarBarraVida()
    {
        ConectarUICanvas();
        if (barraVidaUI != null) barraVidaUI.fillAmount = vidaActual / vidaMaxima;
        if (textoVidaUI != null) textoVidaUI.text = vidaActual.ToString("0") + "/" + vidaMaxima.ToString("0");
    }

    bool ReproducirAnimacionSegura(string nombreAnim, float duracionTransicion)
    {
        if (anim == null) return false;
        if (anim.HasState(0, Animator.StringToHash(nombreAnim)))
        {
            anim.CrossFade(nombreAnim, duracionTransicion);
            return true;
        }
        return false;
    }

    string ObtenerAnimacionAleatoria(string[] listaAtaques)
    {
        if (listaAtaques == null || listaAtaques.Length == 0) return "";
        return listaAtaques[Random.Range(0, listaAtaques.Length)];
    }

    float ObtenerMovimientoHorizontal()
    {
        float val = 0f;
        try { val = Input.GetAxis("Horizontal_P" + idMando); } catch { }

        if (Mathf.Abs(val) < 0.1f)
        {
            if (Input.GetKey(idMando == 1 ? KeyCode.Joystick1Button14 : KeyCode.Joystick2Button14)) val = -1f;
            if (Input.GetKey(idMando == 1 ? KeyCode.Joystick1Button15 : KeyCode.Joystick2Button15)) val = 1f;
        }

        if (idMando == 1)
        {
            if (Input.GetKey(KeyCode.D)) val = 1f;
            else if (Input.GetKey(KeyCode.A)) val = -1f;
        }
        return Mathf.Clamp(val, -1f, 1f);
    }

    bool DetectarPuño()
    {
        return Input.GetKeyDown(KeyCode.Joystick1Button3) || Input.GetKeyDown(KeyCode.Joystick1Button2) || Input.GetKeyDown(KeyCode.J);
    }

    bool DetectarPatada()
    {
        return Input.GetKeyDown(KeyCode.Joystick1Button0) || Input.GetKeyDown(KeyCode.K);
    }

    bool DetectarSalto()
    {
        float axisY = 0f;
        try { axisY = Input.GetAxisRaw("Vertical_P" + idMando); } catch { }
        bool haySalto = axisY > 0.4f || Input.GetKey(KeyCode.Joystick1Button12) || Input.GetKeyDown(KeyCode.Joystick1Button1) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);

        if (haySalto && !saltoRegistrado) { saltoRegistrado = true; return true; }
        else if (!haySalto && saltoRegistrado) { saltoRegistrado = false; }
        return false;
    }

    bool DetectarAbajo()
    {
        float axisY = 0f;
        try { axisY = Input.GetAxisRaw("Vertical_P" + idMando); } catch { }
        return axisY < -0.4f || Input.GetKey(KeyCode.Joystick1Button13) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
    }
}