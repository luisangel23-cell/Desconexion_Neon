using UnityEngine;

namespace DesconexionNeon.GrafosIA
{
    /// <summary>
    /// Componente interactivo para activar la mecánica de "Firewall" en Unity.
    /// Permite al jugador cortar dinámicamente una arista entre dos nodos al presionar una tecla
    /// o ingresar a una zona de activación (Trigger).
    /// </summary>
    public class FirewallTrigger : MonoBehaviour
    {
        [Header("Referencias del Grafo")]
        [Tooltip("Grafo de la ciudad donde se cortará la conexión.")]
        [SerializeField] private CityGraph cityGraph;

        [Tooltip("Primer nodo que define la calle/arista a bloquear.")]
        [SerializeField] private CityNode nodeA;

        [Tooltip("Segundo nodo que define la calle/arista a bloquear.")]
        [SerializeField] private CityNode nodeB;

        [Header("Configuración de Activación")]
        [Tooltip("Tecla para activar el Firewall manualmente.")]
        [SerializeField] private KeyCode activationKey = KeyCode.F;

        [Tooltip("Si es verdadero, se activa automáticamente cuando el jugador entra al trigger.")]
        [SerializeField] private bool triggerOnEnter = false;

        [Header("Efectos Visuales (Opcional)")]
        [Tooltip("GameObject de la barrera láser o malla de firewall a activar.")]
        [SerializeField] private GameObject visualBarrierObject;

        private bool isFirewallActive = false;

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

            if (visualBarrierObject != null)
            {
                visualBarrierObject.SetActive(false);
            }
        }

        private void Update()
        {
            // Activación manual mediante tecla (ej: tecla F)
            if (Input.GetKeyDown(activationKey))
            {
                ToggleFirewall();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerOnEnter && other.CompareTag("Player") && !isFirewallActive)
            {
                DeployFirewall();
            }
        }

        /// <summary>
        /// Activa el firewall eliminando la arista entre nodeA y nodeB en el grafo.
        /// </summary>
        [ContextMenu("Activar Firewall")]
        public void DeployFirewall()
        {
            if (cityGraph == null || nodeA == null || nodeB == null)
            {
                Debug.LogWarning("[FirewallTrigger] Faltan referencias para activar el Firewall.");
                return;
            }

            if (isFirewallActive) return;

            bool success = cityGraph.BreakEdge(nodeA, nodeB);
            if (success)
            {
                isFirewallActive = true;
                if (visualBarrierObject != null)
                {
                    visualBarrierObject.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Desactiva el firewall restaurando la conexión en el grafo.
        /// </summary>
        [ContextMenu("Desactivar Firewall")]
        public void RemoveFirewall()
        {
            if (cityGraph == null || nodeA == null || nodeB == null) return;

            if (!isFirewallActive) return;

            bool success = cityGraph.RestoreEdge(nodeA, nodeB);
            if (success)
            {
                isFirewallActive = false;
                if (visualBarrierObject != null)
                {
                    visualBarrierObject.SetActive(false);
                }
            }
        }

        public void ToggleFirewall()
        {
            if (isFirewallActive)
            {
                RemoveFirewall();
            }
            else
            {
                DeployFirewall();
            }
        }
    }
}
