using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DesconexionNeon.Networking;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class ControladorCombate : MonoBehaviour
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

    [Header("Ajustes de Combate y Detección de Golpes")]
    public float rangoAtaque = 3.5f;
    public float danoAtaque = 5f; 
    private ControladorCombate oponente;

    [Header("Efectos de Sonido")]
    public AudioClip sonidoSalto;
    public AudioClip sonidoPuño;
    public AudioClip sonidoPatada;
    public AudioClip sonidoRecibirGolpe;
    public AudioClip sonidoDerrota; 
    public AudioClip segundoSonido; 
    public AudioClip sonidoDerrotaFinal; 
    [Tooltip("Sonido que suena cuando ambos terminan de hablar y arranca la pelea")]
    public AudioClip sonidoInicioPelea;

    [Header("Ataque Especial (Bola de Fuego)")]
    public GameObject prefabBolaFuego;
    public Transform puntoDisparoFuego;
    public AudioClip sonidoBolaFuego;
    public float danoFuego = 15f; 
    public float tiempoParaDisparar = 0.4f;
    public float alturaDisparo = 1.5f;
    public float tamanoFuego = 1.5f;

    [Header("Mecánicas Avanzadas (Dash, Stun, Combos)")]
    public Text textoComboUI;
    public float fuerzaDash = 7f; 
    public float duracionDash = 0.25f;
    public string animacionDashAtras = "Quickstep_B";
    public string animacionDashAdelante = "caminar adelante";
    public float duracionHitStun = 0.4f;

    [Header("Configuración de Intro")]
    [Tooltip("Tiempo en segundos que tardan los personajes hablando antes de iniciar la pelea")]
    public float duracionIntroSegundos = 3.0f; 
    private static float tiempoInicioJuego = 0f;
    private static bool sonidoGlobalReproducido = false;

    [Header("Configuración de Animaciones por Personaje")]
    public string animacionDerrotaFinal = "Falling Back Death";
    public string[] ataquesPuño = { "puño", "hp_straight_A", "hp_upper_right_A", "bp_upper_left_A", "bp_hook_right_A" };
    public string[] ataquesPatada = { "hk_rh_right_A", "lk_rh_right_A", "lk_rh_left_A" };

    [Header("Ataques Aéreos")]
    public string[] ataquesPuñoAire = { "jump_punch" };
    public string[] ataquesPatadaAire = { "jump_kick" };
    private bool atacoEnElAire = false;

    private bool saltoRegistrado = false;
    private bool estaDerrotado = false;
    
    // Variables Combo Especial
    private float tiempoUltimoAtras = -1f;
    private float tiempoTriangulo = -1f;
    private float tiempoCirculo = -1f;
    private bool estaHaciendoEspecial = false;

    // Variables Mecánicas Avanzadas
    private float tiempoFinHitStun = 0f;
    private bool haciendoDash = false;
    private float tiempoUltimoInputDer = -1f;
    private float tiempoUltimoInputIzq = -1f;
    private float ultimoMovimientoH = 0f;
    
    // Contador de Hits 
    private int contadorCombo = 0;
    private float tiempoUltimoImpactoRecibido = 0f;

    [Header("Sistema de Rondas")]
    public int derrotasAcumuladas = 0;

    public static bool controlesBloqueados = true; 
    public static bool juegoTerminado = false;

    // --- VARIABLES DE RED LAN ---
    [HideInInspector] public bool esRemotoEnRed = false;
    [HideInInspector] public bool usarControlesP1EnRed = false;
    [HideInInspector] public string ultimaAnimacionReproducida = "idle_A";
    public static bool esperandoRedLAN = false;

    public static void IniciarCombateLAN()
    {
        esperandoRedLAN = false;
        tiempoInicioJuego = Time.time;
        sonidoGlobalReproducido = false;
    }

    public float ObtenerVidaActual() => vidaActual;

    public void SincronizarVidaDesdeRed(float nuevaVida)
    {
        if (Mathf.Abs(vidaActual - nuevaVida) > 0.01f)
        {
            vidaActual = nuevaVida;
            ActualizarBarraVida();
        }
    }

    void Start()
    {
        derrotasAcumuladas = 0;
        estaDerrotado = false;
        juegoTerminado = false;
        controlesBloqueados = true;

        if (tiempoInicioJuego == 0f)
        {
            tiempoInicioJuego = Time.time;
            sonidoGlobalReproducido = false;
        }

        anim = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null) audioSource.playOnAwake = false;

        // Auto-Conectar UI de Vida
        string nombreBarra = (idMando == 1) ? "Relleno_P1" : "Relleno_P2";
        GameObject barraObj = GameObject.Find(nombreBarra);
        if (barraObj != null) barraVidaUI = barraObj.GetComponent<Image>();

        string nombreTexto = (idMando == 1) ? "Texto_P1" : "Texto_P2";
        GameObject textoObj = GameObject.Find(nombreTexto);
        if (textoObj != null) textoVidaUI = textoObj.GetComponent<Text>();

        // Auto-conectar el texto del Combo
        if (textoComboUI == null)
        {
            string nombreTextoCombo = (idMando == 1) ? "TextoCombo_P1" : "TextoCombo_P2";
            GameObject comboObj = GameObject.Find(nombreTextoCombo);
            if (comboObj != null) textoComboUI = comboObj.GetComponent<Text>();
        }
        
        if (textoComboUI != null) textoComboUI.text = "";

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
        esperandoRedLAN = false;
    }

    void BuscarOponente()
    {
        ControladorCombate[] peleadores = FindObjectsOfType<ControladorCombate>();
        foreach (var p in peleadores) { if (p != this) { oponente = p; break; } }
    }

    void Update()
    {
        if (oponente == null) BuscarOponente();

        // Si estamos en la sala LAN esperando a que ambos se conecten, mantener quietos
        if (esperandoRedLAN)
        {
            MoverPersonaje(0f);
            return;
        }

        // Si este personaje lo controla el oponente por red LAN, no leer inputs locales
        if (esRemotoEnRed)
        {
            return;
        }

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

                ControladorCombate[] peleadores = FindObjectsOfType<ControladorCombate>();
                foreach (var p in peleadores)
                {
                    p.AlternarVisibilidadUI(true);
                }

                if (!sonidoGlobalReproducido && audioSource != null && sonidoInicioPelea != null)
                {
                    audioSource.PlayOneShot(sonidoInicioPelea);
                    sonidoGlobalReproducido = true;
                }
            }
        }

        if (juegoTerminado && estaDerrotado) return;
        if (juegoTerminado || controlesBloqueados)
        {
            MoverPersonaje(0f);
            if (!estaDerrotado && juegoTerminado) ReproducirAnimacionSegura("idle_A", 0.1f);
            return;
        }

        // --- VOLTEARSE AUTOMÁTICAMENTE HACIA EL RIVAL SI NO LO MIRA ---
        if (oponente != null && !haciendoDash && !estaDerrotado)
        {
            MirarAlOponente();
        }

        if (vidaActual <= 0 || estaDerrotado || estaHaciendoEspecial || haciendoDash) 
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

        int mandoEfectivo = usarControlesP1EnRed ? 1 : idMando;

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
                    float dirDash = (oponente != null && oponente.transform.position.x > transform.position.x) ? 1f : -1f;
                    StartCoroutine(EjecutarDash(1f, dirDash));
                }
                tiempoUltimoInputDer = Time.time;
            }
            if (tapIzq)
            {
                if (Time.time - tiempoUltimoInputIzq < 0.3f) 
                {
                    float dirDash = (oponente != null && oponente.transform.position.x > transform.position.x) ? 1f : -1f;
                    StartCoroutine(EjecutarDash(-1f, dirDash));
                }
                tiempoUltimoInputIzq = Time.time;
            }
        }

        if (oponente != null)
        {
            float difX = transform.position.x - oponente.transform.position.x;
            if ((difX < 0 && movimientoH < -0.1f) || (difX > 0 && movimientoH > 0.1f)) tiempoUltimoAtras = Time.time;
        }
        bool caminandoAtrasRecientemente = (Time.time - tiempoUltimoAtras) <= 0.35f;

        if (Input.GetKeyDown((mandoEfectivo == 1) ? KeyCode.Joystick1Button3 : KeyCode.Joystick2Button3)) tiempoTriangulo = Time.time;
        if (Input.GetKeyDown((mandoEfectivo == 1) ? KeyCode.Joystick1Button1 : KeyCode.Joystick2Button1) || Input.GetKeyDown((mandoEfectivo == 1) ? KeyCode.Joystick1Button2 : KeyCode.Joystick2Button2)) tiempoCirculo = Time.time;
        
        bool presionoAmbos = (Time.time - tiempoTriangulo <= 0.3f) && (Time.time - tiempoCirculo <= 0.3f);
        bool atajoEspecial = Input.GetKeyDown((mandoEfectivo == 1) ? KeyCode.Joystick1Button5 : KeyCode.Joystick2Button5) || ((mandoEfectivo == 1) ? Input.GetKeyDown(KeyCode.L) : Input.GetKeyDown(KeyCode.Keypad3));

        bool quiereLanzarPoder = (presionoAmbos && caminandoAtrasRecientemente) || atajoEspecial;

        bool estaAtacandoTierra = false;
        foreach (string p in ataquesPuño) { if (estadoActual.IsName(p)) estaAtacandoTierra = true; }
        foreach (string k in ataquesPatada) { if (estadoActual.IsName(k)) estaAtacandoTierra = true; }

        bool estaAtacandoAire = false;
        foreach (string p in ataquesPuñoAire) { if (estadoActual.IsName(p)) estaAtacandoAire = true; }
        foreach (string k in ataquesPatadaAire) { if (estadoActual.IsName(k)) estaAtacandoAire = true; }

        if (estaAtacandoTierra && enSuelo) 
        { 
            if (!quiereLanzarPoder) { MoverPersonaje(0f); return; }
        }

        bool presionPuño = DetectarPuño();
        bool presionPatada = DetectarPatada();
        bool presionSalto = DetectarSalto();
        bool presionAbajo = DetectarAbajo();

        if (enSuelo) 
        {
            if (quiereLanzarPoder)
            {
                tiempoTriangulo = 0f; tiempoCirculo = 0f; tiempoUltimoAtras = 0f;
                StartCoroutine(SecuenciaBolaFuego());
                return;
            }
            else if (presionPuño)
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
            if (!estaAtacandoAire) animacionDeseada = "saltar";
            else animacionDeseada = ""; 
        }

        MoverPersonaje(movimientoH);

        if (animacionDeseada != "" && !estadoActual.IsName(animacionDeseada) && !anim.IsInTransition(0)) 
        {
            ReproducirAnimacionSegura(animacionDeseada, 0.2f);
        }
    }

    void MirarAlOponente()
    {
        Vector3 direccion = (oponente.transform.position - transform.position).normalized;
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

        if (direccionRelativaAlEnemigo < 0) 
        {
            ReproducirAnimacionSegura(animacionDashAtras, 0.05f); 
        }
        else 
        {
            ReproducirAnimacionSegura(animacionDashAdelante, 0.05f); 
        }

        float elapsed = 0f;
        while(elapsed < duracionDash)
        {
            if (controller != null && controller.enabled)
            {
                controller.Move(new Vector3(direccionEntrada * fuerzaDash * Time.deltaTime, -5f * Time.deltaTime, 0));
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        ReproducirAnimacionSegura("idle_A", 0.02f);
        haciendoDash = false;
    }

    IEnumerator SecuenciaBolaFuego()
    {
        estaHaciendoEspecial = true;
        MoverPersonaje(0f); 
        ReproducirAnimacionSegura("magic heal", 0.05f);
        yield return new WaitForSeconds(tiempoParaDisparar);

        if (sonidoBolaFuego != null && audioSource != null) audioSource.PlayOneShot(sonidoBolaFuego);

        InstanciarProyectilFuego(true);

        // Notificar al otro PC en LAN para que dibuje la bola de fuego visualmente
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
        {
            NetworkManager.Instance.SendPacket(new PlayerActionPacket
            {
                playerId = this.idMando,
                actionName = "Fireball"
            });
        }

        yield return new WaitForSeconds(0.6f); 
        ReproducirAnimacionSegura("idle_A", 0.1f);
        estaHaciendoEspecial = false;
    }

    public void DispararBolaFuegoVisualRemota()
    {
        if (sonidoBolaFuego != null && audioSource != null) audioSource.PlayOneShot(sonidoBolaFuego);
        InstanciarProyectilFuego(false);
    }

    private void InstanciarProyectilFuego(bool haceDanoReal)
    {
        if (prefabBolaFuego == null) return;

        float dirX = (oponente != null && oponente.transform.position.x > transform.position.x) ? 1f : -1f;
        Vector3 posDisparo = transform.position + new Vector3(dirX * 0.8f, alturaDisparo, 0f); 
        if (puntoDisparoFuego != null) posDisparo = puntoDisparoFuego.position;

        GameObject bola = Instantiate(prefabBolaFuego, posDisparo, Quaternion.identity);
        if (dirX < 0) bola.transform.rotation = Quaternion.Euler(0, 180, 0);
        bola.transform.localScale *= tamanoFuego;

        Proyectil proyectil = bola.GetComponent<Proyectil>();
        if (proyectil == null) proyectil = bola.AddComponent<Proyectil>();
        // Si es visual remoto, daño 0 porque el PC atacante ya calcula y envía el impacto
        proyectil.Inicializar(new Vector3(dirX, 0, 0), this, haceDanoReal ? danoFuego : 0f);
    }

    void MoverPersonaje(float movX)
    {
        if (controller != null && controller.enabled)
        {
            velocidadY += gravedad * Time.deltaTime;
            Vector3 movimientoFinal = new Vector3(movX * velocidadCaminar, velocidadY, 0);
            controller.Move(movimientoFinal * Time.deltaTime);
        }
    }

    void ProcesarImpactoAtaque()
    {
        if (oponente == null) BuscarOponente();
        if (oponente != null)
        {
            if (Mathf.Abs(transform.position.x - oponente.transform.position.x) <= rangoAtaque)
            {
                oponente.RecibirGolpe(danoAtaque); 
            }
        }
    }

    public void RecibirGolpe(float cantidadDano)
    {
        if (cantidadDano <= 0f) return;

        // Si estamos en LAN y golpeamos al rival remoto, enviar el daño por red a su PC
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected && esRemotoEnRed)
        {
            NetworkManager.Instance.SendPacket(new PlayerDamagePacket
            {
                targetPlayerId = this.idMando,
                damageAmount = cantidadDano
            });
            return;
        }

        AplicarDanoLocal(cantidadDano);
    }

    public void RecibirGolpeDesdeRed(float cantidadDano)
    {
        AplicarDanoLocal(cantidadDano);
    }

    private void AplicarDanoLocal(float cantidadDano)
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

        StartCoroutine(EfectoImpactoVisual());

        if (vidaActual > 0f)
        {
            ReproducirAnimacionSegura("recibir_golpe", 0.05f);
            if (audioSource != null && sonidoRecibirGolpe != null) audioSource.PlayOneShot(sonidoRecibirGolpe);
        }
        else
        {
            derrotasAcumuladas++;
            if (derrotasAcumuladas >= 2) StartCoroutine(SecuenciaDerrotaFinal());
            else StartCoroutine(SecuenciaDerrotaRonda());
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

    IEnumerator SecuenciaDerrotaRonda()
    {
        estaDerrotado = true; controlesBloqueados = true;
        ReproducirAnimacionSegura("Agony", 0.05f);

        float duracionSonidoDerrota = 0f;
        if (audioSource != null && sonidoDerrota != null)
        {
            audioSource.PlayOneShot(sonidoDerrota);
            duracionSonidoDerrota = sonidoDerrota.length;
        }
        yield return new WaitForSeconds(Mathf.Max(duracionSonidoDerrota, 1.5f));

        float duracionSegundoSonido = 0f;
        if (audioSource != null && segundoSonido != null)
        {
            audioSource.PlayOneShot(segundoSonido);
            duracionSegundoSonido = segundoSonido.length;
        }
        yield return new WaitForSeconds(Mathf.Max(duracionSegundoSonido, 1.0f));

        CurarAlMaximo();
        if (oponente != null) oponente.CurarAlMaximo();
        estaDerrotado = false; controlesBloqueados = false;
    }

    IEnumerator SecuenciaDerrotaFinal()
    {
        estaDerrotado = true; 
        juegoTerminado = true; 
        controlesBloqueados = true;
        Time.timeScale = 1f;

        if (controller != null) controller.enabled = false;
        if (anim != null) anim.applyRootMotion = true;
        
        ReproducirAnimacionSegura(animacionDerrotaFinal, 0.05f);
        if (audioSource != null && sonidoDerrotaFinal != null) audioSource.PlayOneShot(sonidoDerrotaFinal);

        yield return new WaitForSeconds(1.5f);

        int idGanador = (idMando == 1) ? 2 : 1;
        if (ControladorFinMultijugador.Instancia != null)
        {
            ControladorFinMultijugador.Instancia.MostrarGanador(idGanador);
        }
    }

    public void CurarAlMaximo()
    {
        vidaActual = vidaMaxima;
        ActualizarBarraVida();
        ReproducirAnimacionSegura("idle_A", 0.1f);
    }

    void ActualizarBarraVida()
    {
        if (barraVidaUI != null) barraVidaUI.fillAmount = vidaActual / vidaMaxima;
        if (textoVidaUI != null) textoVidaUI.text = vidaActual.ToString("0") + "/" + vidaMaxima.ToString("0");
    }

    bool ReproducirAnimacionSegura(string nombreAnim, float duracionTransicion)
    {
        if (anim == null) return false;
        if (anim.HasState(0, Animator.StringToHash(nombreAnim)))
        {
            anim.CrossFade(nombreAnim, duracionTransicion);
            ultimaAnimacionReproducida = nombreAnim;
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
        int mando = usarControlesP1EnRed ? 1 : idMando;
        float val = 0f;
        try { val = Input.GetAxis("Horizontal_P" + mando); } catch { }

        if (Mathf.Abs(val) < 0.1f)
        {
            KeyCode dpadLeft = (mando == 1) ? KeyCode.Joystick1Button14 : KeyCode.Joystick2Button14;
            KeyCode dpadRight = (mando == 1) ? KeyCode.Joystick1Button15 : KeyCode.Joystick2Button15;
            if (Input.GetKey(dpadLeft)) val = -1f;
            if (Input.GetKey(dpadRight)) val = 1f;
        }

        if (mando == 1)
        {
            if (Input.GetKey(KeyCode.D)) val = 1f;
            else if (Input.GetKey(KeyCode.A)) val = -1f;
        }
        else if (mando == 2)
        {
            if (Input.GetKey(KeyCode.RightArrow)) val = 1f;
            else if (Input.GetKey(KeyCode.LeftArrow)) val = -1f;
        }
        return Mathf.Clamp(val, -1f, 1f);
    }

    bool DetectarPuño()
    {
        int mando = usarControlesP1EnRed ? 1 : idMando;
        return Input.GetKeyDown((mando == 1) ? KeyCode.Joystick1Button3 : KeyCode.Joystick2Button3) ||
               Input.GetKeyDown((mando == 1) ? KeyCode.Joystick1Button2 : KeyCode.Joystick2Button2) ||
               ((mando == 1) ? Input.GetKeyDown(KeyCode.J) : Input.GetKeyDown(KeyCode.Keypad1));
    }

    bool DetectarPatada()
    {
        int mando = usarControlesP1EnRed ? 1 : idMando;
        return Input.GetKeyDown((mando == 1) ? KeyCode.Joystick1Button0 : KeyCode.Joystick2Button0) ||
               ((mando == 1) ? Input.GetKeyDown(KeyCode.K) : Input.GetKeyDown(KeyCode.Keypad2));
    }

    bool DetectarSalto()
    {
        int mando = usarControlesP1EnRed ? 1 : idMando;
        float axisY = 0f;
        try { axisY = Input.GetAxisRaw("Vertical_P" + mando); } catch { }

        bool hayIntencionSalto = axisY > 0.4f || Input.GetKey((mando == 1) ? KeyCode.Joystick1Button12 : KeyCode.Joystick2Button12) || Input.GetKeyDown((mando == 1) ? KeyCode.Joystick1Button1 : KeyCode.Joystick2Button1) || ((mando == 1) ? Input.GetKeyDown(KeyCode.W) : Input.GetKeyDown(KeyCode.UpArrow));

        if (hayIntencionSalto && !saltoRegistrado) { saltoRegistrado = true; return true; }
        else if (!hayIntencionSalto && saltoRegistrado) { saltoRegistrado = false; }
        return false;
    }

    bool DetectarAbajo()
    {
        int mando = usarControlesP1EnRed ? 1 : idMando;
        float axisY = 0f;
        try { axisY = Input.GetAxisRaw("Vertical_P" + mando); } catch { }
        return axisY < -0.4f || Input.GetKey((mando == 1) ? KeyCode.Joystick1Button13 : KeyCode.Joystick2Button13) || ((mando == 1) ? Input.GetKey(KeyCode.S) : Input.GetKey(KeyCode.DownArrow));
    }
}