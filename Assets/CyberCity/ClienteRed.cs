using UnityEngine;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

public class ClienteRed : MonoBehaviour
{
    private TcpClient socketClient;
    private StreamReader lectorStream;
    private NetworkStream streamEscritura; // Canal para enviar mensajes
    private bool estaConectado = false;

    // MAGIA: Creamos una instancia global para que los botones la encuentren fácil
    public static ClienteRed instancia; 

    public string ipServidor = "127.0.0.1";
    public int puertoServidor = 5000; 

    void Awake()
    {
        instancia = this; 
    }

    void Start()
    {
        // ELIMINADO: Ya no se conecta automáticamente al iniciar la escena.
        // Esperará a que el NPC active la conexión de forma manual.
    }

    // NUEVO MÉTODO PÚBLICO: Se conectará únicamente cuando elijas la Opción 2 del NPC
    public void IniciarConexionServidor()
    {
        if (estaConectado) return; // Si ya está conectado, no hace nada

        try
        {
            socketClient = new TcpClient(ipServidor, puertoServidor);
            streamEscritura = socketClient.GetStream();
            lectorStream = new StreamReader(streamEscritura, Encoding.UTF8);
            estaConectado = true;
            Debug.Log("[RED] ¡Conectado al servidor de Python exitosamente por orden del jugador!");
        }
        catch (Exception e)
        {
            Debug.LogError("[RED] Error al conectar con el servidor: " + e.Message);
        }
    }

    // Añadimos este alias por si algún otro script llama a ConectarYEscuchar()
    public void ConectarYEscuchar()
    {
        IniciarConexionServidor();
    }

    // Método para desconectar si se requiere
    public void Desconectar()
    {
        if (socketClient != null)
        {
            socketClient.Close();
            estaConectado = false;
            Debug.Log("[RED] Desconectado del servidor.");
        }
    }

    void Update()
    {
        if (!estaConectado || lectorStream == null) return;

        if (socketClient.GetStream().DataAvailable)
        {
            try
            {
                string jsonRecibido = lectorStream.ReadLine();
                if (!string.IsNullOrEmpty(jsonRecibido))
                {
                    Debug.Log("[RED] JSON recibido desde Python: " + jsonRecibido);
                    NeuronaDTO arbolRecibido = JsonConvert.DeserializeObject<NeuronaDTO>(jsonRecibido);
                    
                    if (arbolRecibido != null)
                    {
                        // REDIBUJAMOS EL ÁRBOL CON LOS DATOS NUEVOS
                        GestorArbolVisual gestor = FindObjectOfType<GestorArbolVisual>();
                        if (gestor != null)
                        {
                            gestor.DibujarArbol(arbolRecibido);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[RED] Error leyendo datos: " + e.Message);
            }
        }
    }

    // Los botones usarán esto para enviar órdenes a Python por el mismo túnel
    public void EnviarComando(string mensaje)
    {
        if (!estaConectado || streamEscritura == null) return;

        try
        {
            byte[] datos = Encoding.UTF8.GetBytes(mensaje + "\n");
            streamEscritura.Write(datos, 0, datos.Length);
            streamEscritura.Flush();
            Debug.Log("[RED] Comando enviado a Python: " + mensaje);
        }
        catch (Exception e)
        {
            Debug.LogError("[RED] Error al enviar comando: " + e.Message);
        }
    }

    void OnApplicationQuit()
    {
        Desconectar();
    }
}