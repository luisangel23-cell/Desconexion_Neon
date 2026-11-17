using UnityEngine;

public class CamaraDinamicaPelea : MonoBehaviour
{
    [Header("Objetivos")]
    public Transform jugador;
    public Transform bot;

    [Header("Ajustes de Seguimiento Horizontal")]
    public float suavizado = 6.0f;

    [Header("Límites de Pantalla (Paredes Invisibles)")]
    public bool limitarMovimientoJugadores = true;
    [Range(0.02f, 0.25f)]
    public float margenBorde = 0.08f; // 8% de margen en los bordes izquierdo y derecho

    private Camera cam;
    private float zFija;
    private float yFija;
    private bool inicializado = false;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        // Guardamos la altura (Y) y profundidad (Z) exactas para que NUNCA cambien
        yFija = transform.position.y;
        zFija = transform.position.z;
        inicializado = true;
    }

    void BuscarLuchadores()
    {
        if (jugador == null)
        {
            ControladorJugadorVsBot p1 = FindObjectOfType<ControladorJugadorVsBot>();
            if (p1 != null) jugador = p1.transform;
        }
        if (bot == null)
        {
            ControladorBotIA p2 = FindObjectOfType<ControladorBotIA>();
            if (p2 != null) bot = p2.transform;
        }
    }

    void LateUpdate()
    {
        if (!inicializado) return;

        if (jugador == null || bot == null)
        {
            BuscarLuchadores();
            if (jugador == null || bot == null) return;
        }

        // 1. Punto medio SOLO en el eje horizontal (X)
        float centroX = (jugador.position.x + bot.position.x) / 2f;

        // 2. La cámara mantiene Y y Z intactas; solo se desliza de lado a lado
        Vector3 posicionObjetivo = new Vector3(centroX, yFija, zFija);
        transform.position = Vector3.Lerp(transform.position, posicionObjetivo, suavizado * Time.deltaTime);

        // 3. Evitar que cualquiera de los dos se salga de los bordes de la cámara
        if (limitarMovimientoJugadores)
        {
            RestringirEnPantalla(jugador);
            RestringirEnPantalla(bot);
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