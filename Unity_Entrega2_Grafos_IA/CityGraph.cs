using System;
using System.Collections.Generic;
using UnityEngine;

namespace DesconexionNeon.GrafosIA
{
    /// <summary>
    /// Estructura de Grafo No Dirigido basada en Listas de Adyacencia para la red urbana de "Desconexión Neón".
    /// Administra nodos, aristas bidireccionales, el algoritmo de Dijkstra y la mecánica de Firewall (BreakEdge).
    /// </summary>
    public class CityGraph : MonoBehaviour
    {
        [Header("Configuración Automática")]
        [Tooltip("Si es verdadero, recopila todos los CityNode hijos o en escena al iniciar.")]
        [SerializeField] private bool autoRegisterSceneNodes = true;

        [Header("Depuración Visual (Gizmos)")]
        [SerializeField] private bool drawGraphGizmos = true;
        [SerializeField] private Color edgeColor = new Color(0.0f, 1.0f, 0.6f, 0.7f); // Verde neón
        [SerializeField] private Color firewallColor = new Color(1.0f, 0.1f, 0.2f, 0.9f); // Rojo neón alerta

        // Estructura de datos principal: Lista de Adyacencia
        // Cada nodo mapea a una lista de aristas que apuntan a sus vecinos conectados.
        private Dictionary<CityNode, List<CityEdge>> adjacencyList = new Dictionary<CityNode, List<CityEdge>>();

        // Lista de todos los nodos registrados
        private List<CityNode> allNodes = new List<CityNode>();

        // Registro de aristas bloqueadas por Firewall para trazado visual y posible restauración
        private List<(CityNode from, CityNode to)> activeFirewalls = new List<(CityNode, CityNode)>();

        /// <summary>
        /// Evento C# (Patrón Observer) disparado cuando la topología del grafo cambia
        /// (por ejemplo, al activar un Firewall y eliminar una arista).
        /// Notifica a la 'Thought Police' para que recalcule inmediatamente su ruta con Dijkstra.
        /// </summary>
        public event Action OnGraphTopologyChanged;

        private void Awake()
        {
            if (autoRegisterSceneNodes)
            {
                InitializeFromScene();
            }
        }

        /// <summary>
        /// Busca todos los nodos en la escena o hijos del GameObject y crea el grafo automáticamente.
        /// </summary>
        public void InitializeFromScene()
        {
            CityNode[] foundNodes = GetComponentsInChildren<CityNode>();
            if (foundNodes == null || foundNodes.Length == 0)
            {
#if UNITY_2023_1_OR_NEWER
                foundNodes = FindObjectsByType<CityNode>(FindObjectsSortMode.None);
#else
                foundNodes = FindObjectsOfType<CityNode>();
#endif
            }

            foreach (var node in foundNodes)
            {
                RegisterNode(node);
            }

            // Conectar vecinos iniciales asignados en el Inspector de cada nodo
            foreach (var node in foundNodes)
            {
                if (node.initialNeighbors != null)
                {
                    foreach (var neighbor in node.initialNeighbors)
                    {
                        if (neighbor != null)
                        {
                            AddEdge(node, neighbor);
                        }
                    }
                }
            }

            Debug.Log($"<color=#00FFAA>[CityGraph]</color> Grafo inicializado con {allNodes.Count} nodos.");
        }

        // ==============================================================================
        // 1. GESTIÓN DE NODOS Y ARISTAS (LISTA DE ADYACENCIA)
        // ==============================================================================

        /// <summary>
        /// Registra un nuevo nodo en el grafo si aún no existe.
        /// </summary>
        public void RegisterNode(CityNode node)
        {
            if (node == null) return;

            if (!adjacencyList.ContainsKey(node))
            {
                adjacencyList[node] = new List<CityEdge>();
                allNodes.Add(node);
            }
        }

        /// <summary>
        /// Conecta dos nodos bidireccionalmente en el grafo no dirigido.
        /// Si no se especifica costo o es <= 0, calcula automáticamente la distancia euclidiana entre ambos.
        /// </summary>
        public void AddEdge(CityNode a, CityNode b, float customCost = -1f)
        {
            if (a == null || b == null || a == b) return;

            RegisterNode(a);
            RegisterNode(b);

            // Evitar duplicar aristas ya existentes
            if (HasEdge(a, b)) return;

            float weight = customCost > 0 ? customCost : Vector3.Distance(a.Position, b.Position);

            // Grafo no dirigido: crear conexión en ambas direcciones (A -> B y B -> A)
            adjacencyList[a].Add(new CityEdge(a, b, weight));
            adjacencyList[b].Add(new CityEdge(b, a, weight));
        }

        /// <summary>
        /// Verifica si existe una arista directa entre dos nodos.
        /// </summary>
        public bool HasEdge(CityNode a, CityNode b)
        {
            if (a == null || b == null || !adjacencyList.ContainsKey(a)) return false;

            return adjacencyList[a].Exists(e => e.To == b);
        }

        /// <summary>
        /// Obtiene todas las aristas adyacentes a un nodo.
        /// </summary>
        public List<CityEdge> GetEdges(CityNode node)
        {
            if (node != null && adjacencyList.ContainsKey(node))
            {
                return adjacencyList[node];
            }
            return new List<CityEdge>();
        }

        /// <summary>
        /// Encuentra el nodo del grafo más cercano a una posición arbitraria en el espacio 3D/2D.
        /// </summary>
        public CityNode FindNearestNode(Vector3 worldPosition)
        {
            if (allNodes.Count == 0) return null;

            CityNode closestNode = null;
            float minDistanceSqr = float.MaxValue;

            for (int i = 0; i < allNodes.Count; i++)
            {
                CityNode node = allNodes[i];
                if (node == null) continue;

                float distSqr = (node.Position - worldPosition).sqrMagnitude;
                if (distSqr < minDistanceSqr)
                {
                    minDistanceSqr = distSqr;
                    closestNode = node;
                }
            }

            return closestNode;
        }

        // ==============================================================================
        // 2. MECÁNICA DE FIREWALL (ELIMINACIÓN DINÁMICA DE ARISTAS)
        // ==============================================================================

        /// <summary>
        /// Elimina dinámicamente la arista bidireccional entre los nodos A y B al activar un Firewall.
        /// Dispara inmediatamente el evento OnGraphTopologyChanged para forzar el recalculo de la IA.
        /// </summary>
        /// <param name="a">Primer nodo de la arista.</param>
        /// <param name="b">Segundo nodo de la arista.</param>
        /// <returns>True si la arista fue cortada exitosamente, False si no existía.</returns>
        public bool BreakEdge(CityNode a, CityNode b)
        {
            if (a == null || b == null)
            {
                Debug.LogWarning("[CityGraph] Intento de romper arista con nodos nulos.");
                return false;
            }

            bool removedFromA = false;
            bool removedFromB = false;

            // Eliminar conexión A -> B
            if (adjacencyList.ContainsKey(a))
            {
                int countA = adjacencyList[a].RemoveAll(e => e.To == b);
                removedFromA = countA > 0;
            }

            // Eliminar conexión B -> A (naturaleza no dirigida)
            if (adjacencyList.ContainsKey(b))
            {
                int countB = adjacencyList[b].RemoveAll(e => e.To == a);
                removedFromB = countB > 0;
            }

            if (removedFromA || removedFromB)
            {
                // Registrar para visualización de alerta en Gizmos
                activeFirewalls.Add((a, b));

                Debug.Log($"<color=#FF0055>[FIREWALL ACTIVADO]</color> Conexión cortada entre <b>{a.nodeId}</b> y <b>{b.nodeId}</b>. " +
                          "Notificando a 'Thought Police' para recalculo inmediato.");

                // Notificar a todos los suscriptores (ThoughtPoliceController)
                OnGraphTopologyChanged?.Invoke();
                return true;
            }

            Debug.LogWarning($"[CityGraph] No se encontró arista activa entre {a.nodeId} y {b.nodeId} para eliminar.");
            return false;
        }

        /// <summary>
        /// Restaura una arista previamente rota (por ejemplo, si el Firewall tiene tiempo límite o es desactivado).
        /// </summary>
        public bool RestoreEdge(CityNode a, CityNode b, float customCost = -1f)
        {
            if (a == null || b == null) return false;

            AddEdge(a, b, customCost);

            // Eliminar del registro de firewalls activos
            activeFirewalls.RemoveAll(f => (f.from == a && f.to == b) || (f.from == b && f.to == a));

            Debug.Log($"<color=#00AAFF>[FIREWALL RESTAURADO]</color> Conexión restablecida entre <b>{a.nodeId}</b> y <b>{b.nodeId}</b>.");

            OnGraphTopologyChanged?.Invoke();
            return true;
        }

        // ==============================================================================
        // 3. ALGORITMO DE RUTA MÁS CORTA (DIJKSTRA)
        // ==============================================================================

        /// <summary>
        /// Calcula la ruta de menor costo (distancia mínima) desde un nodo de inicio hasta un nodo objetivo
        /// utilizando el Algoritmo de Dijkstra.
        /// </summary>
        /// <param name="startNode">Nodo origen (ej: posición actual de Thought Police).</param>
        /// <param name="targetNode">Nodo destino (ej: posición actual del jugador).</param>
        /// <returns>Lista ordenada de nodos que conforman la ruta más corta, o lista vacía si es inalcanzable.</returns>
        public List<CityNode> GetShortestPathDijkstra(CityNode startNode, CityNode targetNode)
        {
            // Validaciones previas
            if (startNode == null || targetNode == null)
            {
                Debug.LogWarning("[Dijkstra] Nodo inicial u objetivo es nulo.");
                return new List<CityNode>();
            }

            if (!adjacencyList.ContainsKey(startNode) || !adjacencyList.ContainsKey(targetNode))
            {
                Debug.LogWarning("[Dijkstra] Nodos no registrados en la lista de adyacencia.");
                return new List<CityNode>();
            }

            // Caso base: Ya se encuentra en el nodo destino
            if (startNode == targetNode)
            {
                return new List<CityNode> { startNode };
            }

            // 1. Estructuras de datos auxiliares para Dijkstra:
            // - distances: Distancia acumulada mínima conocida desde startNode hasta cada nodo 'v'.
            // - previous: Almacena el predecesor inmediato de cada nodo para reconstruir el camino al final.
            // - unvisited: Conjunto de nodos no evaluados.
            Dictionary<CityNode, float> distances = new Dictionary<CityNode, float>();
            Dictionary<CityNode, CityNode> previous = new Dictionary<CityNode, CityNode>();
            HashSet<CityNode> unvisited = new HashSet<CityNode>();

            // Inicialización: todas las distancias a infinito excepto el nodo de partida (dist = 0)
            foreach (var node in allNodes)
            {
                distances[node] = float.MaxValue;
                previous[node] = null;
                unvisited.Add(node);
            }

            distances[startNode] = 0f;

            // 2. Bucle principal de Dijkstra:
            while (unvisited.Count > 0)
            {
                // Extraer el nodo no visitado con la menor distancia tentativa
                CityNode current = null;
                float smallestDist = float.MaxValue;

                foreach (var node in unvisited)
                {
                    if (distances[node] < smallestDist)
                    {
                        smallestDist = distances[node];
                        current = node;
                    }
                }

                // Si no hay nodo accesible o la menor distancia es infinita, no existe camino posible
                if (current == null || smallestDist == float.MaxValue)
                {
                    break;
                }

                // Si alcanzamos el nodo objetivo, podemos terminar la búsqueda anticipadamente
                if (current == targetNode)
                {
                    break;
                }

                // Marcar como visitado removiéndolo del conjunto de pendientes
                unvisited.Remove(current);

                // Relajación de aristas: Inspeccionar todos los vecinos conectados en la lista de adyacencia
                List<CityEdge> neighbors = adjacencyList[current];
                for (int i = 0; i < neighbors.Count; i++)
                {
                    CityEdge edge = neighbors[i];
                    CityNode neighbor = edge.To;

                    // Solo evaluar vecinos no visitados
                    if (!unvisited.Contains(neighbor)) continue;

                    float tentativeDistance = distances[current] + edge.Weight;

                    // Si encontramos un camino más corto hacia 'neighbor', actualizar
                    if (tentativeDistance < distances[neighbor])
                    {
                        distances[neighbor] = tentativeDistance;
                        previous[neighbor] = current;
                    }
                }
            }

            // 3. Reconstrucción del camino óptimo desde targetNode hacia startNode:
            if (distances[targetNode] == float.MaxValue)
            {
                // Inalcanzable (por ejemplo, si los Firewalls aislaron completamente al jugador)
                return new List<CityNode>();
            }

            List<CityNode> shortestPath = new List<CityNode>();
            CityNode step = targetNode;

            while (step != null)
            {
                shortestPath.Add(step);
                if (step == startNode) break;
                step = previous[step];
            }

            // Invertir para que la lista empiece en startNode y termine en targetNode
            shortestPath.Reverse();

            return shortestPath;
        }

        // ==============================================================================
        // 4. DEPURACIÓN VISUAL EN UNITY SCENE (GIZMOS)
        // ==============================================================================

        private void OnDrawGizmos()
        {
            if (!drawGraphGizmos) return;

            // Dibujar conexiones activas en verde neón
            Gizmos.color = edgeColor;
            HashSet<string> drawnEdges = new HashSet<string>();

            foreach (var kvp in adjacencyList)
            {
                CityNode nodeA = kvp.Key;
                if (nodeA == null) continue;

                foreach (var edge in kvp.Value)
                {
                    CityNode nodeB = edge.To;
                    if (nodeB == null) continue;

                    // Evitar dibujar la misma arista dos veces (al ser bidireccional)
                    string edgeKey = nodeA.GetInstanceID() < nodeB.GetInstanceID()
                        ? $"{nodeA.GetInstanceID()}_{nodeB.GetInstanceID()}"
                        : $"{nodeB.GetInstanceID()}_{nodeA.GetInstanceID()}";

                    if (!drawnEdges.Contains(edgeKey))
                    {
                        drawnEdges.Add(edgeKey);
                        Gizmos.DrawLine(nodeA.Position, nodeB.Position);
                    }
                }
            }

            // Dibujar aristas cortadas por Firewall en rojo neón parpadeante
            Gizmos.color = firewallColor;
            for (int i = 0; i < activeFirewalls.Count; i++)
            {
                var (from, to) = activeFirewalls[i];
                if (from != null && to != null)
                {
                    Gizmos.DrawLine(from.Position, to.Position);
                    Vector3 center = (from.Position + to.Position) * 0.5f;
                    Gizmos.DrawWireCube(center, Vector3.one * 0.6f);
                }
            }
        }
    }
}
