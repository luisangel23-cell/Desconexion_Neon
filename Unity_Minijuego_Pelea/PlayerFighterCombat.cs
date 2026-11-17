using System.Collections;
using UnityEngine;

namespace JuegoPelea.IA
{
    /// <summary>
    /// Controlador de combate básico para el Jugador.
    /// Permite testear inmediatamente el duelo contra el FighterBotAI:
    /// - Movimiento WASD / Flechas.
    /// - Golpe Ligero [J] o [Clic Izquierdo].
    /// - Golpe Fuerte [K] o [Clic Derecho].
    /// - Esquive / Dash [L] o [Shift Izquierdo].
    /// - Bloqueo / Guardia [I] o [Espacio].
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PlayerFighterCombat : MonoBehaviour
    {
        [Header("Configuración de Movimiento")]
        [SerializeField] private CombatDimension dimension = CombatDimension.ThreeD_Arena_XZ;
        [SerializeField] private float moveSpeed = 5.0f;
        [SerializeField] private float rotationSpeed = 12.0f;

        [Header("Salud y Estado")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;

        [Header("Controles de Ataque y Defensa")]
        [SerializeField] private KeyCode lightAttackKey = KeyCode.J;
        [SerializeField] private KeyCode heavyAttackKey = KeyCode.K;
        [SerializeField] private KeyCode dodgeKey = KeyCode.L;
        [SerializeField] private KeyCode blockKey = KeyCode.I;

        [Header("Hitboxes del Jugador")]
        [Tooltip("Hitbox de puño o patada ligera.")]
        [SerializeField] private FighterHitbox lightHitbox;

        [Tooltip("Hitbox de golpe pesado o patada fuerte.")]
        [SerializeField] private FighterHitbox heavyHitbox;

        [Header("Animator Opcional")]
        [SerializeField] private Animator playerAnimator;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsBlocking { get; private set; }

        private CharacterController charController;
        private Rigidbody rb3D;
        private Rigidbody2D rb2D;
        private bool isAttacking = false;
        private bool isDodging = false;

        private void Awake()
        {
            gameObject.tag = "Player";
            currentHealth = maxHealth;

            charController = GetComponent<CharacterController>();
            rb3D = GetComponent<Rigidbody>();
            rb2D = GetComponent<Rigidbody2D>();

            if (playerAnimator == null)
            {
                playerAnimator = GetComponentInChildren<Animator>();
            }
        }

        private void Update()
        {
            if (currentHealth <= 0f) return;

            HandleBlockingInput();

            if (!IsBlocking && !isAttacking && !isDodging)
            {
                HandleMovementInput();
                HandleAttackInput();
                HandleDodgeInput();
            }
        }

        private void HandleMovementInput()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 moveDir = Vector3.zero;

            if (dimension == CombatDimension.ThreeD_Arena_XZ)
            {
                moveDir = new Vector3(h, 0f, v).normalized;
            }
            else if (dimension == CombatDimension.TwoD_SideScroller_XY)
            {
                moveDir = new Vector3(h, 0f, 0f).normalized;
            }
            else
            {
                moveDir = new Vector3(h, v, 0f).normalized;
            }

            if (moveDir.sqrMagnitude > 0.001f)
            {
                // Rotación suave hacia la dirección de movimiento
                if (dimension == CombatDimension.ThreeD_Arena_XZ)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                }

                // Movimiento
                if (charController != null && charController.enabled)
                {
                    charController.Move(moveDir * (moveSpeed * Time.deltaTime));
                }
                else if (rb3D != null && !rb3D.isKinematic)
                {
                    rb3D.MovePosition(rb3D.position + moveDir * (moveSpeed * Time.deltaTime));
                }
                else if (rb2D != null && !rb2D.isKinematic)
                {
                    rb2D.MovePosition(rb2D.position + (Vector2)moveDir * (moveSpeed * Time.deltaTime));
                }
                else
                {
                    transform.position += moveDir * (moveSpeed * Time.deltaTime);
                }

                if (playerAnimator != null) playerAnimator.SetFloat("Speed", 1f);
            }
            else
            {
                if (playerAnimator != null) playerAnimator.SetFloat("Speed", 0f);
            }
        }

        private void HandleAttackInput()
        {
            if (Input.GetKeyDown(lightAttackKey) || Input.GetMouseButtonDown(0))
            {
                StartCoroutine(PerformAttack(lightHitbox, 0.25f, "LightAttack"));
            }
            else if (Input.GetKeyDown(heavyAttackKey) || Input.GetMouseButtonDown(1))
            {
                StartCoroutine(PerformAttack(heavyHitbox, 0.50f, "HeavyAttack"));
            }
        }

        private IEnumerator PerformAttack(FighterHitbox hitbox, float duration, string animTrigger)
        {
            isAttacking = true;

            if (playerAnimator != null) playerAnimator.SetTrigger(animTrigger);

            // Avisar a los bots cercanos de que se lanzó un ataque para que evalúen esquivar con su tiempo de reacción
            Collider[] hits = Physics.OverlapSphere(transform.position, 5f);
            foreach (Collider c in hits)
            {
                if (c.CompareTag("Enemy"))
                {
                    FighterBotAI bot = c.GetComponent<FighterBotAI>();
                    if (bot != null)
                    {
                        bot.NotifyIncomingAttack(transform.position);
                    }
                }
            }

            yield return new WaitForSeconds(duration * 0.3f);

            if (hitbox != null) hitbox.EnableHitbox();

            yield return new WaitForSeconds(duration * 0.4f);

            if (hitbox != null) hitbox.DisableHitbox();

            yield return new WaitForSeconds(duration * 0.3f);

            isAttacking = false;
        }

        private void HandleDodgeInput()
        {
            if (Input.GetKeyDown(dodgeKey) || Input.GetKeyDown(KeyCode.LeftShift))
            {
                StartCoroutine(PerformDodge());
            }
        }

        private IEnumerator PerformDodge()
        {
            isDodging = true;
            if (playerAnimator != null) playerAnimator.SetTrigger("Dodge");

            Vector3 dashDir = -transform.forward;
            float elapsed = 0f;
            float dashTime = 0.25f;

            while (elapsed < dashTime)
            {
                transform.position += dashDir * (8f * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(0.15f);
            isDodging = false;
        }

        private void HandleBlockingInput()
        {
            IsBlocking = Input.GetKey(blockKey) || Input.GetKey(KeyCode.Space);
            if (playerAnimator != null) playerAnimator.SetBool("IsBlocking", IsBlocking);
        }

        public void TakeDamage(float amount)
        {
            if (IsBlocking)
            {
                amount *= 0.25f; // Absorbe 75%
            }

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            if (playerAnimator != null) playerAnimator.SetTrigger("Hit");

            if (currentHealth <= 0f)
            {
                if (playerAnimator != null) playerAnimator.SetTrigger("Die");
                Debug.Log("<color=red>[Jugador]</color> ¡K.O.! Has sido derrotado.");
            }
        }
    }
}

