using System;
using UnityEngine;

namespace DesconexionNeon.Networking
{
    /// <summary>
    /// Paquete de sincronización cinemática continua enviado a frecuencia fija (20-30 Hz).
    /// </summary>
    [Serializable]
    public class PlayerSyncPacket
    {
        public string type = "sync";
        public int playerId;
        public float posX;
        public float posY;
        public float posZ;
        public float rotY;
        public float speed;
        public bool isAttacking;
        public bool isBlocking;
    }

    /// <summary>
    /// Paquete de eventos puntuales o acciones críticas (golpes, habilidades, despliegue de firewall).
    /// </summary>
    [Serializable]
    public class PlayerActionPacket
    {
        public string type = "action";
        public int playerId;
        public string actionName;
    }

    /// <summary>
    /// Componente que sincroniza la entidad del jugador en la red.
    /// 
    /// - Si isLocalPlayer == true: Lee inputs, mueve la entidad y transmite su posición a tasa fija (Tick-Rate).
    /// - Si isLocalPlayer == false: Desactiva inputs locales e interpola suavemente la posición/rotación
    ///   recibida por red mediante Vector3.Lerp / Quaternion.Slerp, evitando saltos bruscos.
    /// </summary>
    public class NetworkPlayer : MonoBehaviour
    {
        [Header("--- Identidad de Red ---")]
        [SerializeField] private int playerId = 0;
        [SerializeField] private bool isLocalPlayer = false;

        [Header("--- Configuración de Transmisión (Tick Rate) ---")]
        [Tooltip("Frecuencia de envío de paquetes por segundo (Hz). 25-30 Hz es ideal para LAN.")]
        [SerializeField] private float sendRate = 30f;

        [Tooltip("Velocidad de interpolación cinemática para el jugador remoto.")]
        [SerializeField] private float interpolationSpeed = 16f;

        [Header("--- Referencias Internas ---")]
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private Renderer playerRenderer;

        // Variables de interpolación cinemática (Jugador Remoto)
        private Vector3 networkTargetPosition;
        private Quaternion networkTargetRotation;
        private float lastSendTime = 0f;
        private float sendInterval;

        // Caché de hashes de animación para alto rendimiento
        private static readonly int AnimSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimAttackLight = Animator.StringToHash("LightAttack");
        private static readonly int AnimAttackHeavy = Animator.StringToHash("HeavyAttack");
        private static readonly int AnimDodge = Animator.StringToHash("Dodge");
        private static readonly int AnimIsBlocking = Animator.StringToHash("IsBlocking");

        public int PlayerId => playerId;
        public bool IsLocalPlayer => isLocalPlayer;

        private void Awake()
        {
            sendInterval = 1f / sendRate;
            networkTargetPosition = transform.position;
            networkTargetRotation = transform.rotation;

            if (playerAnimator == null)
            {
                playerAnimator = GetComponentInChildren<Animator>();
            }

            if (playerRenderer == null)
            {
                playerRenderer = GetComponentInChildren<Renderer>();
            }
        }

        /// <summary>
        /// Configura el rol y colores de la entidad tras ser instanciada por NetworkManager.
        /// </summary>
        public void Initialize(int id, bool isLocal, Color neonColor)
        {
            this.playerId = id;
            this.isLocalPlayer = isLocal;

            // Asignar color temático neón al material si está disponible
            if (playerRenderer != null)
            {
                Material mat = playerRenderer.material;
                if (mat != null)
                {
                    mat.color = neonColor;
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.SetColor("_EmissionColor", neonColor * 1.5f);
                    }
                }
            }

            // Si es un jugador remoto, desactivar componentes de entrada local
            if (!isLocalPlayer)
            {
                DisableLocalInputComponents();
            }
        }

        private void DisableLocalInputComponents()
        {
            // Desactivar scripts de movimiento del jugador local para evitar interferencias de teclado
            MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
            foreach (var script in scripts)
            {
                if (script == this) continue;

                string typeName = script.GetType().Name;
                if (typeName.Contains("PlayerCityController") ||
                    typeName.Contains("PlayerFighterCombat") ||
                    typeName.Contains("PlayerController"))
                {
                    script.enabled = false;
                }
            }

            // Si tiene CharacterController, dejarlo kinematic o controlado por script
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }

            // Si tiene Rigidbody, configurarlo como cinemático
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }

        private void Update()
        {
            if (isLocalPlayer)
            {
                HandleLocalPlayerTick();
            }
            else
            {
                HandleRemotePlayerInterpolation();
            }
        }

        #region --- Lógica Jugador Local (Emisor) ---

        private void HandleLocalPlayerTick()
        {
            // Transmisión periódica a tasa fija (Tick-Rate) para no saturar el buffer TCP
            if (Time.time - lastSendTime >= sendInterval)
            {
                lastSendTime = Time.time;
                BroadcastCurrentState();
            }

            // Detección de acciones locales para retransmisión inmediata
            DetectAndBroadcastActions();
        }

        private void BroadcastCurrentState()
        {
            if (NetworkManager.Instance == null || !NetworkManager.Instance.IsConnected) return;

            Vector3 pos = transform.position;
            float rotY = transform.eulerAngles.y;

            // Extraer velocidad actual para sincronizar animación
            float currentSpeed = 0f;
            if (playerAnimator != null)
            {
                currentSpeed = playerAnimator.GetFloat(AnimSpeed);
            }

            PlayerSyncPacket packet = new PlayerSyncPacket
            {
                playerId = this.playerId,
                posX = pos.x,
                posY = pos.y,
                posZ = pos.z,
                rotY = rotY,
                speed = currentSpeed,
                isAttacking = false,
                isBlocking = false
            };

            NetworkManager.Instance.SendPacket(packet);
        }

        private void DetectAndBroadcastActions()
        {
            // Ataque Ligero [J]
            if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))
            {
                TriggerLocalAndNetworkAction("AttackLight");
            }
            // Ataque Pesado [K]
            else if (Input.GetKeyDown(KeyCode.K) || Input.GetMouseButtonDown(1))
            {
                TriggerLocalAndNetworkAction("AttackHeavy");
            }
            // Esquive / Dash [L]
            else if (Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.LeftShift))
            {
                TriggerLocalAndNetworkAction("Dodge");
            }
            // Despliegue de Firewall [F]
            else if (Input.GetKeyDown(KeyCode.F))
            {
                TriggerLocalAndNetworkAction("DeployFirewall");
            }
        }

        /// <summary>
        /// Dispara una acción en la máquina local y la notifica al oponente por la red.
        /// </summary>
        public void TriggerLocalAndNetworkAction(string actionName)
        {
            if (!isLocalPlayer) return;

            // Notificar por red al oponente
            if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
            {
                PlayerActionPacket packet = new PlayerActionPacket
                {
                    playerId = this.playerId,
                    actionName = actionName
                };
                NetworkManager.Instance.SendPacket(packet);
            }
        }

        #endregion

        #region --- Lógica Jugador Remoto (Receptor) ---

        /// <summary>
        /// Recibe el paquete de cinemática continua y actualiza las metas de interpolación.
        /// </summary>
        public void ApplySyncData(PlayerSyncPacket data)
        {
            if (isLocalPlayer) return;

            networkTargetPosition = new Vector3(data.posX, data.posY, data.posZ);
            networkTargetRotation = Quaternion.Euler(0f, data.rotY, 0f);

            // Sincronizar parámetro de animación de velocidad
            if (playerAnimator != null)
            {
                playerAnimator.SetFloat(AnimSpeed, data.speed);
            }
        }

        /// <summary>
        /// Ejecuta una acción o habilidad disparada remotamente por el oponente.
        /// </summary>
        public void ExecuteRemoteAction(string actionName)
        {
            if (isLocalPlayer) return;

            Debug.Log($"<color=magenta>[NetworkPlayer]</color> Ejecutando acción remota: {actionName}");

            switch (actionName)
            {
                case "AttackLight":
                    if (playerAnimator != null) playerAnimator.SetTrigger(AnimAttackLight);
                    break;

                case "AttackHeavy":
                    if (playerAnimator != null) playerAnimator.SetTrigger(AnimAttackHeavy);
                    break;

                case "Dodge":
                    if (playerAnimator != null) playerAnimator.SetTrigger(AnimDodge);
                    break;

                case "DeployFirewall":
                    // Si el minijuego tiene componente de Firewall, se puede invocar aquí
                    SendMessage("DeployFirewallRemote", SendMessageOptions.DontRequireReceiver);
                    break;
            }
        }

        private void HandleRemotePlayerInterpolation()
        {
            // Si la distancia es muy grande (ej. teletransporte o spawn inicial), ajustar de golpe
            if (Vector3.Distance(transform.position, networkTargetPosition) > 5.0f)
            {
                transform.position = networkTargetPosition;
                transform.rotation = networkTargetRotation;
                return;
            }

            // Interpolación suave y orgánica (Lerp / Slerp)
            transform.position = Vector3.Lerp(transform.position, networkTargetPosition, Time.deltaTime * interpolationSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, networkTargetRotation, Time.deltaTime * interpolationSpeed);
        }

        #endregion
    }
}

