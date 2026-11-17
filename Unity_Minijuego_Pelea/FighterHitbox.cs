using System.Collections.Generic;
using UnityEngine;

namespace JuegoPelea.IA
{
    /// <summary>
    /// Componente de Hitbox físico/trigger para puños, patadas o armas en juegos de pelea.
    /// Funciona tanto en 3D (Collider) como en 2D (Collider2D).
    /// </summary>
    public class FighterHitbox : MonoBehaviour
    {
        [Header("Configuración del Golpe")]
        [Tooltip("Daño base que inflige este golpe.")]
        [SerializeField] private float damage = 15f;

        [Tooltip("Si el golpe es imparable y rompe el bloqueo/guardia.")]
        [SerializeField] private bool isUnblockable = false;

        [Tooltip("Tag del bando que origina el ataque (para no auto-golpearse).")]
        [SerializeField] private string ownerTag = "Player";

        [Tooltip("Tag del rival objetivo.")]
        [SerializeField] private string targetTag = "Enemy";

        [Header("Efectos Opcionales")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private AudioClip hitSound;

        private Collider col3D;
        private Collider2D col2D;
        private readonly HashSet<GameObject> hitTargets = new HashSet<GameObject>();

        private void Awake()
        {
            col3D = GetComponent<Collider>();
            col2D = GetComponent<Collider2D>();

            // Desactivar hitbox por defecto al iniciar
            DisableHitbox();
        }

        /// <summary>
        /// Activa la ventana activa del golpe (usado típicamente en Animation Events).
        /// </summary>
        public void EnableHitbox()
        {
            hitTargets.Clear();

            if (col3D != null) col3D.enabled = true;
            if (col2D != null) col2D.enabled = true;

            // Si es un ataque del jugador, avisar en un radio al bot para que pueda evaluar su reacción humana
            if (ownerTag == "Player")
            {
                NotifySurroundingBots();
            }
        }

        /// <summary>
        /// Desactiva la colisión del golpe al terminar la animación activa.
        /// </summary>
        public void DisableHitbox()
        {
            hitTargets.Clear();
            if (col3D != null) col3D.enabled = false;
            if (col2D != null) col2D.enabled = false;
        }

        private void NotifySurroundingBots()
        {
            // Radio de alerta para que la IA perciba el golpe con su tiempo de reacción
            Collider[] colliders = Physics.OverlapSphere(transform.position, 4.0f);
            foreach (Collider c in colliders)
            {
                if (c.CompareTag(targetTag))
                {
                    FighterBotAI bot = c.GetComponent<FighterBotAI>();
                    if (bot != null)
                    {
                        bot.NotifyIncomingAttack(transform.position, isUnblockable);
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleHit(other.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleHit(other.gameObject);
        }

        private void HandleHit(GameObject hitObj)
        {
            if (hitObj.CompareTag(ownerTag)) return;
            if (!hitObj.CompareTag(targetTag)) return;

            // Evitar golpear varias veces al mismo objetivo en un solo swing
            if (hitTargets.Contains(hitObj)) return;
            hitTargets.Add(hitObj);

            // Intentar aplicar daño al Bot IA
            FighterBotAI bot = hitObj.GetComponent<FighterBotAI>();
            if (bot != null)
            {
                bot.TakeDamage(damage, transform.position, isUnblockable);
                SpawnEffects(hitObj.transform.position);
                return;
            }

            // O enviar mensaje genérico a cualquier otro script de salud
            hitObj.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            SpawnEffects(hitObj.transform.position);
        }

        private void SpawnEffects(Vector3 contactPoint)
        {
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, contactPoint, Quaternion.identity);
            }

            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, contactPoint);
            }
        }
    }
}

