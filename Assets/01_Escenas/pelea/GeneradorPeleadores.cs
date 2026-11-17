using UnityEngine;

public class GeneradorPeleadores : MonoBehaviour
{
    [Header("Lista de Prefabs")]
    public GameObject[] prefabsPersonajes;

    [Header("Puntos de Aparición")]
    public Transform puntoSpawnP1;
    public Transform puntoSpawnP2;

    [HideInInspector]
    public GameObject instanciaP1;
    [HideInInspector]
    public GameObject instanciaP2;

    void Awake()
    {
        GenerarLuchadores();
    }

    void GenerarLuchadores()
    {
        string nombreP1 = PlayerPrefs.GetString("P1_Personaje", "Sombra");
        string nombreP2 = PlayerPrefs.GetString("P2_Personaje", "Sombra");

        GameObject prefabP1 = BuscarPrefabPorNombre(nombreP1);
        GameObject prefabP2 = BuscarPrefabPorNombre(nombreP2);

        // --- INSTANCIAR JUGADOR 1 Y ASIGNAR MANDO 1 ---
        if (prefabP1 != null && puntoSpawnP1 != null)
        {
            instanciaP1 = Instantiate(prefabP1, puntoSpawnP1.position, puntoSpawnP1.rotation);

            ControladorCombate controlP1 = instanciaP1.GetComponent<ControladorCombate>();
            if (controlP1 != null)
            {
                controlP1.idMando = 1; // Asigna Mando 1
            }
        }

        // --- INSTANCIAR JUGADOR 2 Y ASIGNAR MANDO 2 ---
        if (prefabP2 != null && puntoSpawnP2 != null)
        {
            instanciaP2 = Instantiate(prefabP2, puntoSpawnP2.position, puntoSpawnP2.rotation);

            ControladorCombate controlP2 = instanciaP2.GetComponent<ControladorCombate>();
            if (controlP2 != null)
            {
                controlP2.idMando = 2; // Asigna Mando 2
            }
        }

        // --- CORRECCIÓN DE ORIENTACIÓN ---
        if (instanciaP1 != null && instanciaP2 != null)
        {
            Vector3 posP2 = new Vector3(instanciaP2.transform.position.x, instanciaP1.transform.position.y, instanciaP2.transform.position.z);
            Vector3 posP1 = new Vector3(instanciaP1.transform.position.x, instanciaP2.transform.position.y, instanciaP1.transform.position.z);

            instanciaP1.transform.LookAt(posP2);
            instanciaP2.transform.LookAt(posP1);
        }
    }

    GameObject BuscarPrefabPorNombre(string nombre)
    {
        foreach (GameObject prefab in prefabsPersonajes)
        {
            if (prefab != null && prefab.name.Equals(nombre, System.StringComparison.OrdinalIgnoreCase))
            {
                return prefab;
            }
        }
        
        Debug.LogWarning("No se encontró el prefab '" + nombre + "'. Cargando personaje por defecto.");
        return prefabsPersonajes.Length > 0 ? prefabsPersonajes[0] : null;
    }
}