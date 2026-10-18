using System.Collections.Generic;
using UnityEngine;

namespace DesconexionNeon.GrafosIA
{
    /// <summary>
    /// Controlador de Inteligencia Artificial para el enemigo "Thought Police" (anteriormente Trend-Bot).
    /// Utiliza el algoritmo de Dijkstra provisto por CityGraph para calcular la ruta óptima hacia el jugador,
    /// navegando fluidamente nodo por nodo y recalculando su trayectoria inmediatamente al detectar un Firewall.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ThoughtPoliceController : MonoBehaviour
    {
        [Header("Referencias Principales")]
        [Tooltip("Referencia al grafo de la ciudad.")]
        [SerializeField] private CityGraph cityGraph;

        [Tooltip("Transform del jugador a perseguir.")]
        [SerializeField] private Transform playerTransform;

        [Header("Parámetros de Navegación")]
        [Tooltip("Velocidad de traslación del bot (unidades/segundo).")]
        [SerializeField] private float moveSpeed = 4.5f;

        [Tooltip("Velocidad de rotación angular hacia el siguiente nodo.")]
        [SerializeField] private float rotationSpeed = 8.0f;

        [Tooltip("Distancia mínima para considerar que el bot alcanzó un nodo.")]
        [SerializeField] private float waypointTolerance = 0.2f;

        [Header("Comportamiento y Persecución")]
        [Tooltip("Intervalo en segundos para verificar si el jugador cambió de intersección.")]
        [SerializeField] private float playerTrackInterval = 1.0f;

        [Header("Contramedidas de IA (Bypass)")]
        [Tooltip("Si la policía queda atrapada por Firewalls, intenta hackear el bypass tras unos segundos.")]
        [SerializeField] private bool autoBypassFirewallsWhenTrapped = true;
        [SerializeField] private float bypassDuration = 5.0f;
        private float blockedTimer = 0f;

        [Header("Efectos Visuales (Game View)")]
        [Tooltip("LineRenderer opcional para proyectar el rayo de rastreo Dijkstra en la vista de juego.")]
        [SerializeField] private LineRenderer pathLineRenderer;

        [Header("Depuración Visual (Scene View)")]
        [SerializeField] private bool drawPathGizmos = true;
        [SerializeField] private Color pathGizmoColor = new Color(1.0f, 0.0f, 0.5f, 0.9f); // Magenta Neón

        // Estado interno de la ruta
        private List<CityNode> currentPath = new List<CityNode>();
        private int currentWaypointIndex = 0;
        private CityNode lastTargetPlayerNode = null;
        private float trackTimer = 0f;
        private bool isPathBlocked = false;

        private void OnEnable()
        {
            // Suscripción al evento del grafo: Si un Firewall corta una arista, reaccionamos al instante
            if (cityGraph != null)
            {
                cityGraph.OnGraphTopologyChanged += HandleGraphTopologyChanged;
            }
        }

        private void OnDisable()
        {
            // Desuscripción para evitar pérdidas de memoria o llamadas a objetos destruidos
            if (cityGraph != null)
            {
                cityGraph.OnGraphTopologyChanged -= HandleGraphTopologyChanged;
            }
        }

        private void Start()
        {
            // Auto-localizar referencias si no fueron asignadas en el Inspector
            if (cityGraph == null)
            {
#if UNITY_2023_1_OR_NEWER
                cityGraph = FindAnyObjectByType<CityGraph>();
#else
                cityGraph = FindObjectOfType<CityGraph>();
#endif
            }

            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
            }

            // Cálculo inicial de ruta
            RecalculatePathToPlayer(forceImmediate: true);
        }

        private void Update()
        {
            // 1. Manejo del estado bloqueado por Firewalls (Protocolo de Contra-Hackeo)
            if (isPathBlocked)
            {
                if (pathLineRenderer != null) pathLineRenderer.positionCount = 0;

                if (autoBypassFirewallsWhenTrapped)
                {
                    blockedTimer += Time.deltaTime;
                    if (blockedTimer >= bypassDuration)
                    {
                        blockedTimer = 0f;
                        AttemptFirewallBypass();
                    }
                }
                return;
            }
            else
            {
                blockedTimer = 0f;
            }

            // 2. Manejo del temporizador de seguimiento del jugador
            trackTimer += Time.deltaTime;
            if (trackTimer >= playerTrackInterval)
            {
                trackTimer = 0f;
                CheckIfPlayerChangedNode();
            }

            // 3. Ejecución del movimiento fluido nodo a nodo
            ExecutePathMovement();

            // 4. Proyección visual de la ruta en la vista de juego (LineRenderer)
            UpdateLineRenderer();
        }

        private void UpdateLineRenderer()
        {
            if (pathLineRenderer == null) return;

            if (currentPath == null || currentPath.Count == 0 || isPathBlocked)
            {
                pathLineRenderer.positionCount = 0;
                return;
            }

            int remainingPoints = (currentPath.Count - currentWaypointIndex) + 1;
            pathLineRenderer.positionCount = remainingPoints;

            pathLineRenderer.SetPosition(0, transform.position);
            int pointIdx = 1;
            for (int i = currentWaypointIndex; i < currentPath.Count; i++)
            {
                if (currentPath[i] != null)
                {
                    pathLineRenderer.SetPosition(pointIdx, currentPath[i].Position);
                    pointIdx++;
                }
            }
        }

        private void AttemptFirewallBypass()
        {
            Debug.Log("<color=#FF0044>[THOUGHT POLICE]</color> <b>¡Fuerza Bruta Completada!</b> " +
                      "La policía ha desmantelado un firewall para reanudar la cacería.");

            CityNode myNode = cityGraph.FindNearestNode(transform.position);
            CityNode targetPlayerNode = cityGraph.FindNearestNode(playerTransform.position);

            if (myNode != null && targetPlayerNode != null)
            {
                cityGraph.RestoreEdge(myNode, targetPlayerNode);
                RecalculatePathToPlayer(forceImmediate: true);
            }
        }

        /// <summary>
        /// Mueve y orienta suavemente a la Thought Police hacia el siguiente nodo de la ruta calculada por Dijkstra.
        /// </summary>
        private void ExecutePathMovement()
        {
            if (currentPath == null || currentPath.Count == 0 || isPathBlocked)
            {
                // La ruta está bloqueada por Firewalls o no hay objetivo
                return;
            }

            if (currentWaypointIndex >= currentPath.Count)
            {
                // Ha completado la ruta actual (llegó al nodo del jugador)
                return;
            }

            CityNode targetWaypoint = currentPath[currentWaypointIndex];
            if (targetWaypoint == null) return;

            Vector3 currentPos = transform.position;
            Vector3 targetPos = targetWaypoint.Position;

            // Mantener la altura (Y) original para evitar inclinaciones accidentales en terrenos planos
            targetPos.y = currentPos.y;

            // Movimiento fluido con MoveTowards (garantiza velocidad constante a 30-60+ fps)
            transform.position = Vector3.MoveTowards(currentPos, targetPos, moveSpeed * Time.deltaTime);

            // Rotación fluida orientada hacia el destino
            Vector3 moveDirection = (targetPos - currentPos).normalized;
            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Comprobar si alcanzamos el nodo actual
            float distanceToNode = Vector3.Distance(transform.position, targetPos);
            if (distanceToNode <= waypointTolerance)
            {
                currentWaypointIndex++;

                if (currentWaypointIndex >= currentPath.Count)
                {
                    OnTargetNodeReached();
                }
            }
        }

        /// <summary>
        /// Controlador de eventos: se ejecuta inmediatamente cuando el jugador rompe una arista con un Firewall.
        /// </summary>
        private void HandleGraphTopologyChanged()
        {
            Debug.Log("<color=#FF3366>[THOUGHT POLICE]</color> <b>¡Alerta de Firewall recibida!</b> " +
                      "La topología urbana ha cambiado. Recalculando ruta con Dijkstra inmediatamente...");

            RecalculatePathToPlayer(forceImmediate: true);
        }

        /// <summary>
        /// Comprueba si el jugador se ha movido a una intersección distinta y amerita actualizar la ruta.
        /// </summary>
        private void CheckIfPlayerChangedNode()
        {
            if (playerTransform == null || cityGraph == null) return;

            CityNode currentPlayerNode = cityGraph.FindNearestNode(playerTransform.position);
            if (currentPlayerNode != null && currentPlayerNode != lastTargetPlayerNode)
            {
                RecalculatePathToPlayer(forceImmediate: false);
            }
        }

        /// <summary>
        /// Determina el nodo inicial más adecuado, el nodo objetivo del jugador y ejecuta Dijkstra.
        /// </summary>
        public void RecalculatePathToPlayer(bool forceImmediate)
        {
            if (cityGraph == null || playerTransform == null) return;

            // 1. Determinar el nodo de inicio de la policía
            // Si el bot ya estaba en tránsito hacia un waypoint y este sigue siendo válido,
            // ese nodo puede ser su punto de partida más cercano.
            CityNode startNode = null;

            if (currentPath != null && currentWaypointIndex < currentPath.Count && currentPath[currentWaypointIndex] != null)
            {
                startNode = currentPath[currentWaypointIndex];
            }
            else
            {
                startNode = cityGraph.FindNearestNode(transform.position);
            }

            // 2. Determinar el nodo del jugador
            CityNode targetNode = cityGraph.FindNearestNode(playerTransform.position);

            if (startNode == null || targetNode == null)
            {
                isPathBlocked = true;
                return;
            }

            // 3. Ejecutar el Algoritmo de Dijkstra
            List<CityNode> newPath = cityGraph.GetShortestPathDijkstra(startNode, targetNode);

            if (newPath != null && newPath.Count > 0)
            {
                currentPath = newPath;
                currentWaypointIndex = 0;
                lastTargetPlayerNode = targetNode;
                isPathBlocked = false;

                Debug.Log($"<color=#FF9900>[THOUGHT POLICE]</color> Nueva ruta calculada: {currentPath.Count} nodos. " +
                          $"Próximo objetivo: {currentPath[0].nodeId}");
            }
            else
            {
                // No hay camino disponible (por ejemplo, el jugador activó Firewalls en todas las salidas)
                currentPath.Clear();
                currentWaypointIndex = 0;
                isPathBlocked = true;

                Debug.LogWarning("<color=#FF0000>[THOUGHT POLICE]</color> ¡Camino bloqueado por Firewalls! " +
                                 "No existe ruta válida hacia el jugador en este momento.");
            }
        }

        private void OnTargetNodeReached()
        {
            Debug.Log("<color=#FF9900>[THOUGHT POLICE]</color> Se ha alcanzado el último nodo de la ruta planificada.");
            // Si el jugador sigue lejos del nodo alcanzado, actualizar la persecución
            RecalculatePathToPlayer(forceImmediate: false);
        }

        private void OnDrawGizmos()
        {
            if (!drawPathGizmos || currentPath == null || currentPath.Count == 0) return;

            // Dibujar la trayectoria activa de la Thought Police en magenta neón
            Gizmos.color = pathGizmoColor;

            // Línea desde la posición actual del bot hasta su próximo waypoint
            if (currentWaypointIndex < currentPath.Count && currentPath[currentWaypointIndex] != null)
            {
                Gizmos.DrawLine(transform.position, currentPath[currentWaypointIndex].Position);
            }

            // Líneas entre los nodos restantes del camino
            for (int i = currentWaypointIndex; i < currentPath.Count - 1; i++)
            {
                if (currentPath[i] != null && currentPath[i + 1] != null)
                {
                    Gizmos.DrawLine(currentPath[i].Position, currentPath[i + 1].Position);
                    Gizmos.DrawSphere(currentPath[i].Position, 0.25f);
                }
            }

            // Esfera resaltada en el nodo destino final
            if (currentPath.Count > 0 && currentPath[currentPath.Count - 1] != null)
            {
                Gizmos.DrawWireSphere(currentPath[currentPath.Count - 1].Position, 0.45f);
            }
        }
    }
}
