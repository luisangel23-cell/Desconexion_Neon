using System.Collections.Generic;
using UnityEngine;

namespace DesconexionNeon.GrafosIA
{
    /// <summary>
    /// Representa una intersección, calle o punto de decisión en el mapa de la ciudad.
    /// Puede ser posicionado visualmente en la escena de Unity mediante su Transform.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class CityNode : MonoBehaviour
    {
        [Header("Identificación")]
        [Tooltip("Identificador único del nodo (ej: 'Calle_Neon_1', 'Plaza_Central')")]
        public string nodeId;

        [Header("Conexiones Iniciales (Diseño de Nivel)")]
        [Tooltip("Vecinos iniciales conectados bidireccionalmente a este nodo en el inicio.")]
        public List<CityNode> initialNeighbors = new List<CityNode>();

        /// <summary>
        /// Posición en coordenadas del mundo del nodo.
        /// </summary>
        public Vector3 Position => transform.position;

        private void Reset()
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                nodeId = gameObject.name;
            }
        }

        private void OnDrawGizmos()
        {
            // Dibujar nodo en la vista de escena (Cian Neón para coincidir con la estética del juego)
            Gizmos.color = new Color(0.0f, 0.9f, 1.0f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.4f);

            // Dibujar las líneas hacia sus vecinos iniciales en modo edición
            if (initialNeighbors != null)
            {
                Gizmos.color = new Color(0.0f, 0.6f, 0.9f, 0.4f);
                foreach (var neighbor in initialNeighbors)
                {
                    if (neighbor != null)
                    {
                        Gizmos.DrawLine(transform.position, neighbor.transform.position);
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Resaltar el nodo seleccionado con amarillo neón
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
    }
}
