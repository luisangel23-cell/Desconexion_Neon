using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DesconexionNeon.Networking
{
    /// <summary>
    /// Estructura base para el desempaquetado de encabezados JSON.
    /// </summary>
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
    public class MatchStartPacket
    {
        public string type;
        public double timestamp;
        public int playerCount;
    }

    [Serializable]
    public class DisconnectPacket
    {
        public string type;
        public int playerId;
        public string message;
    }

    /// <summary>
    /// Administrador Central de Redes LAN (TCP) para "Desconexión Neón".
    /// 
    /// CARACTERÍSTICAS TÉCNICAS SENIOR:
    /// 1. Thread-Safe: Lectura continua en hilo secundario de I/O sin bloquear el hilo principal.
    /// 2. ConcurrentQueue: Despacha mensajes entrantes a Unity MainThread dentro de Update().
    /// 3. Cero Freezes en Editor: Cierre controlado de sockets y cancelación de hilos en OnApplicationQuit() y OnDestroy().
    /// 4. Gestión de Conexión: Interfaz directa para TMP_InputField / InputField y botones de conexión.
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("--- Configuración de Conexión ---")]
        [Tooltip("Dirección IP predeterminada si el campo de texto está vacío.")]
        [SerializeField] private string defaultServerIP = "127.0.0.1";

        [Tooltip("Puerto TCP del servidor de relay (debe coincidir con server.py).")]
        [SerializeField] private int serverPort = 5555;

        [Header("--- Interfaz de Usuario (UI) ---")]
        [Tooltip("Campo de texto TMP para escribir la IP del servidor.")]
        [SerializeField] private TMP_InputField ipInputField;

        [Tooltip("Botón para iniciar la conexión.")]
        [SerializeField] private Button connectButton;

        [Tooltip("Texto para mostrar estado de la conexión en pantalla.")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Tooltip("Panel UI de conexión (se oculta al conectarse).")]
        [SerializeField] private GameObject connectionPanel;

        [Header("--- Prefabs y Spawns de Jugadores ---")]
        [Tooltip("Prefab del jugador con componentes NetworkPlayer y PlayerCityController.")]
        [SerializeField] private GameObject playerPrefab;

        [Tooltip("Punto de aparición para el Jugador 1 (Host).")]
        [SerializeField] private Transform spawnPointP1;

        [Tooltip("Punto de aparición para el Jugador 2 (Cliente).")]
        [SerializeField] private Transform spawnPointP2;

        [Header("--- Colores Neón Identificativos ---")]
        [SerializeField] private Color p1Color = new Color(0f, 1f, 0.9f); // Neón Cian
        [SerializeField] private Color p2Color = new Color(1f, 0f, 0.5f); // Neón Magenta

        // Estado público de red
        public int LocalPlayerId { get; private set; } = 0;
        public bool IsHost { get; private set; } = false;
        public bool IsConnected => isConnected;

        // Referencias a las instancias locales y remotas
        public NetworkPlayer LocalPlayerInstance { get; private set; }
        public NetworkPlayer RemotePlayerInstance { get; private set; }

        // Infraestructura de Sockets y Concurrencia
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
        }

        private void Start()
        {
            if (ipInputField != null)
            {
                ipInputField.text = defaultServerIP;
            }

            if (connectButton != null)
            {
                connectButton.onClick.AddListener(OnConnectButtonClicked);
            }

            UpdateStatusUI("Desconectado. Ingresa la IP del Host y presiona Conectar.");
        }

        private void Update()
        {
            // PROCESAMIENTO THREAD-SAFE EN EL HILO PRINCIPAL DE UNITY:
            // Desencolar los mensajes recibidos por el hilo secundario y procesarlos en Update().
            int processedThisFrame = 0;
            const int maxPacketsPerFrame = 60; // Protección contra bucles infinitos en picos de paquetes

            while (incomingMessages.TryDequeue(out string rawJson) && processedThisFrame < maxPacketsPerFrame)
            {
                processedThisFrame++;
                ProcessPacketOnMainThread(rawJson);
            }
        }

        #region --- Gestión de Conexión ---

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

            // Iniciar conexión en hilo secundario para evitar congelamiento de la UI
            ThreadPool.QueueUserWorkItem(_ => ConnectAsync(targetIP, serverPort));
        }

        private void ConnectAsync(string ip, int port)
        {
            try
            {
                tcpClient = new TcpClient();
                // Desactivar Nagle para latencia ultra-baja en LAN
                tcpClient.NoDelay = true;
                tcpClient.ReceiveTimeout = 0;
                tcpClient.SendTimeout = 3000;

                tcpClient.Connect(ip, port);

                networkStream = tcpClient.GetStream();
                streamReader = new StreamReader(networkStream, Encoding.UTF8);
                streamWriter = new StreamWriter(networkStream, Encoding.UTF8) { AutoFlush = true };

                isConnected = true;

                // Iniciar hilo de lectura continua
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

                    // Encolar mensaje en la cola concurrente para consumo en Update()
                    incomingMessages.Enqueue(line);
                }
            }
            catch (IOException)
            {
                // Desconexión esperada al cerrar el socket
            }
            catch (SocketException ex)
            {
                Debug.LogWarning($"[NetworkManager] Socket cerrado: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Excepción en lectura de red: {ex}");
            }
            finally
            {
                if (isConnected)
                {
                    incomingMessages.Enqueue(JsonUtility.ToJson(new DisconnectPacket { type = "player_disconnected", playerId = 0, message = "Conexión perdida con el servidor." }));
                }
                isConnected = false;
            }
        }

        #endregion

        #region --- Procesamiento en MainThread ---

        private void ProcessPacketOnMainThread(string rawJson)
        {
            try
            {
                NetworkPacketHeader header = JsonUtility.FromJson<NetworkPacketHeader>(rawJson);
                if (header == null || string.IsNullOrEmpty(header.type)) return;

                switch (header.type)
                {
                    case "welcome":
                        HandleWelcomePacket(rawJson);
                        break;

                    case "match_start":
                        HandleMatchStartPacket(rawJson);
                        break;

                    case "sync":
                        HandleSyncPacket(rawJson);
                        break;

                    case "action":
                        HandleActionPacket(rawJson);
                        break;

                    case "player_disconnected":
                        HandlePlayerDisconnected(rawJson);
                        break;

                    case "internal_error":
                        UpdateStatusUI("Fallo al conectar. Verifica la IP y el Firewall del Host.");
                        if (connectButton != null) connectButton.interactable = true;
                        break;

                    default:
                        Debug.Log($"[NetworkManager] Paquete no manejado: {header.type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Error procesando JSON: {rawJson}\nEx: {ex.Message}");
            }
        }

        private void HandleWelcomePacket(string json)
        {
            WelcomePacket packet = JsonUtility.FromJson<WelcomePacket>(json);
            LocalPlayerId = packet.playerId;
            IsHost = packet.isHost;

            UpdateStatusUI($"Conectado como Jugador {LocalPlayerId} ({(IsHost ? "HOST" : "CLIENTE")}). Esperando rival...");

            // Instanciar el jugador local en su spawn correspondiente
            SpawnLocalPlayer();

            if (connectionPanel != null)
            {
                connectionPanel.SetActive(false);
            }
        }

        private void HandleMatchStartPacket(string json)
        {
            UpdateStatusUI("¡RIVAL CONECTADO! Partida Neón en curso.");

            // Si aún no se ha instanciado al rival remoto, instanciarlo ahora
            if (RemotePlayerInstance == null)
            {
                SpawnRemotePlayer();
            }
        }

        private void HandleSyncPacket(string json)
        {
            PlayerSyncPacket syncData = JsonUtility.FromJson<PlayerSyncPacket>(json);

            // Si el paquete pertenece al rival remoto, aplicar su estado
            if (RemotePlayerInstance == null)
            {
                SpawnRemotePlayer();
            }

            if (RemotePlayerInstance != null)
            {
                RemotePlayerInstance.ApplySyncData(syncData);
            }
        }

        private void HandleActionPacket(string json)
        {
            PlayerActionPacket actionData = JsonUtility.FromJson<PlayerActionPacket>(json);

            if (RemotePlayerInstance != null)
            {
                RemotePlayerInstance.ExecuteRemoteAction(actionData.actionName);
            }
        }

        private void HandlePlayerDisconnected(string json)
        {
            DisconnectPacket packet = JsonUtility.FromJson<DisconnectPacket>(json);
            UpdateStatusUI($"Aviso: {packet.message}");

            if (RemotePlayerInstance != null)
            {
                Destroy(RemotePlayerInstance.gameObject);
                RemotePlayerInstance = null;
            }

            if (packet.playerId == 0)
            {
                // Desconexión general de este cliente
                if (connectionPanel != null) connectionPanel.SetActive(true);
                if (connectButton != null) connectButton.interactable = true;
            }
        }

        #endregion

        #region --- Spawning de Jugadores ---

        private void SpawnLocalPlayer()
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[NetworkManager] 'playerPrefab' no está asignado en el Inspector.");
                return;
            }

            Transform spawn = (LocalPlayerId == 1) ? spawnPointP1 : spawnPointP2;
            Vector3 spawnPos = spawn != null ? spawn.position : Vector3.zero;
            Quaternion spawnRot = spawn != null ? spawn.rotation : Quaternion.identity;

            GameObject playerObj = Instantiate(playerPrefab, spawnPos, spawnRot);
            playerObj.name = $"Player_{LocalPlayerId}_LOCAL";

            LocalPlayerInstance = playerObj.GetComponent<NetworkPlayer>();
            if (LocalPlayerInstance != null)
            {
                LocalPlayerInstance.Initialize(LocalPlayerId, true, (LocalPlayerId == 1) ? p1Color : p2Color);
            }
        }

        private void SpawnRemotePlayer()
        {
            if (playerPrefab == null) return;

            int remoteId = (LocalPlayerId == 1) ? 2 : 1;
            Transform spawn = (remoteId == 1) ? spawnPointP1 : spawnPointP2;
            Vector3 spawnPos = spawn != null ? spawn.position : new Vector3(2f, 0f, 0f);
            Quaternion spawnRot = spawn != null ? spawn.rotation : Quaternion.identity;

            GameObject playerObj = Instantiate(playerPrefab, spawnPos, spawnRot);
            playerObj.name = $"Player_{remoteId}_REMOTE";

            RemotePlayerInstance = playerObj.GetComponent<NetworkPlayer>();
            if (RemotePlayerInstance != null)
            {
                RemotePlayerInstance.Initialize(remoteId, false, (remoteId == 1) ? p1Color : p2Color);
            }
        }

        #endregion

        #region --- Envío de Mensajes (Despacho Seguro) ---

        /// <summary>
        /// Despacha un objeto serializable convertido a JSON delimitado con '\n'.
        /// Thread-safe mediante lock del StreamWriter.
        /// </summary>
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

        #endregion

        #region --- Limpieza y Apagado Seguro ---

        private void UpdateStatusUI(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"<color=cyan>[NetworkManager]</color> {message}");
        }

        private void Disconnect()
        {
            isConnected = false;

            try
            {
                streamReader?.Close();
                streamWriter?.Close();
                networkStream?.Close();
                tcpClient?.Close();
            }
            catch (Exception)
            {
                // Ignorar errores de cierre
            }

            if (receiveThread != null && receiveThread.IsAlive)
            {
                // Dar 200ms para terminar limpiamente antes de continuar
                receiveThread.Join(200);
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }

        #endregion
    }
}

