using System;
using System.Collections.Generic;
using UnityEngine;

namespace DesconexionNeon.GrafosIA
{
    public class CityGraph : MonoBehaviour
    {
        [Header("Configuración Automática")]
        [SerializeField] private bool autoRegisterSceneNodes = true;

        [Header("Depuración Visual (Gizmos)")]
        [SerializeField] private bool drawGraphGizmos = true;
        [SerializeField] private Color edgeColor = new Color(0.0f, 1.0f, 0.6f, 0.7f);
        [SerializeField] private Color firewallColor = new Color(1.0f, 0.1f, 0.2f, 0.9f);

        private Dictionary<CityNode, List<CityEdge>> adjacencyList = new Dictionary<CityNode, List<CityEdge>>();
        private List<CityNode> allNodes = new List<CityNode>();
        private List<(CityNode from, CityNode to)> activeFirewalls = new List<(CityNode, CityNode)>();
        private Dictionary<string, GameObject> visualLines = new Dictionary<string, GameObject>();

        public event Action OnGraphTopologyChanged;

        private void Awake()
        {
            if (autoRegisterSceneNodes)
            {
                InitializeFromScene();
            }
        }

        private void Start()
        {
            CrearLineasVisuales();
        }

        public void InitializeFromScene()
        {
            // Limpiar datos previos para evitar conexiones corruptas al recargar escena
            adjacencyList.Clear();
            allNodes.Clear();

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
        }

        public void CrearLineasVisuales()
        {
            GameObject lineasContainer = GameObject.Find("LineasGrafo_Visual");
            if (lineasContainer != null)
            {
                Destroy(lineasContainer);
            }

            lineasContainer = new GameObject("LineasGrafo_Visual");
            lineasContainer.transform.SetParent(this.transform);

            // Shader visible compatible con URP y Built-in
            Shader lineShader = Shader.Find("Sprites/Default");
            if (lineShader == null) lineShader = Shader.Find("Unlit/Color");
            Material lineMaterial = new Material(lineShader);

            HashSet<string> aristasProcesadas = new HashSet<string>();

            foreach (var kvp in adjacencyList)
            {
                CityNode nodeA = kvp.Key;
                if (nodeA == null) continue;

                foreach (var edge in kvp.Value)
                {
                    CityNode nodeB = edge.To;
                    if (nodeB == null) continue;

                    string edgeKey = GetEdgeKey(nodeA, nodeB);

                    if (aristasProcesadas.Contains(edgeKey)) continue;
                    aristasProcesadas.Add(edgeKey);

                    GameObject lineObj = new GameObject($"Linea_{edgeKey}");
                    lineObj.transform.SetParent(lineasContainer.transform);

                    LineRenderer line = lineObj.AddComponent<LineRenderer>();
                    line.material = lineMaterial;
                    line.startColor = new Color(0f, 1f, 1f, 1f); // Cian sólido neón
                    line.endColor = new Color(0f, 1f, 1f, 1f);
                   // ... (código anterior del LineRenderer)
                    line.startWidth = 0.2f;
                    line.endWidth = 0.2f;
                    line.positionCount = 2;
                    line.useWorldSpace = true;

                    // ¡AQUÍ ESTÁ LA MAGIA! Elevamos la línea 0.25 metros hacia arriba 
                    // para que no se hunda en el suelo gris ni se corte.
                    Vector3 alturaSuelo = Vector3.up * 0.25f;
                    line.SetPosition(0, nodeA.Position + alturaSuelo);
                    line.SetPosition(1, nodeB.Position + alturaSuelo);

                    visualLines[edgeKey] = lineObj;
                }
            }
        }

        private string GetEdgeKey(CityNode a, CityNode b)
        {
            return a.GetInstanceID() < b.GetInstanceID()
                ? $"{a.GetInstanceID()}_{b.GetInstanceID()}"
                : $"{b.GetInstanceID()}_{a.GetInstanceID()}";
        }

        public void RegisterNode(CityNode node)
        {
            if (node == null) return;

            if (!adjacencyList.ContainsKey(node))
            {
                adjacencyList[node] = new List<CityEdge>();
                allNodes.Add(node);
            }
        }

        public void AddEdge(CityNode a, CityNode b, float customCost = -1f)
        {
            if (a == null || b == null || a == b) return;

            RegisterNode(a);
            RegisterNode(b);

            if (HasEdge(a, b)) return;

            float weight = customCost > 0 ? customCost : Vector3.Distance(a.Position, b.Position);

            adjacencyList[a].Add(new CityEdge(a, b, weight));
            adjacencyList[b].Add(new CityEdge(b, a, weight));
        }

        public bool HasEdge(CityNode a, CityNode b)
        {
            if (a == null || b == null || !adjacencyList.ContainsKey(a)) return false;
            return adjacencyList[a].Exists(e => e.To == b);
        }

        public List<CityEdge> GetEdges(CityNode node)
        {
            if (node != null && adjacencyList.ContainsKey(node))
            {
                return adjacencyList[node];
            }
            return new List<CityEdge>();
        }

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

        public bool BreakEdge(CityNode a, CityNode b)
        {
            if (a == null || b == null) return false;

            bool removedFromA = false;
            bool removedFromB = false;

            if (adjacencyList.ContainsKey(a))
            {
                int countA = adjacencyList[a].RemoveAll(e => e.To == b);
                removedFromA = countA > 0;
            }

            if (adjacencyList.ContainsKey(b))
            {
                int countB = adjacencyList[b].RemoveAll(e => e.To == a);
                removedFromB = countB > 0;
            }

            if (removedFromA || removedFromB)
            {
                activeFirewalls.Add((a, b));

                string edgeKey = GetEdgeKey(a, b);
                if (visualLines.ContainsKey(edgeKey) && visualLines[edgeKey] != null)
                {
                    Destroy(visualLines[edgeKey]);
                    visualLines.Remove(edgeKey);
                }

                OnGraphTopologyChanged?.Invoke();
                return true;
            }

            return false;
        }

        public bool RestoreEdge(CityNode a, CityNode b, float customCost = -1f)
        {
            if (a == null || b == null) return false;

            AddEdge(a, b, customCost);
            activeFirewalls.RemoveAll(f => (f.from == a && f.to == b) || (f.from == b && f.to == a));
            CrearLineasVisuales();

            OnGraphTopologyChanged?.Invoke();
            return true;
        }

        public List<CityNode> GetShortestPathDijkstra(CityNode startNode, CityNode targetNode)
        {
            if (startNode == null || targetNode == null) return new List<CityNode>();
            if (!adjacencyList.ContainsKey(startNode) || !adjacencyList.ContainsKey(targetNode)) return new List<CityNode>();
            if (startNode == targetNode) return new List<CityNode> { startNode };

            Dictionary<CityNode, float> distances = new Dictionary<CityNode, float>();
            Dictionary<CityNode, CityNode> previous = new Dictionary<CityNode, CityNode>();
            HashSet<CityNode> unvisited = new HashSet<CityNode>();

            foreach (var node in allNodes)
            {
                distances[node] = float.MaxValue;
                previous[node] = null;
                unvisited.Add(node);
            }

            distances[startNode] = 0f;

            while (unvisited.Count > 0)
            {
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

                if (current == null || smallestDist == float.MaxValue) break;
                if (current == targetNode) break;

                unvisited.Remove(current);

                List<CityEdge> neighbors = adjacencyList[current];
                for (int i = 0; i < neighbors.Count; i++)
                {
                    CityEdge edge = neighbors[i];
                    CityNode neighbor = edge.To;

                    if (!unvisited.Contains(neighbor)) continue;

                    float tentativeDistance = distances[current] + edge.Weight;

                    if (tentativeDistance < distances[neighbor])
                    {
                        distances[neighbor] = tentativeDistance;
                        previous[neighbor] = current;
                    }
                }
            }

            if (distances[targetNode] == float.MaxValue) return new List<CityNode>();

            List<CityNode> shortestPath = new List<CityNode>();
            CityNode step = targetNode;

            while (step != null)
            {
                shortestPath.Add(step);
                if (step == startNode) break;
                step = previous[step];
            }

            shortestPath.Reverse();
            return shortestPath;
        }

        private void OnDrawGizmos()
        {
            if (!drawGraphGizmos) return;

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

                    string edgeKey = GetEdgeKey(nodeA, nodeB);

                    if (!drawnEdges.Contains(edgeKey))
                    {
                        drawnEdges.Add(edgeKey);
                        Gizmos.DrawLine(nodeA.Position, nodeB.Position);
                    }
                }
            }

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