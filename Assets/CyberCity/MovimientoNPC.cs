using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MovimientoNPC : MonoBehaviour
{
    [Header("Destinos del NPC")]
    public Transform puntoDestino;      
    public Transform puntoSalida;       
    public float velocidad = 2f;

    [Header("UI de Objetivos")]
    public GameObject panelObjetivo;
    
    private Animator anim;
    private bool haLlegado = false;
    private bool temporizadorIniciado = false;
    private bool modoSalidaActivo = false; 

    void Start()
    {
        anim = GetComponent<Animator>();

        // Tu inicio intacto tal cual lo tenías
        if (anim != null)
        {
            anim.SetBool("Caminando", true);
        }

        if (panelObjetivo != null)
        {
            panelObjetivo.SetActive(false);
        }
    }

    void Update()
    {
        Transform destinoActual = modoSalidaActivo ? puntoSalida : puntoDestino;

        if (destinoActual == null || haLlegado) return;

        if (anim != null)
        {
            anim.SetBool("Caminando", true);
        }

        // Usamos MoveTowards para movernos con precisión y sin pasarnos del destino
        Vector3 posicionActualPlana = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 destinoPlano = new Vector3(destinoActual.position.x, 0, destinoActual.position.z);

        transform.position = Vector3.MoveTowards(transform.position, new Vector3(destinoActual.position.x, transform.position.y, destinoActual.position.z), velocidad * Time.deltaTime);

        // Rotación suave hacia el objetivo
        Vector3 direccion = (destinoActual.position - transform.position).normalized;
        if (direccion != Vector3.zero)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(new Vector3(direccion.x, 0, direccion.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * 5f);
        }

        // Medimos la distancia actual
        float distancia = Vector3.Distance(posicionActualPlana, destinoPlano);

        if (distancia < 0.3f)
        {
            haLlegado = true;
           
            if (anim != null)
            {
                anim.SetBool("Caminando", false);
            }

            // Si llegó en modo salida, desaparece
            if (modoSalidaActivo)
            {
                StartCoroutine(DesaparecerNPC());
            }
            // Si llegó en su movimiento inicial del juego, arranca el temporizador original
            else if (!temporizadorIniciado)
            {
                temporizadorIniciado = true;
                StartCoroutine(Esperar10SegundosYMostrarObjetivo());
            }
        }
    }

    // Método para ordenar al NPC irse hacia el punto de salida al terminar la interacción
    public void IniciarCaminoSalida()
    {
        if (puntoSalida == null)
        {
            Debug.LogWarning("[NPC] ¡Ojo! No has asignado el 'Punto Salida' en el Inspector.");
            gameObject.SetActive(false); 
            return;
        }

        haLlegado = false;
        modoSalidaActivo = true;

        if (anim != null)
        {
            // Forzamos el booleano y además reiniciamos la reproducción del estado de caminata por nombre
            anim.SetBool("Caminando", true);
            anim.Play("mixamo_com", 0, 0f); // <--- Esto salta directo a la animación de caminar sin importar dónde estaba antes
        }

        Debug.Log("[NPC] ¡Cambiando al punto de salida y forzando caminata!");
    }

    IEnumerator DesaparecerNPC()
    {
        Debug.Log("[NPC] ¡Llegó al punto final de salida! Desapareciendo...");
        yield return new WaitForSeconds(1f);
        gameObject.SetActive(false);
    }

    IEnumerator Esperar10SegundosYMostrarObjetivo()
    {
        Debug.Log("¡NPC llegó al destino inicial! Iniciando cuenta de 5 segundos...");

        yield return new WaitForSeconds(5f);

        Debug.Log("¡5 segundos cumplidos! Activando cartel.");

        if (panelObjetivo != null)
        {
            panelObjetivo.SetActive(true);
        }

        yield return new WaitForSeconds(5f);

        // Se oculta el cartel de objetivo automáticamente
        if (panelObjetivo != null)
        {
            panelObjetivo.SetActive(false);
            Debug.Log("Cartel ocultado. Activando proximidad para el diálogo del NPC...");
        }

        // Le damos luz verde al script de diálogo para que el Trigger por proximidad empiece a funcionar
        DialogoNPC dialogoScript = GetComponent<DialogoNPC>();
        if (dialogoScript != null)
        {
            dialogoScript.HabilitarDialogoPorProximidad();
        }
    }
}