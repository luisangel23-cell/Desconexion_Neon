using System;
using UnityEngine;

namespace DesconexionNeon.Networking
{
    [Serializable]
    public class PlayerSyncPacket
    {
        public string type = "sync";
        public int playerId;
        public float posX;
        public float posY;
        public float posZ;
        public float rotY;
        public string currentAnim;
        public float vidaActual;
    }

    [Serializable]
    public class PlayerDamagePacket
    {
        public string type = "damage";
        public int targetPlayerId;
        public float damageAmount;
    }

    [Serializable]
    public class PlayerActionPacket
    {
        public string type = "action";
        public int playerId;
        public string actionName;
    }

    public class NetworkPlayer : MonoBehaviour
    {
        [Header("--- Identidad de Red ---")]
        public int playerId = 1;
        public bool isLocalPlayer = false;

        [Header("--- Configuración de Transmisión ---")]
        [SerializeField] private float sendRate = 30f;
        [SerializeField] private float interpolationSpeed = 18f;

        private Animator playerAnimator;
        private ControladorCombate combate;
        private CharacterController charController;

        private Vector3 networkTargetPosition;
        private Quaternion networkTargetRotation;
        private string lastRemoteAnim = "";
        private float lastSendTime = 0f;
        private float sendInterval;

        private void Awake()
        {
            sendInterval = 1f / sendRate;
            networkTargetPosition = transform.position;
            networkTargetRotation = transform.rotation;

            playerAnimator = GetComponent<Animator>();
            combate = GetComponent<ControladorCombate>();
            charController = GetComponent<CharacterController>();
        }

        public void Initialize(int id, bool isLocal)
        {
            this.playerId = id;
            this.isLocalPlayer = isLocal;
            networkTargetPosition = transform.position;
            networkTargetRotation = transform.rotation;

            if (combate != null)
            {
                combate.esRemotoEnRed = !isLocal;

                if (isLocal)
                {
                    // El jugador local en cada PC siempre usa los controles principales (WASD / JKL / Mando 1)
                    combate.usarControlesP1EnRed = true;
                }
            }

            if (!isLocal && charController != null)
            {
                charController.enabled = false;
            }
        }

        private void Update()
        {
            if (NetworkManager.Instance == null || !NetworkManager.Instance.IsConnected) return;

            if (isLocalPlayer)
            {
                if (Time.time - lastSendTime >= sendInterval)
                {
                    lastSendTime = Time.time;
                    BroadcastCurrentState();
                }
            }
            else
            {
                HandleRemotePlayerInterpolation();
            }
        }

        private void BroadcastCurrentState()
        {
            string animActual = "idle_A";
            if (combate != null && !string.IsNullOrEmpty(combate.ultimaAnimacionReproducida))
            {
                animActual = combate.ultimaAnimacionReproducida;
            }

            float vida = (combate != null) ? combate.ObtenerVidaActual() : 100f;

            PlayerSyncPacket packet = new PlayerSyncPacket
            {
                playerId = this.playerId,
                posX = transform.position.x,
                posY = transform.position.y,
                posZ = transform.position.z,
                rotY = transform.eulerAngles.y,
                currentAnim = animActual,
                vidaActual = vida
            };

            NetworkManager.Instance.SendPacket(packet);
        }

        public void ApplySyncData(PlayerSyncPacket data)
        {
            if (isLocalPlayer) return;

            networkTargetPosition = new Vector3(data.posX, data.posY, data.posZ);
            networkTargetRotation = Quaternion.Euler(0f, data.rotY, 0f);

            // Reproducir exactamente el mismo estado del Animator por nombre (CrossFade)
            if (!string.IsNullOrEmpty(data.currentAnim) && data.currentAnim != lastRemoteAnim && playerAnimator != null)
            {
                int hash = Animator.StringToHash(data.currentAnim);
                if (playerAnimator.HasState(0, hash))
                {
                    playerAnimator.CrossFade(data.currentAnim, 0.08f);
                    lastRemoteAnim = data.currentAnim;
                }
            }

            // Mantener sincronizada la barra de vida visual del oponente
            if (combate != null)
            {
                combate.SincronizarVidaDesdeRed(data.vidaActual);
            }
        }

        public void ExecuteRemoteAction(string actionName)
        {
            if (isLocalPlayer || combate == null) return;

            if (actionName == "Fireball")
            {
                combate.DispararBolaFuegoVisualRemota();
            }
        }

        private void HandleRemotePlayerInterpolation()
        {
            if (Vector3.Distance(transform.position, networkTargetPosition) > 5.0f)
            {
                transform.position = networkTargetPosition;
                transform.rotation = networkTargetRotation;
                return;
            }

            transform.position = Vector3.Lerp(transform.position, networkTargetPosition, Time.deltaTime * interpolationSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, networkTargetRotation, Time.deltaTime * interpolationSpeed);
        }
    }
}