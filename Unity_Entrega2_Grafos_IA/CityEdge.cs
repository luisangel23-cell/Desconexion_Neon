using System;
using UnityEngine;

namespace DesconexionNeon.GrafosIA
{
    /// <summary>
    /// Representa una conexión (arista) dirigida o bidireccional entre dos nodos de la ciudad.
    /// Almacena el nodo destino y el costo (distancia euclidiana o peso de tráfico).
    /// </summary>
    [Serializable]
    public class CityEdge
    {
        public CityNode From { get; private set; }
        public CityNode To { get; private set; }
        public float Weight { get; private set; }

        public CityEdge(CityNode from, CityNode to, float weight)
        {
            From = from;
            To = to;
            Weight = weight;
        }

        public override string ToString()
        {
            return $"{From.nodeId} <--> {To.nodeId} (Costo: {Weight:F2})";
        }
    }
}
