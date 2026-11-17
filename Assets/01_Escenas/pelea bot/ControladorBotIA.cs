using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class ControladorBotIA : MonoBehaviour
{
    private Animator anim;
    private CharacterController controller;
    private AudioSource audioSource;
    private ControladorJugadorVsBot oponente;

    [Header("Ajustes de Vida y UI")]
    public float vidaMaxima = 100f;
    private float vidaActual;
    public Image barraVidaUI; 
    public Text textoVidaUI; 

    [Header("Ajustes de Movimiento y IA")]
    public float velocidadCaminar = 4.5f; 
    public float gravedad = -22f;
    private float velocidadY = 0f;
    public float rangoAtaque = 4.0f; 
    public float danoAtaque = 5f; 

    [Header("Control de Ritmo y Combos del Bot")]
    [Tooltip("Tiempo mínimo que el bot espera para volver a atacar después de golpear")]
    public float tiempoEsperaEntreAtaques = 1.0f; 
    private float siguienteTiempoPermitidoAtaque = 0f;

    [Header("Efectos de Sonido")]
    public AudioClip sonidoPuño;
    public AudioClip sonidoPatada;
    public AudioClip sonidoRecibirGolpe;
    public AudioClip sonidoDerrotaFinal; 

    [Header("Mecánicas Avanzadas (Dash y Stun)")]
    public float fuerzaDash = 7f; 
    public float duracionDash = 0.25f;
    public string animacionDashAtras = "Quickstep_B";
    public float duracionHitStun = 0.4f;

    [Header("Configuración de Animaciones")]
    public string animacionDerrotaFinal = "Falling Back Death";
    public string[] ataquesPuño = { "puño", "hp_straight_A", "hp_upper_right_A", "bp_upper_left_A", "bp_hook_right_A" };
    public string[] ataquesPatada = { "hk_rh_right_A", "lk_rh_right_A", "lk_rh_left_A" };

    private bool estaDerrotado = false;
    private bool estaHaciendoAccion = false;
    private float tiempoFinHitStun = 0f;
    private float temporizadorDecision = 0f;

    void Start()
    {
        estaDerrotado = false;
        estaHaciendoAccion = false;

        anim = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null) audioSource.playOnAwake = false;

        vidaActual = vidaMaxima;
        ActualizarBarraVida();
        BuscarOponente();
    }

    void BuscarOponente()
    {
        oponente = FindObjectOfType<ControladorJugadorVsBot>();
    }

    void Update()
    {
        if (oponente == null) { BuscarOponente(); return; }

        if (estaDerrotado || vidaActual <= 0) 
        {
            MoverPersonaje(0f);
            return;
        }

        if (Time.time < tiempoFinHitStun)
        {
            MoverPersonaje(0f);
            return;
        }

        bool enSuelo = controller.isGrounded;
        if (enSuelo && velocidadY < 0) velocidadY = -5f;

        RotarHaciaOponente();

        if (estaHaciendoAccion) 
        {
            MoverPersonaje(0f);
            return; 
        }

        if (Time.time >= temporizadorDecision)
        {
            TomarDecisionTactica();
            temporizadorDecision = Time.time + Random.Range(0.5f, 1.2f);
        }
    }

    void RotarHaciaOponente()
    {
        if (oponente == null) return;
        Vector3 dir = (oponente.transform.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            Quaternion rotObj = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotObj, 15f * Time.deltaTime);
        }
    }

    void TomarDecisionTactica()
    {
        if (oponente == null) return;
        
        float distancia = Vector3.Distance(transform.position, oponente.transform.position);
        float decision = Random.value; 

        if (distancia <= rangoAtaque)
        {
            if (Time.time >= siguienteTiempoPermitidoAtaque)
            {
                if (decision < 0.65f)
                {
                    StartCoroutine(EjecutarComboTactico());
                }
                else if (decision < 0.85f)
                {
                    StartCoroutine(EjecutarDashIA());
                }
                else
                {
                    StartCoroutine(EsperarIdle(0.5f));
                }
            }
            else
            {
                StartCoroutine(EsperarIdle(0.4f));
            }
        }
        else
        {
            if (decision < 0.80f)
            {
                StartCoroutine(CaminarHaciaJugador(Random.Range(0.8f, 1.5f)));
            }
            else
            {
                StartCoroutine(EsperarIdle(0.6f));
            }
        }
    }

    IEnumerator CaminarHaciaJugador(float duracion)
    {
        estaHaciendoAccion = true;
        float elapsed = 0f;
        ReproducirAnimacionSegura("caminar adelante", 0.1f);

        while (elapsed < duracion && oponente != null)
        {
            float dist = Vector3.Distance(transform.position, oponente.transform.position);
            if (dist <= rangoAtaque - 0.2f) break;

            float dirX = (oponente.transform.position.x > transform.position.x) ? 1f : -1f;
            MoverPersonaje(dirX); 
            elapsed += Time.deltaTime;
            yield return null;
        }

        ReproducirAnimacionSegura("idle_A", 0.1f);
        estaHaciendoAccion = false;
    }

    IEnumerator EsperarIdle(float duracion)
    {
        estaHaciendoAccion = true;
        ReproducirAnimacionSegura("idle_A", 0.1f);
        yield return new WaitForSeconds(duracion);
        estaHaciendoAccion = false;
    }

    IEnumerator EjecutarComboTactico()
    {
        estaHaciendoAccion = true;
        MoverPersonaje(0f);

        int numeroDeGolpes = Random.value > 0.5f ? 2 : 3;

        for (int i = 0; i < numeroDeGolpes; i++)
        {
            if (oponente == null) break;

            bool esPuño = Random.value > 0.4f;
            if (esPuño)
            {
                string animPuño = ObtenerAnimacionAleatoria(ataquesPuño);
                ReproducirAnimacionSegura(animPuño, 0.05f);
                if (sonidoPuño != null) audioSource.PlayOneShot(sonidoPuño);
            }
            else
            {
                string animPatada = ObtenerAnimacionAleatoria(ataquesPatada);
                ReproducirAnimacionSegura(animPatada, 0.05f);
                if (sonidoPatada != null) audioSource.PlayOneShot(sonidoPatada);
            }

            ProcesarImpactoAtaque();
            yield return new WaitForSeconds(0.35f); 
        }

        siguienteTiempoPermitidoAtaque = Time.time + tiempoEsperaEntreAtaques;

        yield return new WaitForSeconds(0.4f);
        ReproducirAnimacionSegura("idle_A", 0.1f);
        estaHaciendoAccion = false;
    }

    IEnumerator EjecutarDashIA()
    {
        estaHaciendoAccion = true;
        ReproducirAnimacionSegura(animacionDashAtras, 0.05f); 

        float dirAlejarseX = (oponente.transform.position.x > transform.position.x) ? -1f : 1f;
        
        float elapsed = 0f;
        while(elapsed < duracionDash && controller != null && controller.enabled)
        {
            controller.Move(new Vector3(dirAlejarseX * fuerzaDash * Time.deltaTime, -5f * Time.deltaTime, 0));
            elapsed += Time.deltaTime;
            yield return null;
        }

        ReproducirAnimacionSegura("idle_A", 0.05f);
        estaHaciendoAccion = false;
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
        if (oponente != null)
        {
            float distancia = Vector3.Distance(transform.position, oponente.transform.position);
            if (distancia <= rangoAtaque)
            {
                oponente.RecibirGolpe(danoAtaque); 
            }
        }
    }

    public void RecibirGolpe(float cantidadDano)
    {
        if (vidaActual <= 0f || estaDerrotado) return;

        vidaActual -= cantidadDano;
        if (vidaActual < 0f) vidaActual = 0f;
        ActualizarBarraVida();

        tiempoFinHitStun = Time.time + duracionHitStun;

        if (vidaActual > 0f)
        {
            ReproducirAnimacionSegura("recibir_golpe", 0.05f);
            if (audioSource != null && sonidoRecibirGolpe != null) audioSource.PlayOneShot(sonidoRecibirGolpe);
        }
        else
        {
            StartCoroutine(SecuenciaMuerteBotDirecta());
        }
    }

    IEnumerator SecuenciaMuerteBotDirecta()
    {
        estaDerrotado = true;
        estaHaciendoAccion = true;
        if (controller != null) controller.enabled = false;
        if (anim != null) anim.applyRootMotion = true;
        
        ReproducirAnimacionSegura(animacionDerrotaFinal, 0.05f);
        if (audioSource != null && sonidoDerrotaFinal != null) audioSource.PlayOneShot(sonidoDerrotaFinal);

        // Notificar victoria del jugador al gestor de fin de pelea
        if (ControladorFinPelea.Instancia != null)
        {
            ControladorFinPelea.Instancia.MostrarResultado(true);
        }
        
        yield break;
    }

    public void CurarAlMaximo()
    {
        vidaActual = vidaMaxima;
        ActualizarBarraVida();
        ReproducirAnimacionSegura("idle_A", 0.1f);
    }

    // --- BUSCADOR INFALIBLE DE CANVAS PARA EL BOT ---
    void ConectarUICanvas()
    {
        if (barraVidaUI == null)
        {
            Image[] imagenes = Resources.FindObjectsOfTypeAll<Image>();
            foreach (Image img in imagenes) { if (img.name == "Relleno_P2") barraVidaUI = img; }
        }
        if (textoVidaUI == null)
        {
            Text[] textos = Resources.FindObjectsOfTypeAll<Text>();
            foreach (Text txt in textos) { if (txt.name == "Texto_P2") textoVidaUI = txt; }
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
}