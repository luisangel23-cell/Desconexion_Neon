using UnityEngine;

namespace DesconexionNeon.GrafosIA
{
    /// <summary>
    /// Controlador del jugador para el minijuego de "Desconexión Neón".
    /// Permite movimiento libre con WASD/Flechas y despliegue dinámico de Firewalls
    /// sobre la calle en la que se encuentra actualmente presionando [F] o [Espacio].
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PlayerCityController : MonoBehaviour
    {
        [Header("Movimiento")]
        [SerializeField] private float moveSpeed = 6.0f;
        [SerializeField] private float rotationSpeed = 12.0f;

        [Header("Mecánica de Firewall")]
        [Tooltip("Tecla para desplegar un Firewall en la calle actual.")]
        [SerializeField] private KeyCode deployFirewallKey = KeyCode.F;

        [Tooltip("Prefab o efecto visual de barrera láser para instanciar (opcional).")]
        [SerializeField] private GameObject laserWallPrefab;

        private CityGraph cityGraph;

        private void Awake()
        {
            gameObject.tag = "Player";
        }

        private void Start()
        {
            if (cityGraph == null)
            {
#if UNITY_2023_1_OR_NEWER
                cityGraph = FindAnyObjectByType<CityGraph>();
#else
                cityGraph = FindObjectOfType<CityGraph>();
#endif
            }
        }

        private void Update()
        {
            // Bloquear movimiento si el juego ya terminó
            if (MinigameManager.Instance != null && MinigameManager.Instance.currentState != GameState.Playing)
            {
                return;
            }

            // 1. Entrada de movimiento (Horizontal: A/D o Flechas, Vertical: W/S o Flechas)
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 moveDir = new Vector3(h, 0f, v).normalized;

            if (moveDir.sqrMagnitude > 0.01f)
            {
                // Mover al jugador a velocidad constante
                transform.position += moveDir * moveSpeed * Time.deltaTime;

                // Rotar hacia la dirección del movimiento
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            // 2. Despliegue dinámico de Firewall
            if (Input.GetKeyDown(deployFirewallKey) || Input.GetKeyDown(KeyCode.Space))
            {
                DeployFirewallOnClosestStreet();
            }
        }

        /// <summary>
        /// Identifica la calle (arista) más cercana a la posición actual del jugador
        /// y activa un Firewall para cortarla, gastando una carga.
        /// </summary>
        public void DeployFirewallOnClosestStreet()
        {
            if (cityGraph == null) return;

            // Encontrar el nodo más cercano
            CityNode nearestNode = cityGraph.FindNearestNode(transform.position);
            if (nearestNode == null) return;

            // Encontrar el segundo nodo conectado más cercano (la calle donde se encuentra el jugador)
            var edges = cityGraph.GetEdges(nearestNode);
            if (edges == null || edges.Count == 0) return;

            CityNode bestNeighbor = null;
            float minCombinedDist = float.MaxValue;

            foreach (var edge in edges)
            {
                CityNode neighbor = edge.To;
                if (neighbor == null) continue;

                // Medir la distancia del jugador al segmento entre nearestNode y neighbor
                float d1 = Vector3.Distance(transform.position, nearestNode.Position);
                float d2 = Vector3.Distance(transform.position, neighbor.Position);
                float combined = d1 + d2;

                if (combined < minCombinedDist)
                {
                    minCombinedDist = combined;
                    bestNeighbor = neighbor;
                }
            }

            if (bestNeighbor != null)
            {
                if (MinigameManager.Instance != null)
                {
                    bool consumed = MinigameManager.Instance.TryConsumeFirewallCharge(nearestNode, bestNeighbor);
                    if (consumed && laserWallPrefab != null)
                    {
                        Vector3 midPoint = (nearestNode.Position + bestNeighbor.Position) * 0.5f;
                        Instantiate(laserWallPrefab, midPoint, Quaternion.identity);
                    }
                }
                else
                {
                    cityGraph.BreakEdge(nearestNode, bestNeighbor);
                }
            }
        }
    }
}
