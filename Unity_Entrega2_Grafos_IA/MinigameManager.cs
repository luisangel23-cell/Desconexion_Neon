using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DesconexionNeon.GrafosIA
{
    public enum GameState { Playing, Victory, GameOver }

    /// <summary>
    /// Administrador del minijuego para la Entrega 2.
    /// Controla la condición de victoria (alcanzar punto de extracción o hackear terminales),
    /// la condición de derrota (ser atrapado por la Thought Police),
    /// y renderiza un HUD cibernético instantáneo en pantalla.
    /// </summary>
    public class MinigameManager : MonoBehaviour
    {
        public static MinigameManager Instance { get; private set; }

        [Header("Referencias")]
        [SerializeField] private CityGraph cityGraph;
        [SerializeField] private ThoughtPoliceController thoughtPolice;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private CityNode extractionNode;

        [Header("Reglas del Minijuego")]
        [Tooltip("Cargas de Firewall disponibles para el jugador.")]
        public int maxFirewallCharges = 3;
        public int currentFirewallCharges;

        [Tooltip("Distancia mínima a la que la Thought Police captura al jugador.")]
        [SerializeField] private float catchDistance = 1.2f;

        [Tooltip("Duración temporal del Firewall en segundos (0 para permanente).")]
        [SerializeField] private float firewallDurationSeconds = 12f;

        [Header("Estado")]
        public GameState currentState = GameState.Playing;
        private string statusMessage = "MISIÓN: Evade a la Thought Police y llega al Nodo de Extracción.";

        // Control de temporizadores para Firewalls temporales
        private class ActiveFirewallInfo
        {
            public CityNode NodeA;
            public CityNode NodeB;
            public float RemainingTime;
        }
        private List<ActiveFirewallInfo> timedFirewalls = new List<ActiveFirewallInfo>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            currentFirewallCharges = maxFirewallCharges;
        }

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

            if (thoughtPolice == null)
            {
#if UNITY_2023_1_OR_NEWER
                thoughtPolice = FindAnyObjectByType<ThoughtPoliceController>();
#else
                thoughtPolice = FindObjectOfType<ThoughtPoliceController>();
#endif
            }

            if (playerTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (currentState != GameState.Playing)
            {
                // Reiniciar juego con tecla R
                if (Input.GetKeyDown(KeyCode.R))
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
                return;
            }

            // 1. Verificar captura del jugador por la Thought Police
            if (thoughtPolice != null && playerTransform != null)
            {
                float distToEnemy = Vector3.Distance(playerTransform.position, thoughtPolice.transform.position);
                if (distToEnemy <= catchDistance)
                {
                    TriggerGameOver("¡CAPTURADO POR LA THOUGHT POLICE! Tu implante cerebral ha sido reiniciado.");
                    return;
                }
            }

            // 2. Verificar condición de victoria (llegada al nodo de extracción)
            if (extractionNode != null && playerTransform != null)
            {
                float distToGoal = Vector3.Distance(playerTransform.position, extractionNode.Position);
                if (distToGoal <= 1.5f)
                {
                    TriggerVictory("¡DESCONEXIÓN EXITOSA! Has alcanzado la zona de extracción segura.");
                    return;
                }
            }

            // 3. Manejo de duración de Firewalls activos
            if (firewallDurationSeconds > 0)
            {
                for (int i = timedFirewalls.Count - 1; i >= 0; i--)
                {
                    timedFirewalls[i].RemainingTime -= Time.deltaTime;
                    if (timedFirewalls[i].RemainingTime <= 0f)
                    {
                        cityGraph.RestoreEdge(timedFirewalls[i].NodeA, timedFirewalls[i].NodeB);
                        timedFirewalls.RemoveAt(i);
                    }
                }
            }
        }

        public bool TryConsumeFirewallCharge(CityNode a, CityNode b)
        {
            if (currentFirewallCharges <= 0)
            {
                statusMessage = "<color=#FF2244>¡SIN CARGAS DE FIREWALL! Recolecta RAM o espera.</color>";
                return false;
            }

            bool broken = cityGraph.BreakEdge(a, b);
            if (broken)
            {
                currentFirewallCharges--;
                statusMessage = $"<color=#00FFAA>¡FIREWALL DESPLEGADO! Arista {a.nodeId} <-> {b.nodeId} cortada. Cargas restantes: {currentFirewallCharges}</color>";

                if (firewallDurationSeconds > 0)
                {
                    timedFirewalls.Add(new ActiveFirewallInfo { NodeA = a, NodeB = b, RemainingTime = firewallDurationSeconds });
                }
                return true;
            }

            return false;
        }

        public void AddFirewallCharge(int amount = 1)
        {
            currentFirewallCharges = Mathf.Min(currentFirewallCharges + amount, maxFirewallCharges);
            statusMessage = $"<color=#00FFFF>+1 Carga de Firewall restaurada ({currentFirewallCharges}/{maxFirewallCharges})</color>";
        }

        public void TriggerVictory(string message)
        {
            currentState = GameState.Victory;
            statusMessage = message;
            Debug.Log($"<color=#00FF99>[VICTORIA]</color> {message}");
        }

        public void TriggerGameOver(string message)
        {
            currentState = GameState.GameOver;
            statusMessage = message;
            Debug.LogWarning($"<color=#FF0055>[GAME OVER]</color> {message}");
        }

        // ==============================================================================
        // HUD CIBERNÉTICO AUTOMÁTICO (No requiere configuración de Canvas en el Editor)
        // ==============================================================================
        private void OnGUI()
        {
            // Estilo cibernético
            GUI.backgroundColor = new Color(0.05f, 0.05f, 0.12f, 0.92f);
            GUI.color = Color.white;

            // Panel superior izquierdo: Estado de la partida
            GUILayout.BeginArea(new Rect(20, 20, 420, 180), GUI.skin.box);
            GUILayout.Label("<size=16><b><color=#00FFFF>DESCONEXIÓN NEÓN // ENTREGA 2: GRAFOS & IA</color></b></size>");
            GUILayout.Space(5);

            // Cargas de Firewall con indicador visual
            string chargesBar = new string('█', currentFirewallCharges) + new string('░', Mathf.Max(0, maxFirewallCharges - currentFirewallCharges));
            GUILayout.Label($"<b>FIREWALL RAM:</b> <color=#00FF99>[{chargesBar}]</color> ({currentFirewallCharges}/{maxFirewallCharges})");

            // Distancia y alerta de proximidad con la Thought Police
            if (thoughtPolice != null && playerTransform != null)
            {
                float dist = Vector3.Distance(playerTransform.position, thoughtPolice.transform.position);
                string distColor = dist < 5f ? "#FF0044" : (dist < 10f ? "#FFAA00" : "#00FF99");
                GUILayout.Label($"<b>PROXIMIDAD THOUGHT POLICE:</b> <color={distColor}>{dist:F1} metros</color>");
            }

            if (extractionNode != null)
            {
                GUILayout.Label($"<b>OBJETIVO DE ESCAPE:</b> {extractionNode.nodeId}");
            }

            GUILayout.Label($"<b>ESTADO:</b> {statusMessage}");
            GUILayout.EndArea();

            // Mensajes de Fin de Partida en el centro
            if (currentState == GameState.Victory)
            {
                GUI.backgroundColor = new Color(0.0f, 0.4f, 0.2f, 0.95f);
                GUI.Box(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 90, 400, 180), "");
                GUILayout.BeginArea(new Rect(Screen.width / 2 - 190, Screen.height / 2 - 80, 380, 160));
                GUILayout.Label("<size=22><b><color=#00FF99>¡DESCONEXIÓN EXITOSA!</color></b></size>");
                GUILayout.Label("Has superado el rastreo de la Thought Police mediante Dijkstra.");
                GUILayout.Space(10);
                GUILayout.Label("Presiona <b>[R]</b> para reiniciar la simulación.");
                GUILayout.EndArea();
            }
            else if (currentState == GameState.GameOver)
            {
                GUI.backgroundColor = new Color(0.5f, 0.05f, 0.1f, 0.95f);
                GUI.Box(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 90, 400, 180), "");
                GUILayout.BeginArea(new Rect(Screen.width / 2 - 190, Screen.height / 2 - 80, 380, 160));
                GUILayout.Label("<size=22><b><color=#FF0044>¡SISTEMA COMPROMETIDO!</color></b></size>");
                GUILayout.Label("La Thought Police ha interceptado tu señal en la red urbana.");
                GUILayout.Space(10);
                GUILayout.Label("Presiona <b>[R]</b> para reintentar la fuga.");
                GUILayout.EndArea();
            }
        }
    }
}
