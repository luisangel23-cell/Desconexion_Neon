using UnityEngine;
using System.Collections;
using UnityEngine.Playables;
using UnityEngine.SceneManagement; 

public class CaminarYHablarPostTimeline : MonoBehaviour
{
    [Header("Conexión con Timeline")]
    public PlayableDirector directorTimeline;

    [Header("Referencias del Personaje NPC")]
    public Animator animatorNPC;
    public AudioSource audioSourceNPC;
    public AudioClip clipVoz;
    public Transform puntoDestino;
    public float velocidadCaminar = 2.5f;
    public string nombreAnimCaminar = "Walking";
    public bool desaparecerAlLlegar = true;

    [Header("Monstruo - Animación 1")]
    public GameObject monstruo;
    public Animator animatorMonstruo;
    public string nombreAnimMonstruo = "Falling";
    public AudioSource audioSourceMonstruo;
    public AudioClip clipSonidoMonstruo;
    public float tiempoEsperaAnim1 = 1.0f;

    [Header("Monstruo - Animación 2")]
    public string stringAnimMonstruo2 = "Roar";
    public AudioClip clipSonidoMonstruo2;
    public float retardoAudio2 = 0.3f;
    public float tiempoEsperaAnim2 = 1.2f;

    [Header("Monstruo - Animación 3 y Caída de Objeto 1")]
    public string nombreAnimMonstruo3 = "Throw";
    public AudioClip clipSonidoMonstruo3;
    public GameObject objetoACaer;
    public Transform puntoLanzamiento;
    public Transform puntoCaidaObjeto;
    public float retardoSoltarObjeto = 0.4f;
    public float duracionCaidaObjeto = 0.7f;
    public Transform puntoAparicionMonstruo;
    public AudioClip clipSonidoLanzamiento;
    public AudioSource audioSourceLanzamiento;

    [Header("Secuencia del Personaje: Reacción + Hablar")]
    public Animator animatorPersonajeReaccion;
    public string nombreAnimReaccionPersonaje = "Scared";
    public float duracionAnimReaccion = 1.5f;
    public string nombreAnimHablarPersonaje = "Talking";
    public AudioClip clipVozPersonajeReaccion;
    public AudioSource audioSourcePersonajeReaccion;

    [Header("Monstruo - Animación 4 y Caída de Objeto 2")]
    public string nombreAnimMonstruo4 = "Throw2";
    public AudioClip clipSonidoMonstruo4;
    public GameObject objetoACaer2;
    public Transform puntoLanzamiento2;
    public Transform puntoCaidaObjeto2;
    public float retardoSoltarObjeto2 = 0.4f;
    public float duracionCaidaObjeto2 = 0.7f;
    public AudioClip clipSonidoLanzamiento2;

    [Header("Reacción Final al Objeto 2")]
    public string nombreAnimReaccionObjeto2 = "Hit";
    public AudioClip clipSonidoImpacto2;

    [Header("Fase Final: Monstruo 5 y Secuaces")]
    public string nombreAnimMonstruo5 = "Roar2";
    public AudioClip clipSonidoMonstruo5;
    public GameObject[] monstruosSecuaces;
    public string nombreAnimSecuaces = "Spawn";
    public float duracionAnimSpawnSecuaces = 2.0f;

    [Header("Fase de Reacción del Monstruo al Morir los Secuaces")]
    public string nombreAnimMonstruoVictoriaSecuaces = "Roar"; 
    public AudioClip clipSonidoMonstruoVictoriaSecuaces;
    public float duracionReaccionMonstruo = 2.0f;

    [Header("Fase de Diálogo y Mirada Directa al Personaje")]
    [Tooltip("El objeto del personaje (ej: Survivalist (1)) para que la cámara lo mire fijo")]
    public Transform objetivoPersonajeMirar;
    public Animator animatorPersonajeDialogo; 
    public string nombreAnimHablarFinal = "Talking";
    public AudioClip clipAudioDialogoFinal;
    public AudioSource audioSourceDialogoFinal;

    [Header("Fase de Persecución y Ataque de Zombies")]
    public Transform jugador;
    public string nombreAnimMonstruoLoop = "Idle";
    public string nombreAnimSecuacesCaminar = "Zombie Running";
    public string nombreAnimSecuacesAtacar = "Zombie Attack";
    public float distanciaParaAtacar = 2.5f;
    public AudioClip clipSonidoSecuacesPersecucion;
    public float velocidadMinimaSecuaces = 2.0f;
    public float velocidadMaximaSecuaces = 4.5f;
    public float ajusteAlturaZombies = 0f;

    [Header("Sistema de Combate del Jugador (Golpear Zombies)")]
    public Animator animatorJugadorAtacar;
    public string nombreAnimGolpeJugador = "bp_hook_right_A";
    public float rangoDeGolpe = 3.0f;
    public int golpesParaMatarZombie = 3;
    public string nombreAnimZombieMuerto = "Zombie Death";
    public AudioClip clipSonidoGolpePuch;

    [Header("Sistema de Vida y Muerte del Jugador")]
    public int golpesMaximos = 5;
    public float tiempoEntreGolpes = 1.5f;
    public AudioClip clipSonidoGolpeJugador;
    public AudioClip clipSonidoMuerte;
    public GameObject pantallaMuerteUI;

    // Variables internas
    private float[] velocidadesGeneradas;
    private bool[] secuacesAtacando;
    private float[] timersAtaque;
    private int[] vidaSecuaces;
    private bool[] secuacesMuertos;
    private int zombiesMuertosContador = 0;
    private int golpesRecibidos = 0;
    private bool jugadorMuerto = false;
    private bool juegoIniciadoFaseCombate = false;
    private bool reaccionJefeActivada = false;

    private void OnEnable()
    {
        if (directorTimeline != null)
            directorTimeline.stopped += OnTimelineFinalizado;
    }

    private void OnDisable()
    {
        if (directorTimeline != null)
            directorTimeline.stopped -= OnTimelineFinalizado;
    }

    private void OnTimelineFinalizado(PlayableDirector director)
    {
        if (director == directorTimeline)
            StartCoroutine(EjecutarCaminataYAudio());
    }

    IEnumerator EjecutarCaminataYAudio()
    {
        if (audioSourceNPC != null && clipVoz != null)
            audioSourceNPC.PlayOneShot(clipVoz);

        if (animatorNPC != null)
            animatorNPC.Play(nombreAnimCaminar);

        if (puntoDestino != null)
        {
            float tiempoMaximoCaminando = 20f;
            float cronometro = 0f;

            while (Vector3.Distance(transform.position, puntoDestino.position) > 0.3f && cronometro < tiempoMaximoCaminando)
            {
                if (animatorNPC != null)
                {
                    AnimatorStateInfo estadoAnim = animatorNPC.GetCurrentAnimatorStateInfo(0);
                    if (estadoAnim.IsName(nombreAnimCaminar) && estadoAnim.normalizedTime >= 0.95f)
                        animatorNPC.Play(nombreAnimCaminar, 0, 0f);
                }

                Vector3 siguientePosicion = Vector3.MoveTowards(transform.position, puntoDestino.position, velocidadCaminar * Time.deltaTime);

                RaycastHit hit;
                if (Physics.Raycast(siguientePosicion + Vector3.up * 2f, Vector3.down, out hit, 10f))
                    siguientePosicion.y = hit.point.y;

                transform.position = siguientePosicion;

                Vector3 direccion = (puntoDestino.position - transform.position).normalized;
                if (direccion != Vector3.zero)
                {
                    Quaternion rotacionDeseada = Quaternion.LookRotation(new Vector3(direccion.x, 0, direccion.z));
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, 8f * Time.deltaTime);
                }

                cronometro += Time.deltaTime;
                yield return null;
            }
        }

        StartCoroutine(EjecutarSecuenciaMonstruo());

        if (desaparecerAlLlegar)
            gameObject.SetActive(false);
    }

    IEnumerator EjecutarSecuenciaMonstruo()
    {
        if (monstruo != null && monstruo != this.gameObject)
        {
            if (puntoAparicionMonstruo != null)
            {
                monstruo.transform.position = puntoAparicionMonstruo.position;
                monstruo.transform.rotation = puntoAparicionMonstruo.rotation;
            }

            monstruo.SetActive(true);

            if (animatorMonstruo != null && !string.IsNullOrEmpty(nombreAnimMonstruo))
                animatorMonstruo.Play(nombreAnimMonstruo);

            if (audioSourceMonstruo != null && clipSonidoMonstruo != null)
                audioSourceMonstruo.PlayOneShot(clipSonidoMonstruo);

            StartCoroutine(ReproducirSegundoAudio());
            yield return new WaitForSeconds(tiempoEsperaAnim1);

            if (animatorMonstruo != null && !string.IsNullOrEmpty(stringAnimMonstruo2))
                animatorMonstruo.Play(stringAnimMonstruo2);

            yield return new WaitForSeconds(tiempoEsperaAnim2);

            if (animatorMonstruo != null && !string.IsNullOrEmpty(nombreAnimMonstruo3))
                animatorMonstruo.Play(nombreAnimMonstruo3);

            if (audioSourceMonstruo != null && clipSonidoMonstruo3 != null)
                audioSourceMonstruo.PlayOneShot(clipSonidoMonstruo3);

            yield return new WaitForSeconds(retardoSoltarObjeto);

            if (objetoACaer != null && puntoCaidaObjeto != null)
                StartCoroutine(MoverPrimerObjetoYReaccion());
        }
    }

    IEnumerator ReproducirSegundoAudio()
    {
        yield return new WaitForSeconds(retardoAudio2);
        if (audioSourceMonstruo != null && clipSonidoMonstruo2 != null)
            audioSourceMonstruo.PlayOneShot(clipSonidoMonstruo2);
    }

    IEnumerator MoverPrimerObjetoYReaccion()
    {
        objetoACaer.SetActive(true);
        Vector3 posInicio = (puntoLanzamiento != null) ? puntoLanzamiento.position : monstruo.transform.position;
        Vector3 posFinal = puntoCaidaObjeto.position;
        objetoACaer.transform.position = posInicio;

        if (clipSonidoLanzamiento != null)
        {
            if (audioSourceLanzamiento != null)
                audioSourceLanzamiento.PlayOneShot(clipSonidoLanzamiento);
            else
                AudioSource.PlayClipAtPoint(clipSonidoLanzamiento, posInicio);
        }

        float tiempoPasado = 0f;
        while (tiempoPasado < duracionCaidaObjeto)
        {
            tiempoPasado += Time.deltaTime;
            float porcentaje = tiempoPasado / duracionCaidaObjeto;
            Vector3 posActual = Vector3.Lerp(posInicio, posFinal, porcentaje);
            posActual.y += Mathf.Sin(porcentaje * Mathf.PI) * 1.5f; 
            objetoACaer.transform.position = posActual;
            yield return null;
        }
        objetoACaer.transform.position = posFinal;

        float tiempoExactoDelAudio = 0f;

        if (animatorPersonajeReaccion != null)
        {
            if (!animatorPersonajeReaccion.gameObject.activeSelf)
                animatorPersonajeReaccion.gameObject.SetActive(true);

            if (!string.IsNullOrEmpty(nombreAnimReaccionPersonaje))
                animatorPersonajeReaccion.Play(nombreAnimReaccionPersonaje);

            yield return new WaitForSeconds(duracionAnimReaccion);

            if (!string.IsNullOrEmpty(nombreAnimHablarPersonaje))
                animatorPersonajeReaccion.Play(nombreAnimHablarPersonaje);

            if (clipVozPersonajeReaccion != null)
            {
                tiempoExactoDelAudio = clipVozPersonajeReaccion.length; 
                
                if (audioSourcePersonajeReaccion != null)
                    audioSourcePersonajeReaccion.PlayOneShot(clipVozPersonajeReaccion);
                else
                    AudioSource.PlayClipAtPoint(clipVozPersonajeReaccion, animatorPersonajeReaccion.transform.position);
            }
        }

        yield return new WaitForSeconds(tiempoExactoDelAudio);

        if (animatorMonstruo != null && !string.IsNullOrEmpty(nombreAnimMonstruo4))
            animatorMonstruo.Play(nombreAnimMonstruo4);

        if (audioSourceMonstruo != null && clipSonidoMonstruo4 != null)
            audioSourceMonstruo.PlayOneShot(clipSonidoMonstruo4);

        yield return new WaitForSeconds(retardoSoltarObjeto2);

        if (objetoACaer2 != null && puntoCaidaObjeto2 != null)
            StartCoroutine(MoverSegundoObjeto());
    }

    IEnumerator MoverSegundoObjeto()
    {
        objetoACaer2.SetActive(true);
        Vector3 posInicio = (puntoLanzamiento2 != null) ? puntoLanzamiento2.position : monstruo.transform.position;
        Vector3 posFinal = puntoCaidaObjeto2.position;
        objetoACaer2.transform.position = posInicio;

        if (clipSonidoLanzamiento2 != null)
        {
            if (audioSourceLanzamiento != null)
                audioSourceLanzamiento.PlayOneShot(clipSonidoLanzamiento2);
            else
                AudioSource.PlayClipAtPoint(clipSonidoLanzamiento2, posInicio);
        }

        float tiempoPasado = 0f;
        while (tiempoPasado < duracionCaidaObjeto2)
        {
            tiempoPasado += Time.deltaTime;
            float porcentaje = tiempoPasado / duracionCaidaObjeto2;
            Vector3 posActual = Vector3.Lerp(posInicio, posFinal, porcentaje);
            posActual.y += Mathf.Sin(porcentaje * Mathf.PI) * 1.5f; 
            objetoACaer2.transform.position = posActual;
            yield return null;
        }
        
        objetoACaer2.transform.position = posFinal;

        float tiempoEsperaFaseFinal = 1.0f;

        if (clipSonidoImpacto2 != null)
        {
            tiempoEsperaFaseFinal = clipSonidoImpacto2.length;
            if (audioSourcePersonajeReaccion != null)
                audioSourcePersonajeReaccion.PlayOneShot(clipSonidoImpacto2);
            else
                AudioSource.PlayClipAtPoint(clipSonidoImpacto2, posFinal);
        }

        if (animatorPersonajeReaccion != null && !string.IsNullOrEmpty(nombreAnimReaccionObjeto2))
        {
            animatorPersonajeReaccion.Play(nombreAnimReaccionObjeto2);
        }

        yield return new WaitForSeconds(tiempoEsperaFaseFinal);

        if (animatorMonstruo != null && !string.IsNullOrEmpty(nombreAnimMonstruo5))
            animatorMonstruo.Play(nombreAnimMonstruo5);

        if (audioSourceMonstruo != null && clipSonidoMonstruo5 != null)
            audioSourceMonstruo.PlayOneShot(clipSonidoMonstruo5);

        if (monstruosSecuaces != null && monstruosSecuaces.Length > 0)
        {
            foreach (GameObject secuaz in monstruosSecuaces)
            {
                if (secuaz != null)
                {
                    secuaz.SetActive(true);
                    Animator animSecuaz = secuaz.GetComponent<Animator>();
                    if (animSecuaz != null && !string.IsNullOrEmpty(nombreAnimSecuaces))
                    {
                        animSecuaz.Play(nombreAnimSecuaces);
                    }
                }
            }
        }

        yield return new WaitForSeconds(duracionAnimSpawnSecuaces);

        if (animatorMonstruo != null && !string.IsNullOrEmpty(nombreAnimMonstruoLoop))
            animatorMonstruo.Play(nombreAnimMonstruoLoop);

        if (jugador != null && monstruosSecuaces != null && monstruosSecuaces.Length > 0)
        {
            velocidadesGeneradas = new float[monstruosSecuaces.Length];
            secuacesAtacando = new bool[monstruosSecuaces.Length];
            timersAtaque = new float[monstruosSecuaces.Length];
            vidaSecuaces = new int[monstruosSecuaces.Length];
            secuacesMuertos = new bool[monstruosSecuaces.Length];
            zombiesMuertosContador = 0;

            for (int i = 0; i < monstruosSecuaces.Length; i++)
            {
                GameObject secuaz = monstruosSecuaces[i];
                if (secuaz != null)
                {
                    velocidadesGeneradas[i] = Random.Range(velocidadMinimaSecuaces, velocidadMaximaSecuaces);
                    secuacesAtacando[i] = false;
                    timersAtaque[i] = 0f;
                    vidaSecuaces[i] = golpesParaMatarZombie; 
                    secuacesMuertos[i] = false;

                    Animator animSecuaz = secuaz.GetComponent<Animator>();
                    if (animSecuaz != null && !string.IsNullOrEmpty(nombreAnimSecuacesCaminar))
                    {
                        animSecuaz.Play(nombreAnimSecuacesCaminar);
                    }

                    if (clipSonidoSecuacesPersecucion != null)
                    {
                        AudioSource audioSecuaz = secuaz.GetComponent<AudioSource>();
                        if (audioSecuaz == null) audioSecuaz = secuaz.AddComponent<AudioSource>();
                        audioSecuaz.clip = clipSonidoSecuacesPersecucion;
                        audioSecuaz.loop = true;
                        audioSecuaz.spatialBlend = 1.0f;
                        audioSecuaz.Play();
                    }
                }
            }

            juegoIniciadoFaseCombate = true; 
            StartCoroutine(PerseguirAlJugador());
        }
    }

    private void Update()
    {
        if (juegoIniciadoFaseCombate && !jugadorMuerto && Input.GetMouseButtonDown(0))
        {
            GolpearZombie();
        }
    }

    void GolpearZombie()
    {
        if (animatorJugadorAtacar != null)
        {
            animatorJugadorAtacar.SetTrigger("Golpear");
        }

        if (clipSonidoGolpePuch != null)
        {
            AudioSource.PlayClipAtPoint(clipSonidoGolpePuch, jugador.position, 1.0f);
        }

        if (monstruosSecuaces != null)
        {
            for (int i = 0; i < monstruosSecuaces.Length; i++)
            {
                GameObject secuaz = monstruosSecuaces[i];
                if (secuaz != null && !secuacesMuertos[i] && secuaz.activeSelf)
                {
                    float distanciaAlJugador = Vector3.Distance(secuaz.transform.position, jugador.position);

                    if (distanciaAlJugador <= rangoDeGolpe)
                    {
                        vidaSecuaces[i]--; 
                        Debug.Log("¡Golpeaste a un zombie! Vida restante: " + vidaSecuaces[i]);

                        if (vidaSecuaces[i] <= 0)
                        {
                            StartCoroutine(MatarSecuaz(i));
                        }
                        break; 
                    }
                }
            }
        }
    }

    IEnumerator MatarSecuaz(int indice)
    {
        secuacesMuertos[indice] = true;
        zombiesMuertosContador++; 
        Debug.Log("Zombies muertos: " + zombiesMuertosContador + " de " + monstruosSecuaces.Length);

        GameObject secuaz = monstruosSecuaces[indice];

        if (secuaz != null)
        {
            AudioSource audioSecuaz = secuaz.GetComponent<AudioSource>();
            if (audioSecuaz != null) audioSecuaz.Stop();

            Animator animSecuaz = secuaz.GetComponent<Animator>();
            if (animSecuaz != null && !string.IsNullOrEmpty(nombreAnimZombieMuerto))
            {
                animSecuaz.Play(nombreAnimZombieMuerto);
            }

            yield return new WaitForSeconds(2.0f);
            secuaz.SetActive(false);
        }

        if (zombiesMuertosContador >= monstruosSecuaces.Length && !reaccionJefeActivada)
        {
            reaccionJefeActivada = true;
            StartCoroutine(SecuenciaFinalCamaraDirecta());
        }
    }

    IEnumerator SecuenciaFinalCamaraDirecta()
    {
        juegoIniciadoFaseCombate = false; 
        Debug.Log("¡Secuaces eliminados! El monstruo ruge y la cámara gira hacia el personaje...");

        // 1. El monstruo hace su animación y sonido
        if (monstruo != null)
        {
            if (animatorMonstruo != null && !string.IsNullOrEmpty(nombreAnimMonstruoVictoriaSecuaces))
            {
                animatorMonstruo.Play(nombreAnimMonstruoVictoriaSecuaces);
            }

            if (clipSonidoMonstruoVictoriaSecuaces != null)
            {
                if (audioSourceMonstruo != null)
                    audioSourceMonstruo.PlayOneShot(clipSonidoMonstruoVictoriaSecuaces);
                else
                    AudioSource.PlayClipAtPoint(clipSonidoMonstruoVictoriaSecuaces, monstruo.transform.position, 1.0f);
            }
        }

        yield return new WaitForSeconds(duracionReaccionMonstruo);

        // 2. Desactivamos temporalmente el script de movimiento del First Person Controller para que no pelee con la cámara
        MonoBehaviour scriptMovimiento = jugador.GetComponent<MonoBehaviour>(); // O el script exacto de movimiento si lo conoces
        if (scriptMovimiento != null) scriptMovimiento.enabled = false;

        Camera camaraPrincipal = Camera.main;
        if (camaraPrincipal != null && objetivoPersonajeMirar != null)
        {
            Quaternion rotacionInicialCamara = camaraPrincipal.transform.rotation;
            
            // Calculamos hacia dónde debe mirar la cámara para enfocar al personaje
            Vector3 direccionHaciaPersonaje = (objetivoPersonajeMirar.position - camaraPrincipal.transform.position).normalized;
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionHaciaPersonaje);

            // Transición suave de la cámara hacia el personaje en 1 segundo
            float tiempoGiro = 0f;
            while (tiempoGiro < 1.0f)
            {
                tiempoGiro += Time.deltaTime;
                camaraPrincipal.transform.rotation = Quaternion.Slerp(rotacionInicialCamara, rotacionObjetivo, tiempoGiro / 1.0f);
                yield return null;
            }
        }

        float duracionDialogo = 3.0f;

        // 3. El personaje hace su animación de hablar y reproduce su audio
        if (animatorPersonajeDialogo != null && !string.IsNullOrEmpty(nombreAnimHablarFinal))
        {
            animatorPersonajeDialogo.Play(nombreAnimHablarFinal);
        }

        if (clipAudioDialogoFinal != null)
        {
            duracionDialogo = clipAudioDialogoFinal.length;
            if (audioSourceDialogoFinal != null)
            {
                audioSourceDialogoFinal.PlayOneShot(clipAudioDialogoFinal);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clipAudioDialogoFinal, objetivoPersonajeMirar.position, 1.0f);
            }
        }

        yield return new WaitForSeconds(duracionDialogo);

        // 4. Devolvemos el control y la normalidad al jugador
        if (scriptMovimiento != null) scriptMovimiento.enabled = true;

        Debug.Log("¡Secuencia final completada, control devuelto al jugador!");
    }

    IEnumerator PerseguirAlJugador()
    {
        while (true)
        {
            if (jugadorMuerto || reaccionJefeActivada) yield break;

            if (jugador != null && monstruosSecuaces != null)
            {
                for (int i = 0; i < monstruosSecuaces.Length; i++)
                {
                    if (secuacesMuertos[i]) continue;

                    GameObject secuaz = monstruosSecuaces[i];
                    if (secuaz != null && secuaz.activeSelf)
                    {
                        Vector3 direccion = (jugador.position - secuaz.transform.position).normalized;
                        direccion.y = 0;
                        if (direccion != Vector3.zero)
                        {
                            Quaternion rotacionDeseada = Quaternion.LookRotation(direccion);
                            secuaz.transform.rotation = Quaternion.Slerp(secuaz.transform.rotation, rotacionDeseada, 8f * Time.deltaTime);
                        }

                        float distancia = Vector3.Distance(secuaz.transform.position, jugador.position);
                        Animator animSecuaz = secuaz.GetComponent<Animator>();

                        if (distancia <= distanciaParaAtacar || secuacesAtacando[i])
                        {
                            if (!secuacesAtacando[i])
                            {
                                secuacesAtacando[i] = true;
                                timersAtaque[i] = 0.5f; 
                                if (animSecuaz != null && !string.IsNullOrEmpty(nombreAnimSecuacesAtacar))
                                {
                                    animSecuaz.Play(nombreAnimSecuacesAtacar);
                                }
                            }
                            else
                            {
                                timersAtaque[i] -= Time.deltaTime;
                                if (timersAtaque[i] <= 0f)
                                {
                                    RecibirGolpe();
                                    timersAtaque[i] = tiempoEntreGolpes; 
                                }
                            }
                        }
                        else
                        {
                            float miVelocidad = velocidadesGeneradas[i];
                            Vector3 siguientePos = Vector3.MoveTowards(secuaz.transform.position, jugador.position, miVelocidad * Time.deltaTime);
                            
                            RaycastHit hit;
                            if (Physics.Raycast(siguientePos + Vector3.up * 4f, Vector3.down, out hit, 15f))
                            {
                                siguientePos.y = hit.point.y + ajusteAlturaZombies;
                            }
                            
                            secuaz.transform.position = siguientePos;
                        }
                    }
                }
            }
            yield return null;
        }
    }

    void RecibirGolpe()
    {
        if (jugadorMuerto || reaccionJefeActivada) return;

        golpesRecibidos++;
        Debug.Log("¡Te golpearon! Llevas: " + golpesRecibidos + " de " + golpesMaximos);

        Vector3 posCamara = Camera.main != null ? Camera.main.transform.position : jugador.position;

        if (golpesRecibidos >= golpesMaximos)
        {
            if (clipSonidoMuerte != null)
            {
                AudioSource.PlayClipAtPoint(clipSonidoMuerte, posCamara, 1.0f);
            }
            StartCoroutine(RutinaMuerte());
        }
        else
        {
            if (clipSonidoGolpeJugador != null)
            {
                AudioSource.PlayClipAtPoint(clipSonidoGolpeJugador, posCamara, 1.0f);
            }
        }
    }

    IEnumerator RutinaMuerte()
    {
        jugadorMuerto = true;
        
        if (pantallaMuerteUI != null)
        {
            pantallaMuerteUI.SetActive(true);
        }

        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnAnimatorMove() { }
}