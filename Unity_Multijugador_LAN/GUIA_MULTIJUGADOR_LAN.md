# 🌐 Guía de Despliegue Multijugador LAN - "Desconexión Neón"

Esta guía detalla la configuración paso a paso para conectar **dos computadores distintos** en la misma red local (Wi-Fi o cable Ethernet) utilizando el servidor de relay en Python y los scripts de red en Unity C#.

---

## 📋 Arquitectura de Conexión

```
   [ PC 1 (Host) ]                             [ PC 2 (Cliente) ]
+-------------------+                       +-------------------+
|  Unity (P1 Local) |                       |  Unity (P2 Local) |
+---------+---------+                       +---------+---------+
          |                                           |
          | TCP 5555 (127.0.0.1)                      | TCP 5555 (192.168.x.x)
          v                                           v
+---------------------------------------------------------------+
|               Servidor Relay Python (server.py)               |
|                      (Corriendo en PC 1)                      |
+---------------------------------------------------------------+
```

---

## 🚀 Guía de 4 Pasos para la Configuración

### Paso 1: Configurar la Escena y la UI en Unity
1. En tu escena de Unity, crea un GameObject vacío y nómbralo `NetworkManager`.
2. Adjúntale el script `NetworkManager.cs`.
3. Crea un Canvas con una interfaz sencilla:
   - Un **TMP_InputField** para ingresar la dirección IP.
   - Un **Button** de TextMeshPro con el texto *"Conectar"*.
   - Un **TextMeshProUGUI** para el texto de estado.
   - Un GameObject contenedor vacío (ej: `ConnectionPanel`) que agrupe estos elementos.
4. En el Inspector de `NetworkManager`, arrastra las referencias correspondientes:
   - `Ip Input Field`: El TMP_InputField creado.
   - `Connect Button`: El botón de conexión.
   - `Status Text`: El texto de estado.
   - `Connection Panel`: El panel contenedor.

---

### Paso 2: Configurar los Spawns y el Prefab del Jugador
1. **Crear Puntos de Aparición:**
   - Crea dos GameObjects vacíos en la escena: `SpawnPoint_P1` y `SpawnPoint_P2`.
   - Colócalos en posiciones separadas (ej. `(0, 0, 0)` y `(4, 0, 0)`).
   - Arrástralos a los campos `Spawn Point P1` y `Spawn Point P2` del `NetworkManager`.
2. **Configurar el Prefab del Jugador:**
   - Toma el GameObject de tu jugador (el que tiene `PlayerCityController` o `PlayerFighterCombat`).
   - Agrégale el script `NetworkPlayer.cs`.
   - Arrastra el modelo/cápsula a la carpeta `Assets/Prefabs/` para convertirlo en Prefab.
   - Asigna este Prefab al campo `Player Prefab` del `NetworkManager`.

---

### Paso 3: Ejecutar el Servidor y Obtener la IP Local (PC 1)
El servidor debe correr en la computadora que actuará como anfitrión (PC 1):

1. **Obtener la IP Local (IPv4) de PC 1:**
   - Abre la terminal (PowerShell o CMD) en PC 1 y ejecuta:
     ```powershell
     ipconfig
     ```
   - Busca el adaptador activo (Wi-Fi o Ethernet) y anota la **Dirección IPv4** (ejemplo: `192.168.1.15`).

2. **Iniciar el Servidor en PC 1:**
   - En la terminal de la raíz del proyecto, ejecuta:
     ```powershell
     python server.py
     ```
   - Verás el mensaje:
     ```
     [INFO] Escuchando en: 0.0.0.0:5555 (TCP)
     [INFO] Esperando conexiones de Jugador 1 y Jugador 2...
     ```

---

### Paso 4: Abrir el Puerto en el Firewall de Windows (PC 1)
Para que PC 2 pueda comunicarse con PC 1 por la red local sin que Windows bloquee el paquete, ejecuta el siguiente comando en PowerShell con permisos de Administrador en **PC 1**:

```powershell
New-NetFirewallRule -DisplayName "DesconexionNeon_LAN" -Direction Inbound -LocalPort 5555 -Protocol TCP -Action Allow
```

*(Alternativamente, al ejecutar `python server.py`, si Windows muestra una ventana emergente de alerta del Firewall, asegúrate de marcar la casilla **"Redes privadas (como la red doméstica o del trabajo)"** y hacer clic en **"Permitir acceso"**).*

---

## 🎮 Cómo Probar la Conexión

1. **En PC 1 (Host):**
   - Entra a Play Mode en Unity.
   - Deja la IP como `127.0.0.1` (o su IP local) y haz clic en **"Conectar"**.
   - El servidor mostrará: `Jugador 1 conectado (Host: True)`.
   - El jugador aparecerá en `SpawnPoint_P1` con color neón cian.

2. **En PC 2 (Cliente):**
   - Abre el juego en Unity o compila un ejecutable `.exe`.
   - Escribe en el campo de texto la IP de PC 1 (ej. `192.168.1.15`).
   - Haz clic en **"Conectar"**.
   - El servidor mostrará: `¡SALA COMPLETA! Transmitiendo evento MATCH_START...`.
   - Ambos computadores verán al oponente aparecer con color neón magenta y su movimiento se sincronizará fluidamente a 30 FPS mediante interpolación.

