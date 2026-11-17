using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace JuegoPelea.IA
{
    /// <summary>
    /// Modo dimensional para la arena de pelea.
    /// </summary>
    public enum CombatDimension
    {
        [Tooltip("Pelea 3D libre en plano X-Z (estilo Tekken, Naruto Storm, Sifu).")]
        ThreeD_Arena_XZ,

        [Tooltip("Pelea 2D lateral / 2.5D en plano X-Y (estilo Street Fighter, Smash Bros, Mortal Kombat).")]
        TwoD_SideScroller_XY,

        [Tooltip("Pelea 2D Top-Down en plano X-Y vista aérea (estilo Beat 'em Up clásico).")]
        TwoD_TopDown_XY
    }

    /// <summary>
    /// Máquina de Estados Finita (FSM) para el comportamiento del Bot.
    /// </summary>
    public enum BotCombatState
    {
        Idle,               // Pausa táctica / respiración
        SpacingFootsies,    // Neutral game: se mueve en círculos/laterales, mide distancia, amaga
        Approaching,        // Avanzando hacia el jugador para acortar distancia
        Attacking,          // Ejecutando golpe o combo
        Dodging,            // Esquivando (dash atrás, rodar o paso lateral)
        Blocking,           // Bloqueando / Guardia activa
        VulnerableRecovery, // Ventana de castigo: el bot falló un golpe o terminó de esquivar y queda expuesto
        HitStun,            // Aturdido tras recibir un golpe
        Dead                // Derrotado
    }

    /// <summary>
    /// Tipo de ataque seleccionado por la IA.
    /// </summary>
    public enum AttackType
    {
        None,
        Light,
        Heavy,
        ComboChain
    }

    /// <summary>
    /// Controlador de Inteligencia Artificial Avanzado para Bots en Minijuegos de Pelea.
    /// 
    /// Diseñado con "Dificultad Intermedia Justa":
    /// - Reacción Humana con Retraso (200ms - 380ms) en lugar de lectura instantánea de inputs.
    /// - Probabilidad de Esquive Equilibrada (~55-65%) permitiendo que el jugador conecte combos.
    /// - Cooldown de Esquive: Evita que el bot sea intocable; si el jugador amaga, puede castigarlo.
    /// - Ventana de Castigo (Whiff Punish): Cuando el bot falla un ataque fuerte, queda vulnerable.
    /// - Sistema de Espaciado (Footsies): Movimiento orgánico adelante/atrás y lateral.
    /// </summary>
    [SelectionBase]
    public class FighterBotAI : MonoBehaviour
    {
        [Header("--- Modo de Combate y Referencias ---")]
        [Tooltip("Tipo de espacio en el que se desarrolla el combate.")]
        [SerializeField] private CombatDimension dimensionMode = CombatDimension.ThreeD_Arena_XZ;

        [Tooltip("Transform del oponente (Jugador). Si se deja vacío, buscará por tag 'Player'.")]
        [SerializeField] private Transform playerTarget;

        [Tooltip("Componente Animator opcional para sincronizar animaciones.")]
        [SerializeField] private Animator botAnimator;

        [Header("--- Movimiento y Navegación ---")]
        [Tooltip("Velocidad normal de desplazamiento hacia el jugador.")]
        [SerializeField] private float moveSpeed = 4.2f;

        [Tooltip("Velocidad durante el juego neutral (amagues, espaciado lateral).")]
        [SerializeField] private float footsieSpeed = 2.8f;

        [Tooltip("Velocidad de giro o alineación hacia el rival.")]
        [SerializeField] private float rotationSpeed = 10f;

        [Tooltip("Impulso / velocidad durante el esquive rápido (dash/roll).")]
        [SerializeField] private float dodgeSpeed = 8.5f;

        [Tooltip("Duración física del movimiento de esquive en segundos.")]
        [SerializeField] private float dodgeDuration = 0.28f;

        [Header("--- Rangos de Combate ---")]
        [Tooltip("Distancia máxima a la que el bot detecta y persigue al jugador.")]
        [SerializeField] private float visionRange = 14f;

        [Tooltip("Distancia ideal para mantener espaciado neutral (Footsies).")]
        [SerializeField] private float optimalSpacingDistance = 2.5f;

        [Tooltip("Distancia para conectar golpes ligeros.")]
        [SerializeField] private float lightAttackRange = 1.6f;

        [Tooltip("Distancia para conectar golpes pesados o de embestida.")]
        [SerializeField] private float heavyAttackRange = 2.4f;

        [Header("--- Calibración de Dificultad Intermedia ---")]
        [Range(0.15f, 0.6f)]
        [Tooltip("Tiempo mínimo de reacción humana antes de reaccionar a un ataque del rival (segundos).")]
        [SerializeField] private float reactionDelayMin = 0.20f;

        [Range(0.2f, 0.8f)]
        [Tooltip("Tiempo máximo de reacción humana.")]
        [SerializeField] private float reactionDelayMax = 0.35f;

        [Range(0f, 100f)]
        [Tooltip("Probabilidad (%) de intentar esquivar un ataque que detecta. Un 60% es ideal para nivel intermedio.")]
        [SerializeField] private float dodgeProbability = 60f;

        [Range(0f, 100f)]
        [Tooltip("Si decide no esquivar (o el esquive está en cooldown), probabilidad de levantar guardia/bloquear.")]
        [SerializeField] private float blockProbability = 50f;

        [Tooltip("Tiempo de enfriamiento entre esquives. Impide que esquive indefinidamente.")]
        [SerializeField] private float dodgeCooldown = 2.2f;

        [Tooltip("Ventana de vulnerabilidad tras esquivar o fallar un ataque donde el bot NO puede defenderse.")]
        [SerializeField] private float whiffRecoveryDuration = 0.60f;

        [Range(0.1f, 1f)]
        [Tooltip("Índice de agresividad (0.1 = muy cauteloso/defensivo, 0.9 = extremadamente agresivo).")]
        [SerializeField] private float aggressionRate = 0.60f;

        [Tooltip("Tiempo mínimo entre ataques consecutivos del bot.")]
        [SerializeField] private float attackCooldown = 1.3f;

        [Header("--- Estadísticas de Combate ---")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;

        [Range(0f, 1f)]
        [Tooltip("Porcentaje de daño que absorbe el escudo durante el bloqueo (0.75 = recibe solo 25% del daño).")]
        [SerializeField] private float blockDamageAbsorption = 0.75f;

        [Tooltip("Tiempo de aturdimiento al recibir un golpe limpio (segundos).")]
        [SerializeField] private float hitStunTime = 0.45f;

        [Header("--- Eventos de Unity (Desacoplados) ---")]
        public UnityEvent<AttackType> OnAttackTriggered;
        public UnityEvent OnDodgeStarted;
        public UnityEvent OnBlockStarted;
        public UnityEvent OnBlockEnded;
        public UnityEvent<float> OnTakeDamage;
        public UnityEvent OnHitStun;
        public UnityEvent OnDefeated;

        [Header("--- Depuración y Gizmos ---")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private bool logStateChanges = false;

        // Estado público accesible
        public BotCombatState CurrentState => currentState;
        public float CurrentHealth => currentHealth;
        public bool IsVulnerable => currentState == BotCombatState.VulnerableRecovery;
        public bool IsBlocking => currentState == BotCombatState.Blocking;
        public bool IsDead => currentState == BotCombatState.Dead;

        // Componentes físicos detectados dinámicamente
        private CharacterController characterController;
        private Rigidbody rb3D;
        private Rigidbody2D rb2D;

        // Máquina de estados interna
        private BotCombatState currentState = BotCombatState.Idle;
        private float stateTimer = 0f;
        private float nextAllowedAttackTime = 0f;
        private float nextAllowedDodgeTime = 0f;

        // Variables de movimiento de espaciado (footsies)
        private Vector3 footsieDirection = Vector3.zero;
        private float nextFootsieChangeTime = 0f;
        private Vector3 dodgeDirection = Vector3.zero;

        // Corrutinas activas
        private Coroutine activeAttackRoutine;
        private Coroutine activeDodgeRoutine;
        private Coroutine activeReactionRoutine;
        private Coroutine activeHitStunRoutine;

        // Parámetros de Animator Hasheados para máximo rendimiento
        private static readonly int AnimSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimState = Animator.StringToHash("State");
        private static readonly int AnimLightAttack = Animator.StringToHash("LightAttack");
        private static readonly int AnimHeavyAttack = Animator.StringToHash("HeavyAttack");
        private static readonly int AnimDodge = Animator.StringToHash("Dodge");
        private static readonly int AnimIsBlocking = Animator.StringToHash("IsBlocking");
        private static readonly int AnimHit = Animator.StringToHash("Hit");
        private static readonly int AnimDie = Animator.StringToHash("Die");

        private void Awake()
        {
            currentHealth = maxHealth;

            // Detección automática de física
            characterController = GetComponent<CharacterController>();
            rb3D = GetComponent<Rigidbody>();
            rb2D = GetComponent<Rigidbody2D>();

            if (botAnimator == null)
            {
                botAnimator = GetComponentInChildren<Animator>();
            }
        }

        private void Start()
        {
            // Auto-localizar al jugador si no se asignó en el Inspector
            if (playerTarget == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    playerTarget = playerObj.transform;
                }
            }

            ChangeState(BotCombatState.Idle);
        }

        private void Update()
        {
            if (currentState == BotCombatState.Dead) return;

            // Si el jugador no existe o está inactivo, permanecer ocioso
            if (playerTarget == null)
            {
                ChangeState(BotCombatState.Idle);
                UpdateAnimator(0f);
                return;
            }

            float distanceToPlayer = GetDistanceToPlayer();

            // Orientación continua hacia el jugador en estados no bloqueantes
            if (CanRotateTowardsPlayer())
            {
                FaceTarget(playerTarget.position);
            }

            // Ejecución de la lógica de cada estado
            switch (currentState)
            {
                case BotCombatState.Idle:
                    HandleIdleState(distanceToPlayer);
                    break;

                case BotCombatState.Approaching:
                    HandleApproachingState(distanceToPlayer);
                    break;

                case BotCombatState.SpacingFootsies:
                    HandleSpacingFootsiesState(distanceToPlayer);
                    break;

                case BotCombatState.Attacking:
                    // Manejado por corrutina de ataque
                    break;

                case BotCombatState.Dodging:
                    // Manejado por corrutina de esquive
                    break;

                case BotCombatState.Blocking:
                    HandleBlockingState(distanceToPlayer);
                    break;

                case BotCombatState.VulnerableRecovery:
                    HandleVulnerableState();
                    break;

                case BotCombatState.HitStun:
                    // Manejado por corrutina de aturdimiento
                    break;
            }
        }

        #region --- Comportamiento de Estados ---

        private void HandleIdleState(float distanceToPlayer)
        {
            UpdateAnimator(0f);
            stateTimer -= Time.deltaTime;

            if (distanceToPlayer > visionRange)
            {
                // Fuera de visión: relajado
                return;
            }

            if (stateTimer <= 0f)
            {
                // Toma de decisión táctica
                if (distanceToPlayer > heavyAttackRange)
                {
                    ChangeState(BotCombatState.Approaching);
                }
                else
                {
                    // En rango de combate: decidir entre atacar de inmediato o jugar al espaciado/footsies
                    bool wantsToAttack = Random.value < aggressionRate && Time.time >= nextAllowedAttackTime;
                    if (wantsToAttack)
                    {
                        DecideAndExecuteAttack(distanceToPlayer);
                    }
                    else
                    {
                        ChangeState(BotCombatState.SpacingFootsies);
                    }
                }
            }
        }

        private void HandleApproachingState(float distanceToPlayer)
        {
            if (distanceToPlayer <= optimalSpacingDistance)
            {
                // Ya llegó a la zona de contacto
                bool wantsToAttack = Random.value < aggressionRate && Time.time >= nextAllowedAttackTime;
                if (wantsToAttack)
                {
                    DecideAndExecuteAttack(distanceToPlayer);
                }
                else
                {
                    ChangeState(BotCombatState.SpacingFootsies);
                }
                return;
            }

            // Avanzar hacia el jugador
            Vector3 direction = GetDirectionToPlayer();
            MoveBot(direction * moveSpeed);
            UpdateAnimator(1f);

            // Si el jugador se aleja demasiado, seguir persiguiendo
            if (distanceToPlayer > visionRange * 1.5f)
            {
                ChangeState(BotCombatState.Idle);
            }
        }

        private void HandleSpacingFootsiesState(float distanceToPlayer)
        {
            stateTimer -= Time.deltaTime;

            // Recalcular dirección de footsies periódicamente
            if (Time.time >= nextFootsieChangeTime)
            {
                PickNewFootsieDirection(distanceToPlayer);
                nextFootsieChangeTime = Time.time + Random.Range(0.4f, 1.0f);
            }

            // Mover en la dirección de espaciado calculada
            MoveBot(footsieDirection * footsieSpeed);
            UpdateAnimator(0.5f);

            // Evaluar oportunidad de ataque durante el espaciado
            if (Time.time >= nextAllowedAttackTime && distanceToPlayer <= heavyAttackRange)
            {
                float attackRoll = Random.value;
                if (attackRoll < (aggressionRate * 0.8f))
                {
                    DecideAndExecuteAttack(distanceToPlayer);
                    return;
                }
            }

            // Fin del ciclo de espaciado: volver a evaluar
            if (stateTimer <= 0f)
            {
                if (distanceToPlayer > heavyAttackRange * 1.3f)
                {
                    ChangeState(BotCombatState.Approaching);
                }
                else
                {
                    // Pausa breve para respirar
                    stateTimer = Random.Range(0.2f, 0.5f);
                    ChangeState(BotCombatState.Idle);
                }
            }
        }

        private void PickNewFootsieDirection(float distanceToPlayer)
        {
            Vector3 toPlayer = GetDirectionToPlayer();
            Vector3 lateral = Vector3.zero;

            if (dimensionMode == CombatDimension.ThreeD_Arena_XZ)
            {
                lateral = Vector3.Cross(Vector3.up, toPlayer).normalized;
            }
            else // 2D SideScroller o TopDown
            {
                lateral = new Vector3(-toPlayer.y, toPlayer.x, 0f).normalized;
            }

            // Decidir patrón de espaciado:
            // 1. Retroceder ligeramente si está muy cerca
            // 2. Acercarse con amague si está algo lejos
            // 3. Flanquear hacia la izquierda o derecha
            float behaviorChoice = Random.value;

            if (distanceToPlayer < lightAttackRange * 0.9f)
            {
                // Demasiado cerca: retroceder para ganar aire
                footsieDirection = -toPlayer + (Random.value > 0.5f ? lateral : -lateral) * 0.4f;
            }
            else if (behaviorChoice < 0.35f)
            {
                // Flanqueo a la derecha
                footsieDirection = lateral;
            }
            else if (behaviorChoice < 0.70f)
            {
                // Flanqueo a la izquierda
                footsieDirection = -lateral;
            }
            else if (behaviorChoice < 0.88f)
            {
                // Amague hacia adelante
                footsieDirection = toPlayer * 0.8f;
            }
            else
            {
                // Paso atrás medido
                footsieDirection = -toPlayer * 0.9f;
            }

            footsieDirection.Normalize();
        }

        private void HandleBlockingState(float distanceToPlayer)
        {
            stateTimer -= Time.deltaTime;
            UpdateAnimator(0f);

            // Mantener guardia hasta que termine el temporizador
            if (stateTimer <= 0f)
            {
                StopBlocking();
                // Tras bloquear, queda con una leve pausa táctica
                ChangeState(BotCombatState.SpacingFootsies);
            }
        }

        private void HandleVulnerableState()
        {
            stateTimer -= Time.deltaTime;
            UpdateAnimator(0f);

            // El bot no puede moverse ni defenderse en este periodo (Whiff Punish)
            if (stateTimer <= 0f)
            {
                ChangeState(BotCombatState.SpacingFootsies);
            }
        }

        #endregion

        #region --- Sistema de Ataque y Combos ---

        private void DecideAndExecuteAttack(float distanceToPlayer)
        {
            AttackType selectedAttack = AttackType.Light;

            if (distanceToPlayer <= lightAttackRange)
            {
                // En rango cercano: 60% Ataque Ligero, 40% Combo
                selectedAttack = (Random.value < 0.60f) ? AttackType.Light : AttackType.ComboChain;
            }
            else if (distanceToPlayer <= heavyAttackRange)
            {
                // En rango medio: Ataque Fuerte / Embestida
                selectedAttack = AttackType.Heavy;
            }
            else
            {
                ChangeState(BotCombatState.Approaching);
                return;
            }

            ChangeState(BotCombatState.Attacking);
            activeAttackRoutine = StartCoroutine(ExecuteAttackRoutine(selectedAttack));
        }

        private IEnumerator ExecuteAttackRoutine(AttackType attackType)
        {
            UpdateAnimator(0f);
            nextAllowedAttackTime = Time.time + attackCooldown + Random.Range(-0.2f, 0.4f);

            if (attackType == AttackType.Light)
            {
                // --- ATAQUE LIGERO: Rápido, poco castigo ---
                TriggerAnimatorAction(AnimLightAttack);
                OnAttackTriggered?.Invoke(AttackType.Light);

                yield return new WaitForSeconds(0.20f); // Anticipación ligera
                // Aquí el bot lanza su hitbox (gestionado por eventos o animaciones)
                yield return new WaitForSeconds(0.25f); // Tiempo activo

                // Recuperación breve
                EnterVulnerability(whiffRecoveryDuration * 0.5f);
            }
            else if (attackType == AttackType.Heavy)
            {
                // --- ATAQUE FUERTE: Gran anticipación (telegrafiado) para que el jugador pueda reaccionar ---
                TriggerAnimatorAction(AnimHeavyAttack);
                OnAttackTriggered?.Invoke(AttackType.Heavy);

                // El telegrafiado permite al jugador ver el golpe venir y castigarlo
                float telegraphWindup = 0.38f;
                float elapsed = 0f;
                while (elapsed < telegraphWindup)
                {
                    // Puede avanzar un poco hacia adelante durante el impulso del golpe pesado
                    Vector3 lungeDir = GetDirectionToPlayer();
                    MoveBot(lungeDir * (moveSpeed * 0.5f));
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                yield return new WaitForSeconds(0.25f); // Impacto activo

                // Ventana de Castigo Completa (si falla, el jugador tiene tiempo de oro para contraatacar)
                EnterVulnerability(whiffRecoveryDuration);
            }
            else if (attackType == AttackType.ComboChain)
            {
                // --- COMBO DE 2 A 3 GOLPES ---
                int comboHits = Random.value < 0.5f ? 2 : 3;

                for (int i = 0; i < comboHits; i++)
                {
                    TriggerAnimatorAction(AnimLightAttack);
                    OnAttackTriggered?.Invoke(AttackType.Light);

                    // Pequeño desplazamiento hacia adelante en cada golpe
                    MoveBot(GetDirectionToPlayer() * (moveSpeed * 0.4f));

                    yield return new WaitForSeconds(0.28f);

                    // Pequeña probabilidad de que el bot "dropee" el combo en nivel intermedio
                    if (i == 1 && Random.value < 0.25f)
                    {
                        break;
                    }
                }

                // Recuperación post-combo
                EnterVulnerability(whiffRecoveryDuration * 0.8f);
            }
        }

        private void EnterVulnerability(float duration)
        {
            stateTimer = duration;
            ChangeState(BotCombatState.VulnerableRecovery);
        }

        #endregion

        #region --- Sistema de Reacción Táctica y Esquive (Dificultad Intermedia) ---

        /// <summary>
        /// Método invocado externamente cuando el jugador inicia un ataque hacia el bot
        /// o cuando un hitbox entra en su rango de alerta.
        /// </summary>
        /// <param name="attackerPosition">Posición de origen del ataque.</param>
        /// <param name="isUnblockable">Si el ataque rompe guardia y no se puede bloquear.</param>
        public void NotifyIncomingAttack(Vector3 attackerPosition, bool isUnblockable = false)
        {
            if (currentState == BotCombatState.Dead ||
                currentState == BotCombatState.HitStun ||
                currentState == BotCombatState.VulnerableRecovery ||
                currentState == BotCombatState.Dodging)
            {
                // El bot está ocupado, aturdido o vulnerable; no puede defenderse
                return;
            }

            // Si ya está calculando una reacción previa, ignorar para evitar duplicados
            if (activeReactionRoutine != null) return;

            activeReactionRoutine = StartCoroutine(ProcessHumanReactionRoutine(attackerPosition, isUnblockable));
        }

        private IEnumerator ProcessHumanReactionRoutine(Vector3 attackerPosition, bool isUnblockable)
        {
            // RETRASO HUMANO SIMULADO: Crucial para evitar que parezca un tramposo que lee inputs en frame 0
            float humanReactionTime = Random.Range(reactionDelayMin, reactionDelayMax);
            yield return new WaitForSeconds(humanReactionTime);

            // Verificar si aún se encuentra en un estado defendible tras el retraso
            if (currentState == BotCombatState.HitStun || currentState == BotCombatState.Dead || currentState == BotCombatState.VulnerableRecovery)
            {
                activeReactionRoutine = null;
                yield break;
            }

            // Tirada de dados de Dificultad Intermedia:
            bool canDodge = Time.time >= nextAllowedDodgeTime;
            float roll = Random.Range(0f, 100f);

            if (canDodge && roll < dodgeProbability)
            {
                // ¡Éxito en Esquive!
                ExecuteDodge(attackerPosition);
            }
            else if (!isUnblockable && roll < (dodgeProbability + blockProbability))
            {
                // Esquive fallido o en cooldown: Levanta la Guardia
                ExecuteBlock();
            }
            else
            {
                // El bot comete un error de cálculo humano (40% de las veces) y se queda descubierto
                // Permitiendo que el jugador conecte satisfactoriamente su golpe
                if (logStateChanges)
                {
                    Debug.Log($"<color=orange>[Bot IA]</color> El bot no logró reaccionar a tiempo (Fallo humano simulado).");
                }
            }

            activeReactionRoutine = null;
        }

        private void ExecuteDodge(Vector3 attackerPosition)
        {
            // Interrumpir ataque en progreso si estaba haciendo uno
            if (activeAttackRoutine != null)
            {
                StopCoroutine(activeAttackRoutine);
                activeAttackRoutine = null;
            }

            nextAllowedDodgeTime = Time.time + dodgeCooldown;
            ChangeState(BotCombatState.Dodging);

            // Calcular dirección de escape inteligente
            Vector3 fromAttacker = (transform.position - attackerPosition).normalized;
            Vector3 lateral = Vector3.zero;

            if (dimensionMode == CombatDimension.ThreeD_Arena_XZ)
            {
                fromAttacker.y = 0f;
                lateral = Vector3.Cross(Vector3.up, fromAttacker).normalized;
            }
            else
            {
                lateral = new Vector3(-fromAttacker.y, fromAttacker.x, 0f).normalized;
            }

            // 65% probabilidad de dash hacia atrás, 35% de paso lateral evasivo
            float evasionStyle = Random.value;
            if (evasionStyle < 0.65f)
            {
                dodgeDirection = fromAttacker; // Back dash
            }
            else
            {
                dodgeDirection = (Random.value < 0.5f ? lateral : -lateral); // Side step
            }

            if (dodgeDirection.sqrMagnitude < 0.01f)
            {
                dodgeDirection = -transform.forward;
            }

            activeDodgeRoutine = StartCoroutine(DodgeImpulseRoutine());
        }

        private IEnumerator DodgeImpulseRoutine()
        {
            TriggerAnimatorAction(AnimDodge);
            OnDodgeStarted?.Invoke();

            float elapsed = 0f;
            while (elapsed < dodgeDuration)
            {
                // Aplicar aceleración de esquive
                MoveBot(dodgeDirection * dodgeSpeed);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Tras esquivar, queda brevemente vulnerable si el jugador anticipó la posición
            EnterVulnerability(whiffRecoveryDuration * 0.4f);
        }

        private void ExecuteBlock()
        {
            ChangeState(BotCombatState.Blocking);
            stateTimer = Random.Range(0.6f, 1.2f); // Mantiene guardia por un momento
            SetAnimatorBool(AnimIsBlocking, true);
            OnBlockStarted?.Invoke();
        }

        private void StopBlocking()
        {
            SetAnimatorBool(AnimIsBlocking, false);
            OnBlockEnded?.Invoke();
        }

        #endregion

        #region --- Sistema de Daño y Aturdimiento ---

        /// <summary>
        /// Procesa el impacto recibido desde cualquier ataque del jugador.
        /// </summary>
        /// <param name="damageAmount">Cantidad de daño base.</param>
        /// <param name="hitOrigin">Origen del impacto para calcular retroceso.</param>
        /// <param name="canBreakGuard">Si el golpe rompe guardia obligatoriamente.</param>
        public void TakeDamage(float damageAmount, Vector3 hitOrigin, bool canBreakGuard = false)
        {
            if (currentState == BotCombatState.Dead) return;

            // Caso 1: Guardia Activa
            if (currentState == BotCombatState.Blocking && !canBreakGuard)
            {
                float damageTaken = damageAmount * (1f - blockDamageAbsorption);
                currentHealth = Mathf.Max(0f, currentHealth - damageTaken);
                OnTakeDamage?.Invoke(damageTaken);

                // Pequeño retroceso en escudo
                Vector3 knockbackDir = (transform.position - hitOrigin).normalized;
                MoveBot(knockbackDir * (moveSpeed * 0.4f));

                if (currentHealth <= 0f)
                {
                    Die();
                }
                return;
            }

            // Caso 2: Golpe limpio (o Rompe-Guardia)
            // Cancelar cualquier acción en curso
            StopAllCombatCoroutines();
            StopBlocking();

            currentHealth = Mathf.Max(0f, currentHealth - damageAmount);
            OnTakeDamage?.Invoke(damageAmount);

            if (currentHealth <= 0f)
            {
                Die();
                return;
            }

            // Aplicar Aturdimiento (HitStun)
            activeHitStunRoutine = StartCoroutine(HitStunRoutine(hitOrigin));
        }

        private IEnumerator HitStunRoutine(Vector3 hitOrigin)
        {
            ChangeState(BotCombatState.HitStun);
            TriggerAnimatorAction(AnimHit);
            OnHitStun?.Invoke();

            // Retroceso por impacto
            Vector3 pushDir = (transform.position - hitOrigin).normalized;
            float stunElapsed = 0f;

            while (stunElapsed < hitStunTime)
            {
                if (stunElapsed < hitStunTime * 0.4f)
                {
                    MoveBot(pushDir * (moveSpeed * 0.8f));
                }
                stunElapsed += Time.deltaTime;
                yield return null;
            }

            // Recuperación tras el golpe: intentar ganar espacio inmediatamente
            ChangeState(BotCombatState.SpacingFootsies);
            stateTimer = Random.Range(0.6f, 1.2f);
        }

        private void Die()
        {
            StopAllCombatCoroutines();
            ChangeState(BotCombatState.Dead);
            TriggerAnimatorAction(AnimDie);
            UpdateAnimator(0f);
            OnDefeated?.Invoke();

            if (logStateChanges)
            {
                Debug.Log($"<color=red>[Bot IA]</color> ¡Bot derrotado!");
            }
        }

        private void StopAllCombatCoroutines()
        {
            if (activeAttackRoutine != null) { StopCoroutine(activeAttackRoutine); activeAttackRoutine = null; }
            if (activeDodgeRoutine != null) { StopCoroutine(activeDodgeRoutine); activeDodgeRoutine = null; }
            if (activeReactionRoutine != null) { StopCoroutine(activeReactionRoutine); activeReactionRoutine = null; }
            if (activeHitStunRoutine != null) { StopCoroutine(activeHitStunRoutine); activeHitStunRoutine = null; }
        }

        #endregion

        #region --- Movimiento, Orientación y Física ---

        private void MoveBot(Vector3 velocity)
        {
            // Filtrar ejes según la dimensión elegida
            Vector3 finalVelocity = SanitizeVectorForDimension(velocity);

            if (characterController != null && characterController.enabled)
            {
                // Si el motor 3D usa gravedad simple
                if (dimensionMode == CombatDimension.ThreeD_Arena_XZ && !characterController.isGrounded)
                {
                    finalVelocity.y += Physics.gravity.y * Time.deltaTime;
                }
                characterController.Move(finalVelocity * Time.deltaTime);
            }
            else if (rb3D != null && !rb3D.isKinematic)
            {
                Vector3 newPos = rb3D.position + finalVelocity * Time.deltaTime;
                rb3D.MovePosition(newPos);
            }
            else if (rb2D != null && !rb2D.isKinematic)
            {
                Vector2 newPos2D = rb2D.position + new Vector2(finalVelocity.x, finalVelocity.y) * Time.deltaTime;
                rb2D.MovePosition(newPos2D);
            }
            else
            {
                // Movimiento directo por Transform como alternativa universal
                transform.position += finalVelocity * Time.deltaTime;
            }
        }

        private void FaceTarget(Vector3 targetPos)
        {
            Vector3 currentPos = transform.position;

            if (dimensionMode == CombatDimension.ThreeD_Arena_XZ)
            {
                Vector3 lookDirection = targetPos - currentPos;
                lookDirection.y = 0f;

                if (lookDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
            else if (dimensionMode == CombatDimension.TwoD_SideScroller_XY)
            {
                // En 2D Lateral: Invertir escala X para mirar hacia el oponente
                float dirX = targetPos.x - currentPos.x;
                if (Mathf.Abs(dirX) > 0.1f)
                {
                    Vector3 localScale = transform.localScale;
                    localScale.x = Mathf.Abs(localScale.x) * (dirX > 0 ? 1 : -1);
                    transform.localScale = localScale;
                }
            }
            else if (dimensionMode == CombatDimension.TwoD_TopDown_XY)
            {
                Vector3 dir = targetPos - currentPos;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                Quaternion targetRot = Quaternion.Euler(0, 0, angle);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }

        private bool CanRotateTowardsPlayer()
        {
            return currentState != BotCombatState.Dead &&
                   currentState != BotCombatState.HitStun &&
                   currentState != BotCombatState.Dodging;
        }

        private Vector3 SanitizeVectorForDimension(Vector3 vector)
        {
            switch (dimensionMode)
            {
                case CombatDimension.ThreeD_Arena_XZ:
                    return new Vector3(vector.x, 0f, vector.z);

                case CombatDimension.TwoD_SideScroller_XY:
                    return new Vector3(vector.x, 0f, 0f);

                case CombatDimension.TwoD_TopDown_XY:
                    return new Vector3(vector.x, vector.y, 0f);

                default:
                    return vector;
            }
        }

        private float GetDistanceToPlayer()
        {
            if (playerTarget == null) return float.MaxValue;

            Vector3 diff = playerTarget.position - transform.position;
            diff = SanitizeVectorForDimension(diff);
            return diff.magnitude;
        }

        private Vector3 GetDirectionToPlayer()
        {
            if (playerTarget == null) return transform.forward;

            Vector3 dir = (playerTarget.position - transform.position);
            dir = SanitizeVectorForDimension(dir);
            return dir.normalized;
        }

        #endregion

        #region --- Helpers y Animaciones ---

        private void ChangeState(BotCombatState newState)
        {
            if (currentState == newState) return;

            currentState = newState;

            if (logStateChanges)
            {
                Debug.Log($"<color=cyan>[Bot IA Estado]</color> {newState}");
            }

            if (botAnimator != null)
            {
                botAnimator.SetInteger(AnimState, (int)newState);
            }
        }

        private void UpdateAnimator(float speedNormalized)
        {
            if (botAnimator == null) return;
            botAnimator.SetFloat(AnimSpeed, speedNormalized);
        }

        private void TriggerAnimatorAction(int animHash)
        {
            if (botAnimator == null) return;
            botAnimator.SetTrigger(animHash);
        }

        private void SetAnimatorBool(int animHash, bool value)
        {
            if (botAnimator == null) return;
            botAnimator.SetBool(animHash, value);
        }

        #endregion

        #region --- Detección por Triggers / Hitboxes ---

        private void OnTriggerEnter(Collider other)
        {
            EvaluateIncomingTrigger(other.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            EvaluateIncomingTrigger(other.gameObject);
        }

        private void EvaluateIncomingTrigger(GameObject hitSource)
        {
            // Si el objeto atacante es un Hitbox del jugador o el propio jugador en ataque
            if (hitSource.CompareTag("PlayerAttack") || hitSource.CompareTag("Hitbox"))
            {
                NotifyIncomingAttack(hitSource.transform.position, isUnblockable: false);
            }
        }

        #endregion

        #region --- Gizmos y Depuración Visual en Editor ---

        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;

            Vector3 pos = transform.position;

            // Rango de Ataque Ligero (Rojo)
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(pos, lightAttackRange);

            // Rango de Ataque Pesado (Naranja)
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.4f);
            Gizmos.DrawWireSphere(pos, heavyAttackRange);

            // Distancia de Espaciado Neutral / Footsies (Amarillo)
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.35f);
            Gizmos.DrawWireSphere(pos, optimalSpacingDistance);

            // Rango de Visión (Cian)
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            Gizmos.DrawWireSphere(pos, visionRange);

#if UNITY_EDITOR
            // Etiqueta flotante con el estado actual
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;

            string label = $"[Bot IA: {currentState}]\nHP: {currentHealth:F0}/{maxHealth:F0}";
            UnityEditor.Handles.Label(pos + Vector3.up * 2.2f, label, style);
#endif
        }

        #endregion
    }
}

