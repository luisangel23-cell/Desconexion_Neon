using UnityEngine;

public class CamaraPeleaMultijugador : MonoBehaviour
{
    [Header("Referencias Automáticas")]
    public GeneradorPeleadores generador;
    private Transform jugador1;
    private Transform jugador2;
    private ControladorCombate ctrl1;
    private ControladorCombate ctrl2;

    [Header("Ajustes de Seguimiento Horizontal")]
    public float suavizado = 6.0f;

    [Header("Límites de Pantalla (Paredes Invisibles)")]
    public bool limitarMovimientoJugadores = true;
    [Range(0.02f, 0.25f)]
    public float margenBorde = 0.08f; // 8% de margen en los bordes izquierdo y derecho

    private Camera cam;
    private float yFija;
    private float zFija;
    private bool combateIniciado = false;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        if (generador == null)
        {
            generador = FindObjectOfType<GeneradorPeleadores>();
        }
    }

    void LateUpdate()
    {
        // 1. Obtener los luchadores instanciados desde GeneradorPeleadores
        if (jugador1 == null || jugador2 == null)
        {
            if (generador != null && generador.instanciaP1 != null && generador.instanciaP2 != null)
            {
                jugador1 = generador.instanciaP1.transform;
                jugador2 = generador.instanciaP2.transform;
                ctrl1 = jugador1.GetComponent<ControladorCombate>();
                ctrl2 = jugador2.GetComponent<ControladorCombate>();
            }
            return;
        }

        // 2. Esperar a que SecuenciaIntroManager termine la intro y active los controles
        if (!combateIniciado)
        {
            if (ctrl1 != null && ctrl1.enabled && ctrl2 != null && ctrl2.enabled)
            {
                // Justo cuando termina la intro y la cámara vuelve al plano general, fijamos Y y Z
                yFija = transform.position.y;
                zFija = transform.position.z;
                combateIniciado = true;
            }
            else
            {
                // Mientras están en la intro de rostros, no tocamos la cámara
                return;
            }
        }

        // 3. Mover cámara SOLO horizontalmente (eje X) siguiendo el punto medio
        float centroX = (jugador1.position.x + jugador2.position.x) / 2f;
        Vector3 posicionObjetivo = new Vector3(centroX, yFija, zFija);
        transform.position = Vector3.Lerp(transform.position, posicionObjetivo, suavizado * Time.deltaTime);

        // 4. Encerrar a ambos luchadores dentro de los bordes de la pantalla
        if (limitarMovimientoJugadores)
        {
            RestringirEnPantalla(jugador1);
            RestringirEnPantalla(jugador2);
        }
    }

    void RestringirEnPantalla(Transform personaje)
    {
        if (personaje == null || cam == null) return;

        Vector3 posViewport = cam.WorldToViewportPoint(personaje.position);

        if (posViewport.z > 0)
        {
            float xLimitada = Mathf.Clamp(posViewport.x, margenBorde, 1f - margenBorde);

            if (!Mathf.Approximately(posViewport.x, xLimitada))
            {
                posViewport.x = xLimitada;
                Vector3 nuevaPosMundo = cam.ViewportToWorldPoint(posViewport);
                personaje.position = new Vector3(nuevaPosMundo.x, personaje.position.y, personaje.position.z);
            }
        }
    }
}