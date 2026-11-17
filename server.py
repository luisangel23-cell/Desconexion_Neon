"""
=============================================================================
PROYECTO: "Desconexión Neón" - Módulo Multijugador LAN Cyberpunk
ARCHIVO: server.py
ROL: Servidor de Relay y Matchmaking Autoritativo Ligero (TCP)
AUTOR: Senior Game Network Engineer
=============================================================================
DESCRIPCIÓN TÉCNICA:
- Protocolo: TCP con flag TCP_NODELAY activo (Desactiva el algoritmo de Nagle
  para garantizar envío instantáneo sin acumulación de paquetes / buffer lag).
- Conexión: Escucha en 0.0.0.0 (todas las interfaces de red local / Wi-Fi).
- Formato de Paquetes: JSON en texto plano delimitado estrictamente por saltos
  de línea ('\n').
- Concurrencia: Hilos de atención dedicados por jugador (Multi-threaded I/O) con
  bloqueos (thread locks) para acceso seguro al estado global de la sala.
- Resiliencia: Captura de ConnectionResetError / ConnectionAbortedError con
  limpieza de recursos y notificación automática al oponente.
=============================================================================
"""

import json
import logging
import socket
import sys
import threading
import time

# Configurar logging con formato legible y timestamps
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] (%(threadName)s) %(message)s",
    datefmt="%H:%M:%S",
)

HOST = "0.0.0.0"
DEFAULT_PORT = 5555
MAX_PLAYERS = 2


class NeonRelayServer:
    def __init__(self, host: str = HOST, port: int = DEFAULT_PORT):
        self.host = host
        self.port = port
        self.server_socket = None
        self.is_running = False

        # Diccionario para almacenar sockets de jugadores: {player_id: socket}
        self.players = {}
        # Candado para sincronización de acceso concurrente al diccionario de jugadores
        self.lock = threading.Lock()

    def start(self):
        """Inicializa el socket TCP del servidor y entra en el ciclo de aceptación."""
        try:
            self.server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            # SO_REUSEADDR permite reiniciar el servidor sin esperar el timeout TIME_WAIT del SO
            self.server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)

            self.server_socket.bind((self.host, self.port))
            self.server_socket.listen(MAX_PLAYERS)
            self.is_running = True

            logging.info("=====================================================")
            logging.info("   CYBERPUNK RELAY SERVER - 'DESCONEXIÓN NEÓN'       ")
            logging.info(f"   Escuchando en: {self.host}:{self.port} (TCP)      ")
            logging.info("   Esperando conexiones de Jugador 1 y Jugador 2...   ")
            logging.info("=====================================================")

            self._accept_loop()

        except Exception as e:
            logging.error(f"Fallo crítico al iniciar el servidor: {e}")
        finally:
            self.shutdown()

    def _accept_loop(self):
        """Acepta conexiones entrantes y asigna los slots de Jugador 1 o 2."""
        while self.is_running:
            try:
                client_socket, client_address = self.server_socket.accept()

                # CONFIGURACIÓN CRÍTICA DE RED PARA JUEGOS:
                # TCP_NODELAY desactiva el algoritmo de Nagle, eliminando el delay
                # artificial de 20-40ms al enviar paquetes pequeños de movimiento.
                client_socket.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)

                with self.lock:
                    if len(self.players) >= MAX_PLAYERS:
                        logging.warning(
                            f"Conexión rechazada desde {client_address}: Sala llena (Máx {MAX_PLAYERS})."
                        )
                        reject_msg = json.dumps({
                            "type": "error",
                            "message": "Sala llena. Máximo 2 jugadores permitidos."
                        }) + "\n"
                        try:
                            client_socket.sendall(reject_msg.encode("utf-8"))
                            client_socket.close()
                        except OSError:
                            pass
                        continue

                    # Determinar el ID libre (1 o 2)
                    assigned_id = 1 if 1 not in self.players else 2
                    is_host = (assigned_id == 1)
                    self.players[assigned_id] = client_socket

                logging.info(
                    f"-> Jugador {assigned_id} conectado desde {client_address} (Host: {is_host})"
                )

                # Notificar al cliente recién conectado sobre su identidad
                welcome_packet = {
                    "type": "welcome",
                    "playerId": assigned_id,
                    "isHost": is_host,
                    "message": f"Conectado exitosamente como Jugador {assigned_id}"
                }
                self._send_to_client(client_socket, welcome_packet)

                # Si ya están ambos jugadores en la sala, iniciar la partida formalmente
                with self.lock:
                    if len(self.players) == MAX_PLAYERS:
                        logging.info("¡SALA COMPLETA! Transmitiendo evento MATCH_START...")
                        match_start_packet = {
                            "type": "match_start",
                            "timestamp": time.time(),
                            "playerCount": 2
                        }
                        self._broadcast(match_start_packet)

                # Lanzar hilo dedicado para escuchar al cliente
                client_thread = threading.Thread(
                    target=self._handle_client_stream,
                    args=(client_socket, assigned_id, client_address),
                    name=f"Worker-Player-{assigned_id}",
                    daemon=True,
                )
                client_thread.start()

            except OSError:
                # Ocurre cuando el socket del servidor se cierra intencionalmente
                break

    def _handle_client_stream(self, client_socket: socket.socket, player_id: int, address):
        """
        Lee el flujo TCP continuo del cliente, ensambla paquetes delimitados por '\n'
        y retransmite el estado al oponente.
        """
        buffer = ""

        try:
            while self.is_running:
                data = client_socket.recv(4096)
                if not data:
                    # Conexión cerrada limpiamente por el cliente
                    logging.info(f"Jugador {player_id} ({address}) cerró la conexión (EOF).")
                    break

                buffer += data.decode("utf-8", errors="replace")

                # Procesar todos los mensajes completos separados por salto de línea
                while "\n" in buffer:
                    line, buffer = buffer.split("\n", 1)
                    line = line.strip()
                    if not line:
                        continue

                    self._process_player_message(player_id, line)

        except (ConnectionResetError, ConnectionAbortedError) as e:
            logging.warning(f"Desconexión abrupta de Jugador {player_id} ({address}): {e}")
        except Exception as e:
            logging.error(f"Error imprevisto en socket de Jugador {player_id}: {e}")
        finally:
            self._disconnect_player(player_id, client_socket)

    def _process_player_message(self, sender_id: int, raw_json: str):
        """
        Procesa el payload recibido.
        Por arquitectura de relay de baja latencia, retransmite directamente
        el payload hacia el otro jugador.
        """
        try:
            packet = json.loads(raw_json)
        except json.JSONDecodeError:
            logging.warning(f"JSON malformado recibido de Jugador {sender_id}: {raw_json[:50]}...")
            return

        # Asegurar que el paquete identifique fehacientemente al emisor
        packet["senderId"] = sender_id

        # Retransmitir al oponente (si está conectado)
        opponent_id = 2 if sender_id == 1 else 1

        with self.lock:
            opponent_socket = self.players.get(opponent_id)

        if opponent_socket:
            self._send_to_client(opponent_socket, packet)

    def _send_to_client(self, target_socket: socket.socket, packet_data: dict):
        """Serializa y despacha un diccionario a través del socket con delimitador '\n'."""
        try:
            payload = json.dumps(packet_data) + "\n"
            target_socket.sendall(payload.encode("utf-8"))
        except (BrokenPipeError, ConnectionResetError, OSError) as e:
            logging.warning(f"Fallo al enviar datos a cliente: {e}")

    def _broadcast(self, packet_data: dict):
        """Envía un paquete a todos los jugadores actualmente conectados."""
        with self.lock:
            sockets = list(self.players.values())
        for sock in sockets:
            self._send_to_client(sock, packet_data)

    def _disconnect_player(self, player_id: int, client_socket: socket.socket):
        """Limpia el slot del jugador desconectado y notifica al oponente."""
        with self.lock:
            if player_id in self.players:
                del self.players[player_id]

        try:
            client_socket.shutdown(socket.SHUT_RDWR)
        except OSError:
            pass
        finally:
            client_socket.close()

        logging.info(f"Slot de Jugador {player_id} liberado.")

        # Notificar al oponente restante que el rival se desconectó
        disconnect_notice = {
            "type": "player_disconnected",
            "playerId": player_id,
            "message": f"El Jugador {player_id} se ha desconectado de la partida."
        }
        self._broadcast(disconnect_notice)

    def shutdown(self):
        """Cierre ordenado de todos los recursos del servidor."""
        logging.info("Deteniendo Cyberpunk Relay Server...")
        self.is_running = False

        with self.lock:
            for pid, sock in self.players.items():
                try:
                    sock.close()
                except OSError:
                    pass
            self.players.clear()

        if self.server_socket:
            try:
                self.server_socket.close()
            except OSError:
                pass

        logging.info("Servidor detenido completamente.")


if __name__ == "__main__":
    port_to_use = DEFAULT_PORT
    if len(sys.argv) > 1:
        try:
            port_to_use = int(sys.argv[1])
        except ValueError:
            print(f"Puerto inválido. Usando puerto predeterminado: {DEFAULT_PORT}")

    server = NeonRelayServer(port=port_to_use)
    try:
        server.start()
    except KeyboardInterrupt:
        print("\nInterrupción manual detectada por teclado.")
        server.shutdown()

