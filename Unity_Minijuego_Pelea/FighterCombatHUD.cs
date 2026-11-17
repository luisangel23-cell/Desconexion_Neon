using UnityEngine;

namespace JuegoPelea.IA
{
    /// <summary>
    /// HUD Neón instantáneo para el minijuego de pelea.
    /// Funciona inmediatamente en OnGUI sin necesidad de configurar Canvas complejo en Unity,
    /// mostrando barras de salud de Jugador vs Bot y el estado táctico de la IA en tiempo real.
    /// </summary>
    public class FighterCombatHUD : MonoBehaviour
    {
        [Header("Referencias Opcionales (Auto-detecta si están vacías)")]
        [SerializeField] private PlayerFighterCombat player;
        [SerializeField] private FighterBotAI bot;

        [Header("Configuración Visual")]
        [SerializeField] private bool showHUD = true;

        private void Start()
        {
            if (player == null)
            {
#if UNITY_2023_1_OR_NEWER
                player = FindAnyObjectByType<PlayerFighterCombat>();
#else
                player = FindObjectOfType<PlayerFighterCombat>();
#endif
            }

            if (bot == null)
            {
#if UNITY_2023_1_OR_NEWER
                bot = FindAnyObjectByType<FighterBotAI>();
#else
                bot = FindObjectOfType<FighterBotAI>();
#endif
            }
        }

        private void OnGUI()
        {
            if (!showHUD) return;

            int screenWidth = Screen.width;

            // Estilo general
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            headerStyle.normal.textColor = Color.white;

            GUIStyle statusStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            // 1. Barra de Vida del JUGADOR (Esquina Superior Izquierda)
            float playerHp = player != null ? player.CurrentHealth : 100f;
            float playerMax = player != null ? player.MaxHealth : 100f;
            DrawHealthBar(new Rect(30, 25, 280, 26), playerHp / playerMax, new Color(0.2f, 0.7f, 1f), "JUGADOR (P1)");

            // 2. Barra de Vida del BOT IA (Esquina Superior Derecha)
            float botHp = bot != null ? bot.CurrentHealth : 100f;
            float botMax = 100f;
            DrawHealthBar(new Rect(screenWidth - 310, 25, 280, 26), botHp / botMax, new Color(1f, 0.25f, 0.35f), "BOT IA (RIVAL)");

            // 3. Indicador de Estado Táctico del Bot (Centro)
            if (bot != null)
            {
                string stateText = $"ESTADO IA: {bot.CurrentState.ToString().ToUpper()}";
                Color badgeColor = Color.gray;

                switch (bot.CurrentState)
                {
                    case BotCombatState.Attacking:
                        badgeColor = new Color(1f, 0.3f, 0.1f);
                        break;
                    case BotCombatState.Dodging:
                        badgeColor = new Color(0.1f, 0.9f, 0.8f);
                        break;
                    case BotCombatState.Blocking:
                        badgeColor = new Color(0.3f, 0.6f, 1f);
                        break;
                    case BotCombatState.VulnerableRecovery:
                        badgeColor = new Color(0.9f, 0.9f, 0.1f);
                        stateText += " [¡VULNERABLE - CASTIGA AHORA!]";
                        break;
                    case BotCombatState.HitStun:
                        badgeColor = new Color(0.9f, 0.1f, 0.1f);
                        break;
                }

                GUI.backgroundColor = badgeColor;
                GUI.Box(new Rect(screenWidth / 2f - 180, 25, 360, 30), stateText, statusStyle);
                GUI.backgroundColor = Color.white;
            }

            // 4. Panel de Controles / Atajos (Inferior Izquierda)
            GUI.Box(new Rect(20, Screen.height - 110, 320, 95), "CONTROLES:\n[WASD]: Moverse  |  [J / Clic Izq]: Golpe Rápido\n[K / Clic Der]: Golpe Fuerte  |  [L / Shift]: Esquivar\n[I / Espacio]: Bloqueo / Guardia");
        }

        private void DrawHealthBar(Rect rect, float fillPercentage, Color barColor, string label)
        {
            fillPercentage = Mathf.Clamp01(fillPercentage);

            // Fondo negro / gris
            GUI.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.85f);
            GUI.Box(rect, GUIContent.none);

            // Relleno de vida
            Rect fillRect = new Rect(rect.x + 2, rect.y + 2, (rect.width - 4) * fillPercentage, rect.height - 4);
            GUI.backgroundColor = barColor;
            GUI.Box(fillRect, GUIContent.none);

            // Texto descriptivo
            GUI.backgroundColor = Color.white;
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            labelStyle.normal.textColor = Color.white;
            GUI.Label(rect, $"{label}: {Mathf.CeilToInt(fillPercentage * 100)}%", labelStyle);
        }
    }
}

