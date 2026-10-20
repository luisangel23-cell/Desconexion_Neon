using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DesconexionNeon.GrafosIA
{
    public enum GameState { Playing, Victory, GameOver }

    /// <summary>
    /// Administrador del minijuego para la Entrega 2.
    /// Controla condiciones de victoria y derrota, y maneja el cambio de escena tras ganar.
    /// </summary>
    public class MinigameManager : MonoBehaviour
    {
        public static MinigameManager Instance { get; private set; }

        [Header("Referencias")]
        [SerializeField] private CityGraph cityGraph;
        [SerializeField] private ThoughtPoliceController thoughtPolice;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private CityNode extractionNode;

        [Header("Transición de Escena")]
        [Tooltip("Nombre exacto de la escena a cargar después de ganar el minijuego.")]
        public string nombreEscenaSiguiente = "NombreDeTuEscenaPrincipal";

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

        private List<ActiveFirewallInfo> timedFirewalls = new List<ActiveFirewallInfo>();

        private class ActiveFirewallInfo
        {
            public CityNode NodeA;
            public CityNode NodeB;
            public float RemainingTime;
        }

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

            // NUEVO: Buscar el nodo de extracción automáticamente al iniciar el juego
            if (extractionNode == null)
            {
                // Busca el nodo en la esquina superior derecha de la grilla 3x3
                GameObject nodoMeta = GameObject.Find("Nodo_2_2"); 
                if (nodoMeta != null)
                {
                    extractionNode = nodoMeta.GetComponent<CityNode>();
                    Debug.Log("<color=#00FF99>[MinigameManager]</color> Nodo de extracción asignado automáticamente a Nodo_2_2.");
                }
                else
                {
                    Debug.LogWarning("[MinigameManager] No se encontró el Nodo_2_2. Revisa que se haya generado correctamente.");
                }
            }
        }

        private void Update()
        {
            if (currentState != GameState.Playing)
            {
                // Solo si pierde (GameOver), permite reiniciar con la tecla R
                if (currentState == GameState.GameOver && Input.GetKeyDown(KeyCode.R))
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
                statusMessage = "<color=#FF2244>¡SIN CARGAS DE FIREWALL!</color>";
                return false;
            }

            bool broken = cityGraph.BreakEdge(a, b);
            if (broken)
            {
                currentFirewallCharges--;
                statusMessage = $"<color=#00FFAA>¡FIREWALL DESPLEGADO!</color>";

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
            statusMessage = $"<color=#00FFFF>+1 Carga de Firewall restaurada</color>";
        }

        public void TriggerVictory(string message)
        {
            if (currentState == GameState.Victory) return;

            currentState = GameState.Victory;
            statusMessage = message;
            Debug.Log($"<color=#00FF99>[VICTORIA]</color> {message}");

            // Inicia la cuenta regresiva de 5 segundos para cambiar de escena automáticamente
            StartCoroutine(RutinaCambioDeEscena());
        }

        public void TriggerGameOver(string message)
        {
            currentState = GameState.GameOver;
            statusMessage = message;
            Debug.LogWarning($"<color=#FF0055>[GAME OVER]</color> {message}");
        }

        private IEnumerator RutinaCambioDeEscena()
        {
            yield return new WaitForSeconds(5f);

            if (!string.IsNullOrEmpty(nombreEscenaSiguiente))
            {
                SceneManager.LoadScene(nombreEscenaSiguiente);
            }
            else
            {
                Debug.LogWarning("[MinigameManager] No se especificó el nombre de la escena siguiente en el Inspector.");
            }
        }

        // ==============================================================================
        // HUD CIBERNÉTICO
        // ==============================================================================
        private void OnGUI()
        {
            GUI.backgroundColor = new Color(0.05f, 0.05f, 0.12f, 0.92f);
            GUI.color = Color.white;

            // Muestra HUD mientras se está jugando
            if (currentState == GameState.Playing)
            {
                GUILayout.BeginArea(new Rect(10, 10, 190, 90), GUI.skin.box);
                
                GUILayout.Label("<size=12><b><color=#00FFFF>DESCONEXIÓN NEÓN</color></b></size>");
                
                string chargesBar = new string('█', currentFirewallCharges) + new string('░', Mathf.Max(0, maxFirewallCharges - currentFirewallCharges));
                GUILayout.Label($"<size=11><b>RAM:</b> <color=#00FF99>[{chargesBar}]</color></size>");

                if (thoughtPolice != null && playerTransform != null)
                {
                    float dist = Vector3.Distance(playerTransform.position, thoughtPolice.transform.position);
                    string distColor = dist < 5f ? "#FF0044" : (dist < 10f ? "#FFAA00" : "#00FF99");
                    GUILayout.Label($"<size=11><b>POLICÍA:</b> <color={distColor}>{dist:F1} m</color></size>");
                }

                GUIStyle estiloEstado = new GUIStyle(GUI.skin.label);
                estiloEstado.wordWrap = true; 
                estiloEstado.fontSize = 10;
                
                string estadoCorto = statusMessage.Replace("MISIÓN: Evade a la Thought Police y llega al Nodo de Extracción.", "Misión: Escapar.");
                GUILayout.Label($"<b>INFO:</b> {estadoCorto}", estiloEstado);
                
                GUILayout.EndArea();
            }

            // Mensajes de Fin de Partida (Centro de pantalla)
            if (currentState == GameState.Victory)
            {
                GUI.backgroundColor = new Color(0.0f, 0.4f, 0.2f, 0.95f);
                GUI.Box(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 60, 300, 120), "");
                GUILayout.BeginArea(new Rect(Screen.width / 2 - 140, Screen.height / 2 - 50, 280, 100));
                GUILayout.Label("<size=18><b><color=#00FF99>¡DESCONEXIÓN EXITOSA!</color></b></size>");
                GUILayout.Label("Has escapado.");
                GUILayout.EndArea();
            }
            else if (currentState == GameState.GameOver)
            {
                GUI.backgroundColor = new Color(0.5f, 0.05f, 0.1f, 0.95f);
                GUI.Box(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 75, 300, 150), "");
                GUILayout.BeginArea(new Rect(Screen.width / 2 - 140, Screen.height / 2 - 65, 280, 130));
                GUILayout.Label("<size=18><b><color=#FF0044>¡SISTEMA COMPROMETIDO!</color></b></size>");
                GUILayout.Label("Te han atrapado.");
                GUILayout.Space(10);
                GUILayout.Label("Presiona <b>[R]</b> para reintentar.");
                GUILayout.EndArea();
            }
        }
    }
}