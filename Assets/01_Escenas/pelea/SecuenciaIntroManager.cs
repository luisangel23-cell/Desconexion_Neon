using UnityEngine;
using System.Collections;

public class SecuenciaIntroManager : MonoBehaviour
{
    [Header("Referencias")]
    public Camera camaraPrincipal;
    public GeneradorPeleadores generadorPeleadores;

    [Header("Cámara de Pelea General")]
    public Vector3 posicionCamaraPelea;
    public Vector3 rotacionCamaraPelea;

    private IEnumerator Start()
    {
        // Guardar la posición inicial de combate si no se asignó manualmente
        if (posicionCamaraPelea == Vector3.zero)
        {
            posicionCamaraPelea = camaraPrincipal.transform.position;
            rotacionCamaraPelea = camaraPrincipal.transform.rotation.eulerAngles;
        }

        // Esperar un frame a que el Generador cree los luchadores en escena
        yield return null;

        GameObject p1 = generadorPeleadores.instanciaP1;
        GameObject p2 = generadorPeleadores.instanciaP2;

        if (p1 == null || p2 == null) yield break;

        // 1. Desactivar scripts de movimiento/ataque durante las intros
        ControladorCombate ctrl1 = p1.GetComponent<ControladorCombate>();
        ControladorCombate ctrl2 = p2.GetComponent<ControladorCombate>();

        if (ctrl1) ctrl1.enabled = false;
        if (ctrl2) ctrl2.enabled = false;

        DatosIntro introP1 = p1.GetComponent<DatosIntro>();
        DatosIntro introP2 = p2.GetComponent<DatosIntro>();

        // 2. Intro Jugador 1
        if (introP1 != null)
        {
            MoverCamaraInmediato(introP1.puntoCamaraRostro);
            ReproducirIntro(p1, introP1);
            yield return new WaitForSeconds(introP1.duracionIntro);
        }

        // 3. Intro Jugador 2
        if (introP2 != null)
        {
            MoverCamaraInmediato(introP2.puntoCamaraRostro);
            ReproducirIntro(p2, introP2);
            yield return new WaitForSeconds(introP2.duracionIntro);
        }

        // 4. Transición suave de la cámara de regreso al plano central de pelea
        yield return StartCoroutine(TransicionCamara(posicionCamaraPelea, Quaternion.Euler(rotacionCamaraPelea), 1.2f));

        // 5. Iniciar la pelea activando los controles
        if (ctrl1) ctrl1.enabled = true;
        if (ctrl2) ctrl2.enabled = true;
    }

    void MoverCamaraInmediato(Transform puntoTarget)
    {
        if (puntoTarget != null)
        {
            camaraPrincipal.transform.position = puntoTarget.position;
            camaraPrincipal.transform.rotation = puntoTarget.rotation;
        }
    }

    void ReproducirIntro(GameObject personaje, DatosIntro datos)
    {
        Animator anim = personaje.GetComponent<Animator>();
        AudioSource audio = personaje.GetComponent<AudioSource>();

        if (anim != null && !string.IsNullOrEmpty(datos.nombreAnimacionIntro))
        {
            anim.CrossFade(datos.nombreAnimacionIntro, 0.1f);
        }

        if (audio != null && datos.vozIntro != null)
        {
            audio.PlayOneShot(datos.vozIntro);
        }
    }

    IEnumerator TransicionCamara(Vector3 destinoPos, Quaternion destinoRot, float duracion)
    {
        Vector3 origenPos = camaraPrincipal.transform.position;
        Quaternion origenRot = camaraPrincipal.transform.rotation;
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, tiempo / duracion);

            camaraPrincipal.transform.position = Vector3.Lerp(origenPos, destinoPos, t);
            camaraPrincipal.transform.rotation = Quaternion.Lerp(origenRot, destinoRot, t);
            yield return null;
        }

        camaraPrincipal.transform.position = destinoPos;
        camaraPrincipal.transform.rotation = destinoRot;
    }
}