using System.Collections.Generic;
using UnityEngine;

namespace DesconexionNeon.GrafosIA
{
    [DefaultExecutionOrder(-50)] // ¡VITAL! Fuerza a que se genere la ciudad antes que el Grafo
    public class CityGridGenerator : MonoBehaviour
    {
        [Header("Dimensiones de la Red Urbana")]
        [SerializeField] private int columns = 3;
        [SerializeField] private int rows = 3;
        [SerializeField] private float spacing = 12.0f;

        [Header("Atajos / Subterráneos")]
        [SerializeField] private bool addBypassTunnels = true;

        private void Awake()
        {
            // Solo generamos si la ciudad está vacía
            if (transform.childCount == 0)
            {
                GenerateGridRuntime();
            }
        }

        public void GenerateGridRuntime()
        {
            CityNode[,] nodeMatrix = new CityNode[columns, rows];

            // 1. Crear nodos
            for (int x = 0; x < columns; x++)
            {
                for (int z = 0; z < rows; z++)
                {
                    GameObject nodeObj = new GameObject($"Nodo_{x}_{z}");
                    nodeObj.transform.parent = transform;
                    nodeObj.transform.position = transform.position + new Vector3(x * spacing, 0f, z * spacing);

                    CityNode node = nodeObj.AddComponent<CityNode>();
                    node.nodeId = $"Interseccion_{x}_{z}";
                    nodeMatrix[x, z] = node;
                }
            }

            // 2. Conectar bidireccionalmente
            for (int x = 0; x < columns; x++)
            {
                for (int z = 0; z < rows; z++)
                {
                    CityNode current = nodeMatrix[x, z];

                    if (x + 1 < columns)
                    {
                        CityNode right = nodeMatrix[x + 1, z];
                        current.initialNeighbors.Add(right);
                        right.initialNeighbors.Add(current);
                    }
                    if (z + 1 < rows)
                    {
                        CityNode top = nodeMatrix[x, z + 1];
                        current.initialNeighbors.Add(top);
                        top.initialNeighbors.Add(current);
                    }
                }
            }

            // 3. Conectar atajos
            if (addBypassTunnels && columns >= 2 && rows >= 2)
            {
                CityNode n00 = nodeMatrix[0, 0];
                CityNode n11 = nodeMatrix[1, 1];
                n00.initialNeighbors.Add(n11);
                n11.initialNeighbors.Add(n00);

                CityNode nEnd = nodeMatrix[columns - 1, rows - 1];
                CityNode nPrev = nodeMatrix[columns - 2, rows - 2];
                nEnd.initialNeighbors.Add(nPrev);
                nPrev.initialNeighbors.Add(nEnd);
            }
        }
    }
}