using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DesconexionNeon.Networking
{
    [Serializable]
    public class NetworkPacketHeader
    {
        public string type;
        public int senderId;
    }

    [Serializable]
    public class WelcomePacket
    {
        public string type;
        public int playerId;
        public bool isHost;
        public string message;
    }

    [Serializable]
    public class CharSelectPacket
    {
        public string type = "char_select";
        public int playerId;
        public string characterName;
    }

    [Serializable]
    public class DisconnectPacket
    {
        public string type;
        public int playerId;
        public string message;
    }

    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("--- Configuración de Conexión ---")]
        [SerializeField] private string defaultServerIP = "127.0.0.1";
        [SerializeField] private int serverPort = 5555;

        [Header("--- Interfaz de Usuario (UI) ---")]
        [SerializeField] private TMP_InputField ipInputField;
        [SerializeField] private Button connectButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject connectionPanel;

        public int LocalPlayerId { get; private set; } = 0;
        public bool IsHost { get; private set; } = false;
        public bool IsConnected => isConnected;
        public bool AmbosJugadoresConectados { get; private set; } = false;
        public string PersonajeRivalElegido { get; private set; } = "";

        public NetworkPlayer LocalPlayerInstance { get; private set; }
        public NetworkPlayer RemotePlayerInstance { get; private set; }

        private TcpClient tcpClient;
        private NetworkStream networkStream;
        private StreamReader streamReader;
        private StreamWriter streamWriter;

        private Thread receiveThread;
        private volatile bool isConnected = false;
        private readonly ConcurrentQueue<string> incomingMessages = new ConcurrentQueue<string>();
        private readonly object sendLock = new object();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            if (ipInputField != null) ipInputField.text = defaultServerIP;
            if (connectButton != null) connectButton.onClick.AddListener(OnConnectButtonClicked);
            UpdateStatusUI("Desconectado. Ingresa la IP del Host y presiona Conectar.");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Si volvemos al menú principal, destruir este NetworkManager para limpiar la sesión LAN
            if (scene.name == "intro 2")
            {
                Disconnect();
                Destroy(gameObject);
                return;
            }

            // Cuando cargue EscenaPelea tras la selección LAN, enlazar los peleadores generados
            if (isConnected && scene.name == "EscenaPelea")
            {
                PersonajeRivalElegido = "";
                AsignarPeleadoresDeLaEscena();
                ControladorCombate.IniciarCombateLAN();
            }
        }

        private void Update()
        {
            int processedThisFrame = 0;
            const int maxPacketsPerFrame = 60;

            while (incomingMessages.TryDequeue(out string rawJson) && processedThisFrame < maxPacketsPerFrame)
            {
                processedThisFrame++;
                ProcessPacketOnMainThread(rawJson);
            }
        }

        public void OnConnectButtonClicked()
        {
            if (isConnected) return;

            string targetIP = defaultServerIP;
            if (ipInputField != null && !string.IsNullOrWhiteSpace(ipInputField.text))
            {
                targetIP = ipInputField.text.Trim();
            }

            if (connectButton != null) connectButton.interactable = false;
            UpdateStatusUI($"Conectando a {targetIP}:{serverPort}...");

            ThreadPool.QueueUserWorkItem(_ => ConnectAsync(targetIP, serverPort));
        }

        private void ConnectAsync(string ip, int port)
        {
            try
            {
                tcpClient = new TcpClient();
                tcpClient.NoDelay = true;
                tcpClient.ReceiveTimeout = 0;
                tcpClient.SendTimeout = 3000;

                tcpClient.Connect(ip, port);

                networkStream = tcpClient.GetStream();
                streamReader = new StreamReader(networkStream, Encoding.UTF8);
                streamWriter = new StreamWriter(networkStream, Encoding.UTF8) { AutoFlush = true };

                isConnected = true;

                receiveThread = new Thread(ReceiveWorkerLoop)
                {
                    IsBackground = true,
                    Name = "Network-ReceiveWorker"
                };
                receiveThread.Start();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Error al conectar con {ip}:{port} -> {ex.Message}");
                incomingMessages.Enqueue(JsonUtility.ToJson(new NetworkPacketHeader { type = "internal_error" }));
            }
        }

        private void ReceiveWorkerLoop()
        {
            try
            {
                while (isConnected && streamReader != null && !streamReader.EndOfStream)
                {
                    string line = streamReader.ReadLine();
                    if (string.IsNullOrEmpty(line)) continue;
                    incomingMessages.Enqueue(line);
                }
            }
            catch (IOException) { }
            catch (SocketException ex) { Debug.LogWarning($"[NetworkManager] Socket cerrado: {ex.Message}"); }
            catch (Exception ex) { Debug.LogError($"[NetworkManager] Excepción en lectura de red: {ex}"); }
            finally
            {
                if (isConnected)
                {
                    incomingMessages.Enqueue(JsonUtility.ToJson(new DisconnectPacket { type = "player_disconnected", playerId = 0, message = "Conexión perdida con el servidor." }));
                }
                isConnected = false;
            }
        }

        private void ProcessPacketOnMainThread(string rawJson)
        {
            try
            {
                NetworkPacketHeader header = JsonUtility.FromJson<NetworkPacketHeader>(rawJson);
                if (header == null || string.IsNullOrEmpty(header.type)) return;

                switch (header.type)
                {
                    case "welcome":
                        WelcomePacket welcome = JsonUtility.FromJson<WelcomePacket>(rawJson);
                        LocalPlayerId = welcome.playerId;
                        IsHost = welcome.isHost;
                        UpdateStatusUI($"Conectado como Jugador {LocalPlayerId}. Esperando rival...");

                        if (LocalPlayerId == 2)
                        {
                            AmbosJugadoresConectados = true;
                            if (connectionPanel != null) connectionPanel.SetActive(false);
                        }
                        break;

                    case "match_start":
                        AmbosJugadoresConectados = true;
                        UpdateStatusUI("¡RIVAL CONECTADO! Elige tu personaje.");
                        if (connectionPanel != null) connectionPanel.SetActive(false);
                        break;

                    case "char_select":
                        CharSelectPacket charData = JsonUtility.FromJson<CharSelectPacket>(rawJson);
                        PersonajeRivalElegido = charData.characterName;
                        break;

                    case "sync":
                        PlayerSyncPacket syncData = JsonUtility.FromJson<PlayerSyncPacket>(rawJson);
                        if (RemotePlayerInstance != null) RemotePlayerInstance.ApplySyncData(syncData);
                        break;

                    case "damage":
                        PlayerDamagePacket dmgData = JsonUtility.FromJson<PlayerDamagePacket>(rawJson);
                        if (LocalPlayerInstance != null && dmgData.targetPlayerId == LocalPlayerId)
                        {
                            ControladorCombate cc = LocalPlayerInstance.GetComponent<ControladorCombate>();
                            if (cc != null) cc.RecibirGolpeDesdeRed(dmgData.damageAmount);
                        }
                        break;

                    case "action":
                        PlayerActionPacket actionData = JsonUtility.FromJson<PlayerActionPacket>(rawJson);
                        if (RemotePlayerInstance != null) RemotePlayerInstance.ExecuteRemoteAction(actionData.actionName);
                        break;

                    case "player_disconnected":
                        DisconnectPacket disc = JsonUtility.FromJson<DisconnectPacket>(rawJson);
                        AmbosJugadoresConectados = false;
                        UpdateStatusUI($"Aviso: {disc.message}");
                        if (connectionPanel != null) connectionPanel.SetActive(true);
                        if (connectButton != null) connectButton.interactable = true;
                        break;

                    case "internal_error":
                        UpdateStatusUI("Fallo al conectar. Verifica la IP y el Firewall del Host.");
                        if (connectButton != null) connectButton.interactable = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Error procesando JSON: {rawJson}\nEx: {ex.Message}");
            }
        }

        public void EnviarSeleccionPersonaje(string nombrePersonaje)
        {
            SendPacket(new CharSelectPacket
            {
                playerId = LocalPlayerId,
                characterName = nombrePersonaje
            });
        }

        public void AsignarPeleadoresDeLaEscena()
        {
            ControladorCombate[] peleadores = FindObjectsOfType<ControladorCombate>();
            foreach (var p in peleadores)
            {
                NetworkPlayer netPlayer = p.GetComponent<NetworkPlayer>();
                if (netPlayer == null) netPlayer = p.gameObject.AddComponent<NetworkPlayer>();

                bool esLocal = (p.idMando == LocalPlayerId);
                netPlayer.Initialize(p.idMando, esLocal);

                if (esLocal) LocalPlayerInstance = netPlayer;
                else RemotePlayerInstance = netPlayer;
            }
        }

        public void SendPacket<T>(T packet)
        {
            if (!isConnected || streamWriter == null) return;

            try
            {
                string json = JsonUtility.ToJson(packet) + "\n";
                lock (sendLock)
                {
                    streamWriter.Write(json);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkManager] Fallo despachando paquete: {ex.Message}");
            }
        }

        private void UpdateStatusUI(string message)
        {
            if (statusText != null) statusText.text = message;
            Debug.Log($"<color=cyan>[NetworkManager]</color> {message}");
        }

        private void Disconnect()
        {
            isConnected = false;
            AmbosJugadoresConectados = false;
            ControladorCombate.esperandoRedLAN = false;

            try
            {
                streamReader?.Close();
                streamWriter?.Close();
                networkStream?.Close();
                tcpClient?.Close();
            }
            catch (Exception) { }

            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Join(200);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Disconnect();
        }

        private void OnApplicationQuit() { Disconnect(); }
    }
}