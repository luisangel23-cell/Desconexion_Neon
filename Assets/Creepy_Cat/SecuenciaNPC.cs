using UnityEngine;
using System.Collections;

public class SecuenciaNPC : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animatorNPC;
    public AudioSource audioSourceNPC;
    public AudioClip clipVoz;
    public Transform puntoDestino;
    public float velocidadCorrer = 4f; // Velocidad de avance ajustada

    [Header("Parámetros del Animator")]
    public string nombreAnimHablar = "Yelling";
    public string nombreAnimCorrer = "Running";

    void Start()
    {
        StartCoroutine(EjecutarSecuencia());
    }

    IEnumerator EjecutarSecuencia()
    {
        // 1. Hablar y reproducir audio
        float tiempoEspera = 3f; // Tiempo por defecto por si no hay audio

        if (animatorNPC != null)
        {
            animatorNPC.Play(nombreAnimHablar);
        }

        if (audioSourceNPC != null && clipVoz != null)
        {
            audioSourceNPC.PlayOneShot(clipVoz);
            tiempoEspera = clipVoz.length; // Ajustamos la espera a la duración exacta del audio
        }

        // BUCLE DE HABLAR: Mantiene la animación viva hasta que termine el tiempo del audio
        float tiempoHablando = 0f;
        while (tiempoHablando < tiempoEspera)
        {
            if (animatorNPC != null)
            {
                // Rebobinamos la animación de hablar si está a punto de terminar y congelarse
                AnimatorStateInfo estadoAnimHablar = animatorNPC.GetCurrentAnimatorStateInfo(0);
                if (estadoAnimHablar.IsName(nombreAnimHablar) && estadoAnimHablar.normalizedTime >= 0.95f)
                {
                    animatorNPC.Play(nombreAnimHablar, 0, 0f); 
                }
            }
            
            tiempoHablando += Time.deltaTime;
            yield return null; // Espera al siguiente frame
        }

        // 2. Cambiar a correr
        if (animatorNPC != null)
        {
            animatorNPC.Play(nombreAnimCorrer);
        }

        // 3. Mover hacia el destino de forma suave
        if (puntoDestino != null)
        {
            float tiempoMaximoCorriendo = 15f; 
            float cronometro = 0f;

            while (Vector3.Distance(transform.position, puntoDestino.position) > 0.3f && cronometro < tiempoMaximoCorriendo)
            {
                // HACK 1: Rebobinamos la animación milisegundos antes de que dé el paso hacia atrás o se congele
                AnimatorStateInfo estadoAnimCorrer = animatorNPC.GetCurrentAnimatorStateInfo(0);
                if (estadoAnimCorrer.IsName(nombreAnimCorrer) && estadoAnimCorrer.normalizedTime >= 0.95f)
                {
                    animatorNPC.Play(nombreAnimCorrer, 0, 0f); // Rebobina la animación a 0 forzando el bucle limpio
                }

                // Mover hacia el punto destino
                transform.position = Vector3.MoveTowards(transform.position, puntoDestino.position, velocidadCorrer * Time.deltaTime);

                // Rotar suavemente hacia el destino para que no se vea tieso
                Vector3 direccion = (puntoDestino.position - transform.position).normalized;
                if (direccion != Vector3.zero)
                {
                    Quaternion rotacionDeseada = Quaternion.LookRotation(new Vector3(direccion.x, 0, direccion.z));
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, 10f * Time.deltaTime);
                }

                cronometro += Time.deltaTime;
                yield return null;
            }
        }

        // 4. Desaparecer al llegar al punto exacto
        gameObject.SetActive(false);
    }

    // HACK 2: Al poner esta función vacía aquí abajo, interceptamos el motor de físicas de Unity.
    // Esto destruye matemáticamente cualquier intento del archivo de animación de jalar al personaje hacia atrás.
    void OnAnimatorMove()
    {
        // Se deja completamente vacío a propósito.
    }
}