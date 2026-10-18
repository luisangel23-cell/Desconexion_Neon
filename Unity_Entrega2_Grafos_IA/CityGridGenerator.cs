using System.Collections.Generic;
using UnityEngine;

namespace DesconexionNeon.GrafosIA
{
    /// <summary>
    /// Generador automático de nivel urbano para pruebas y demostración rápida.
    /// Crea una cuadrícula de intersecciones interconectadas con calles principales y túneles subterráneos.
    /// Puedes ejecutarlo desde el menú contextual en el Inspector con un solo clic.
    /// </summary>
    public class CityGridGenerator : MonoBehaviour
    {
        [Header("Dimensiones de la Red Urbana")]
        [SerializeField] private int columns = 3;
        [SerializeField] private int rows = 3;
        [SerializeField] private float spacing = 12.0f;

        [Header("Atajos / Subterráneos")]
        [Tooltip("Agrega conexiones diagonales que sirven como rutas de escape alternativas.")]
        [SerializeField] private bool addBypassTunnels = true;

        [ContextMenu("Generar Cuadrícula Urbana Neón")]
        public void GenerateGrid()
        {
            // Limpiar hijos existentes
            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }

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

            // 2. Conectar calles ortogonales (Malla urbana)
            for (int x = 0; x < columns; x++)
            {
                for (int z = 0; z < rows; z++)
                {
                    CityNode current = nodeMatrix[x, z];

                    // Conectar hacia la derecha (+X)
                    if (x + 1 < columns)
                    {
                        current.initialNeighbors.Add(nodeMatrix[x + 1, z]);
                    }

                    // Conectar hacia arriba (+Z)
                    if (z + 1 < rows)
                    {
                        current.initialNeighbors.Add(nodeMatrix[x, z + 1]);
                    }
                }
            }

            // 3. Conectar túneles bypass (diagonales estratégicas)
            if (addBypassTunnels && columns >= 2 && rows >= 2)
            {
                // Túnel diagonal central
                nodeMatrix[0, 0].initialNeighbors.Add(nodeMatrix[1, 1]);
                nodeMatrix[columns - 1, rows - 1].initialNeighbors.Add(nodeMatrix[columns - 2, rows - 2]);
            }

            // 4. Asegurar que existe el CityGraph en este GameObject
            CityGraph graph = GetComponent<CityGraph>();
            if (graph == null)
            {
                graph = gameObject.AddComponent<CityGraph>();
            }

            Debug.Log($"<color=#00FFAA>[CityGridGenerator]</color> Se ha generado exitosamente una red urbana de {columns * rows} nodos lista para jugar.");
        }
    }
}
