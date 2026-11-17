using UnityEngine;

public class GeneradorModoSolo : MonoBehaviour
{
    [Header("Personaje Fijo del Jugador (P1)")]
    public GameObject prefabTuPersonajeFijo;

    [Header("Personaje Fijo del Bot (Rival)")]
    public GameObject prefabBotRival;

    [Header("Puntos de Aparición")]
    public Transform puntoSpawnP1;
    public Transform puntoSpawnP2;

    [HideInInspector]
    public GameObject instanciaP1;
    [HideInInspector]
    public GameObject instanciaBot;

    void Awake()
    {
        GenerarPartidaFija();
    }

    void GenerarPartidaFija()
    {
        // --- 1. INSTANCIAR TU JUGADOR ---
        if (prefabTuPersonajeFijo != null && puntoSpawnP1 != null)
        {
            instanciaP1 = Instantiate(prefabTuPersonajeFijo, puntoSpawnP1.position, puntoSpawnP1.rotation);
            ControladorJugadorVsBot controlP1 = instanciaP1.GetComponent<ControladorJugadorVsBot>();
            if (controlP1 != null) controlP1.idMando = 1; 
        }

        // --- 2. INSTANCIAR EL BOT ---
        if (prefabBotRival != null && puntoSpawnP2 != null)
        {
            instanciaBot = Instantiate(prefabBotRival, puntoSpawnP2.position, puntoSpawnP2.rotation);
            
            // ¡CLAVE! Destruir cualquier script humano que traiga el prefab para evitar conflictos
            ControladorCombate scriptViejo = instanciaBot.GetComponent<ControladorCombate>();
            if (scriptViejo != null) Destroy(scriptViejo); 
            
            ControladorJugadorVsBot scriptNuevo = instanciaBot.GetComponent<ControladorJugadorVsBot>();
            if (scriptNuevo != null) Destroy(scriptNuevo);

            // Añadir el cerebro del Bot
            ControladorBotIA controlBot = instanciaBot.GetComponent<ControladorBotIA>();
            if (controlBot == null) instanciaBot.AddComponent<ControladorBotIA>();
        }

        // --- 3. CORRECCIÓN DE ORIENTACIÓN ---
        if (instanciaP1 != null && instanciaBot != null)
        {
            Vector3 posBotFija = new Vector3(instanciaBot.transform.position.x, instanciaP1.transform.position.y, instanciaBot.transform.position.z);
            Vector3 posP1Fija = new Vector3(instanciaP1.transform.position.x, instanciaBot.transform.position.y, instanciaP1.transform.position.z);

            instanciaP1.transform.LookAt(posBotFija);
            instanciaBot.transform.LookAt(posP1Fija);
        }
    }
}