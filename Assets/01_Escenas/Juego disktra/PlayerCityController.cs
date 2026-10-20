using UnityEngine;

namespace DesconexionNeon.GrafosIA
{
    /// <summary>
    /// Controlador del jugador para el minijuego de "Desconexión Neón".
    /// Permite movimiento con WASD/Flechas restringido a las aristas de la red urbana
    /// y despliegue dinámico de Firewalls presionando [F] o [Espacio].
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PlayerCityController : MonoBehaviour
    {
        [Header("Movimiento")]
        [SerializeField] private float moveSpeed = 6.0f;
        [SerializeField] private float rotationSpeed = 12.0f;

        [Header("Restricción de Red Urbana")]
        [Tooltip("Distancia máxima permitida para alejarse del centro de la calle.")]
        [SerializeField] private float maxDistanceFromStreet = 0.5f;

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
                // Posición propuesta en este frame
                Vector3 proposedPos = transform.position + moveDir * moveSpeed * Time.deltaTime;

                // Restringir la posición a la calle/arista más cercana
                if (cityGraph != null)
                {
                    Vector3 constrainedPos = GetConstrainedPositionOnStreet(proposedPos);
                    transform.position = constrainedPos;
                }
                else
                {
                    transform.position = proposedPos;
                }

                // Rotar suavemente hacia la dirección del movimiento
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
        /// Evalúa las aristas adyacentes al nodo más cercano y proyecta la posición propuesta
        /// para evitar que el jugador se salga del carril de la calle.
        /// </summary>
        private Vector3 GetConstrainedPositionOnStreet(Vector3 proposedPos)
        {
            CityNode nearestNode = cityGraph.FindNearestNode(proposedPos);
            if (nearestNode == null) return proposedPos;

            var edges = cityGraph.GetEdges(nearestNode);
            if (edges == null || edges.Count == 0) return nearestNode.Position;

            Vector3 bestPoint = proposedPos;
            float minDistanceSqr = float.MaxValue;

            foreach (var edge in edges)
            {
                if (edge.To == null) continue;

                Vector3 closestPointOnSegment = GetClosestPointOnLineSegment(nearestNode.Position, edge.To.Position, proposedPos);
                float distSqr = (proposedPos - closestPointOnSegment).sqrMagnitude;

                if (distSqr < minDistanceSqr)
                {
                    minDistanceSqr = distSqr;
                    bestPoint = closestPointOnSegment;
                }
            }

            // Si intenta alejarse más allá del margen permitido, lo sujetamos a la línea de la calle
            if (minDistanceSqr > maxDistanceFromStreet * maxDistanceFromStreet)
            {
                return new Vector3(bestPoint.x, proposedPos.y, bestPoint.z);
            }

            return proposedPos;
        }

        /// <summary>
        /// Proyecta matemáticamente un punto 3D sobre un segmento entre dos nodos (A y B).
        /// </summary>
        private Vector3 GetClosestPointOnLineSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float sqrLen = ab.sqrMagnitude;
            if (sqrLen < 0.0001f) return a;

            float t = Vector3.Dot(p - a, ab) / sqrLen;
            t = Mathf.Clamp01(t);
            return a + t * ab;
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