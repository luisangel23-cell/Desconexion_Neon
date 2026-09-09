"""
==============================================================================
PROYECTO: Desconexión Neón (Feria Gamer - Backend)
MODULO: arbolAVL.py
DESCRIPCIÓN:
    Núcleo del Backend en Python para el videojuego "Desconexión Neón".
    Implementa:
      1. Entidad Neurona (Nodo de Árbol AVL)
      2. Árbol AVL Adaptado (Modo Hacker: desbalance sin autorrotación)
      3. Gestor de Juego (Combos, Overclock, Penalizaciones y Auditoría)
      4. Servidor TCP Asíncrono no bloqueante con multiplexación 'select'
         y protocolo Line-Delimited JSON (NDJSON con '\n').
==============================================================================
"""

import json
import logging
import random
import select
import socket
import sys
import uuid
from typing import Any

# Soporte para salida UTF-8 en consolas Windows
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8")
        sys.stderr.reconfigure(encoding="utf-8")
    except (AttributeError, OSError):
        pass

# ==============================================================================
# CONFIGURACIÓN DEL SISTEMA DE AUDITORÍA (LOGS EN TIEMPO REAL)
# ==============================================================================
LOG_FILENAME = "auditoria_neo_red.log"

logger = logging.getLogger("neo_red")
logger.setLevel(logging.INFO)

# Configurar manejador de archivo siempre
file_handler = logging.FileHandler(LOG_FILENAME, encoding="utf-8")
file_handler.setLevel(logging.INFO)
file_handler.setFormatter(logging.Formatter("%(asctime)s | %(levelname)-8s | %(message)s", datefmt="%Y-%m-%d %H:%M:%S"))
logger.addHandler(file_handler)

def activar_logs_consola():
    """Activa salida de logs en consola cuando se ejecuta el servidor directamente."""
    console_handler = logging.StreamHandler(sys.stdout)
    console_handler.setLevel(logging.INFO)
    console_handler.setFormatter(logging.Formatter("[CYBER-LOG] %(asctime)s | %(levelname)s | %(message)s"))
    logger.addHandler(console_handler)


# ==============================================================================
# 1. ENTIDAD: NEURONA (NODO AVL)
# ==============================================================================
class Neurona:
    """
    Representa un nodo / neurona en el implante cerebral del jugador.
    Almacena notificaciones tóxicas procesadas como un Árbol Binario de Búsqueda.
    """
    def __init__(self, toxicidad: int, mensaje: str = "", encriptado: bool | None = None):
        self.id: str = str(uuid.uuid4())[:8]
        self.toxicidad: int = toxicidad
        self.mensaje: str = mensaje
        self.altura: int = 1
        self.estado: str = "ESTABLE"  # ESTABLE, PELIGRO_LL, PELIGRO_RR, PELIGRO_LR, PELIGRO_RL
        
        # 20% de probabilidad de nacer encriptado si no se especifica
        if encriptado is None:
            self.encriptado: bool = random.random() < 0.20
        else:
            self.encriptado = bool(encriptado)

        self.izq: Neurona | None = None
        self.der: Neurona | None = None

    def a_dict(self, recursivo: bool = True) -> dict[str, Any]:
        """Serializa la neurona a un diccionario para transmitir a Unity vía JSON."""
        datos = {
            "id": self.id,
            "toxicidad": self.toxicidad,
            "mensaje": self.mensaje,
            "altura": self.altura,
            "estado": self.estado,
            "encriptado": self.encriptado
        }
        if recursivo:
            datos["izq"] = self.izq.a_dict(True) if self.izq else None
            datos["der"] = self.der.a_dict(True) if self.der else None
        return datos

    def __repr__(self) -> str:
        return f"<Neurona ID={self.id} Tox={self.toxicidad} Alt={self.altura} Estado={self.estado} Enc={self.encriptado}>"


# ==============================================================================
# 2. ESTRUCTURA DE DATOS: ÁRBOL AVL MODO HACKER
# ==============================================================================
class ArbolAVL:
    """
    Árbol AVL adaptado a la mecánica de juego "Modo Hacker".
    - Inserción basada en toxicidad (BST).
    - Detección de desbalance (|factor| > 1): NO realiza autorrotación.
    - Cambia el estado del nodo a PELIGRO_* y genera alerta para Unity.
    - El jugador debe enviar el parche correcto (LL, RR, LR, RL) para rotar el nodo.
    """
    def __init__(self):
        self.raiz: Neurona | None = None

    # --- Métodos auxiliares de Altura y Factor de Balance ---
    def obtener_altura(self, nodo: Neurona | None) -> int:
        return nodo.altura if nodo else 0

    def obtener_balance(self, nodo: Neurona | None) -> int:
        """
        Factor de Balance = Altura(Subárbol Izquierdo) - Altura(Subárbol Derecho).
        Balance > 1  => Desbalance hacia la izquierda.
        Balance < -1 => Desbalance hacia la derecha.
        """
        if not nodo:
            return 0
        return self.obtener_altura(nodo.izq) - self.obtener_altura(nodo.der)

    def actualizar_altura(self, nodo: Neurona) -> None:
        if nodo:
            nodo.altura = 1 + max(self.obtener_altura(nodo.izq), self.obtener_altura(nodo.der))

    # --- Rotaciones AVL Quirúrgicas ---
    def rotacion_derecha(self, z: Neurona) -> Neurona:
        """
        Rotación simple a la derecha (soluciona caso LL).
               z                  y
              / \\                / \
             y   T4   --->      x   z
            / \\                    / \
           x   T3                 T3  T4
        """
        y = z.izq
        if not y:
            return z
        t3 = y.der

        # Realizar rotación
        y.der = z
        z.izq = t3

        # Actualizar alturas
        self.actualizar_altura(z)
        self.actualizar_altura(y)
        return y

    def rotacion_izquierda(self, z: Neurona) -> Neurona:
        """
        Rotación simple a la izquierda (soluciona caso RR).
             z                      y
            / \\                    / \
           T1  y      --->        z   x
              / \\                / \
             T2  x              T1  T2
        """
        y = z.der
        if not y:
            return z
        t2 = y.izq

        # Realizar rotación
        y.izq = z
        z.der = t2

        # Actualizar alturas
        self.actualizar_altura(z)
        self.actualizar_altura(y)
        return y

    def rotacion_izquierda_derecha(self, z: Neurona) -> Neurona:
        """Rotación doble izquierda-derecha (soluciona caso LR)."""
        if z.izq:
            z.izq = self.rotacion_izquierda(z.izq)
        return self.rotacion_derecha(z)

    def rotacion_derecha_izquierda(self, z: Neurona) -> Neurona:
        """Rotación doble derecha-izquierda (soluciona caso RL)."""
        if z.der:
            z.der = self.rotacion_derecha(z.der)
        return self.rotacion_izquierda(z)

    # --- Inserción en Modo Hacker ---
    def insertar(self, toxicidad: int, mensaje: str = "", encriptado: bool | None = None) -> tuple[Neurona, dict[str, Any] | None]:
        """
        Inserta una nueva neurona según su toxicidad.
        Si se genera un desbalance, detecta el caso matemático (LL, RR, LR, RL),
        marca el nodo en alerta y prepara el evento para Unity SIN autorotar.
        
        Retorna: (neurona_creada, alerta_dict o None)
        """
        alerta_generada: dict[str, Any] | None = None

        def _insertar_rec(nodo: Neurona | None) -> tuple[Neurona, Neurona]:
            nonlocal alerta_generada
            if not nodo:
                nueva = Neurona(toxicidad, mensaje, encriptado=encriptado)
                return nueva, nueva

            if toxicidad < nodo.toxicidad:
                nodo.izq, creada = _insertar_rec(nodo.izq)
            else:
                nodo.der, creada = _insertar_rec(nodo.der)

            # Recalcular altura del nodo actual en el retroceso
            self.actualizar_altura(nodo)
            balance = self.obtener_balance(nodo)

            # MODO HACKER: Detectar desbalance (|balance| > 1)
            # Solo generamos alerta para el primer ancestro desbalanceado (el más bajo)
            # y solo si aún estaba en estado ESTABLE.
            if abs(balance) > 1 and alerta_generada is None and nodo.estado == "ESTABLE":
                rotacion_esperada = ""

                if balance > 1:
                    # Izquierda pesada
                    balance_hijo = self.obtener_balance(nodo.izq)
                    if toxicidad < (nodo.izq.toxicidad if nodo.izq else 0) or balance_hijo >= 0:
                        rotacion_esperada = "LL"
                    else:
                        rotacion_esperada = "LR"
                else:
                    # Derecha pesada (balance < -1)
                    balance_hijo = self.obtener_balance(nodo.der)
                    if toxicidad > (nodo.der.toxicidad if nodo.der else 0) or balance_hijo <= 0:
                        rotacion_esperada = "RR"
                    else:
                        rotacion_esperada = "RL"

                nodo.estado = f"PELIGRO_{rotacion_esperada}"
                alerta_generada = {
                    "evento": "alerta_desbalance",
                    "id_neurona": nodo.id,
                    "toxicidad": nodo.toxicidad,
                    "tipo_alerta": nodo.estado,
                    "rotacion_esperada": rotacion_esperada,
                    "encriptado": nodo.encriptado,
                    "tiempo_limite": 10
                }

            return nodo, creada

        self.raiz, nueva_neurona = _insertar_rec(self.raiz)
        return nueva_neurona, alerta_generada

    # --- Aplicación y Validación de Parche (Modo Hacker) ---
    def aplicar_parche(self, id_neurona: str, rotacion_elegida: str) -> tuple[bool, str, Neurona | None]:
        """
        Valida si la rotación enviada por el jugador coincide matemáticamente con el estado del nodo.
        Si es correcta, aplica la rotación AVL correspondiente y actualiza la estructura.
        
        Retorna: (exito, motivo_o_mensaje, nodo_actualizado)
        """
        rotacion_elegida = rotacion_elegida.strip().upper()

        def _parche_rec(nodo: Neurona | None) -> tuple[Neurona | None, bool, str, Neurona | None]:
            if not nodo:
                return None, False, "NEURONA_NO_ENCONTRADA", None

            if nodo.id == id_neurona:
                if not nodo.estado.startswith("PELIGRO_"):
                    return nodo, False, "NEURONA_YA_ESTABLE", nodo

                rotacion_esperada = nodo.estado.replace("PELIGRO_", "")
                if rotacion_elegida != rotacion_esperada:
                    return nodo, False, f"ROTACION_INVALIDA (esperada: {rotacion_esperada}, enviada: {rotacion_elegida})", nodo

                # Rotación correcta: ejecutar la transformación correspondiente
                if rotacion_elegida == "LL":
                    nuevo_nodo = self.rotacion_derecha(nodo)
                elif rotacion_elegida == "RR":
                    nuevo_nodo = self.rotacion_izquierda(nodo)
                elif rotacion_elegida == "LR":
                    nuevo_nodo = self.rotacion_izquierda_derecha(nodo)
                elif rotacion_elegida == "RL":
                    nuevo_nodo = self.rotacion_derecha_izquierda(nodo)
                else:
                    return nodo, False, f"TIPO_ROTACION_DESCONOCIDA_{rotacion_elegida}", nodo

                nuevo_nodo.estado = "ESTABLE"
                self.actualizar_altura(nuevo_nodo)
                return nuevo_nodo, True, "PARCHE_APLICADO_EXITO", nuevo_nodo

            # Búsqueda en subárbol izquierdo
            if nodo.izq:
                nuevo_izq, exito, motivo, modificado = _parche_rec(nodo.izq)
                if exito:
                    nodo.izq = nuevo_izq
                    self.actualizar_altura(nodo)
                    return nodo, True, motivo, modificado
                elif motivo != "NEURONA_NO_ENCONTRADA":
                    return nodo, False, motivo, modificado

            # Búsqueda en subárbol derecho
            if nodo.der:
                nuevo_der, exito, motivo, modificado = _parche_rec(nodo.der)
                if exito:
                    nodo.der = nuevo_der
                    self.actualizar_altura(nodo)
                    return nodo, True, motivo, modificado
                elif motivo != "NEURONA_NO_ENCONTRADA":
                    return nodo, False, motivo, modificado

            return nodo, False, "NEURONA_NO_ENCONTRADA", None

        if not self.raiz:
            return False, "ARBOL_VACIO", None

        nueva_raiz, exito, motivo, nodo_mod = _parche_rec(self.raiz)
        if exito:
            self.raiz = nueva_raiz
            return True, motivo, nodo_mod
        return False, motivo, nodo_mod

    def buscar_por_id(self, id_neurona: str) -> Neurona | None:
        """Búsqueda transversal de una neurona por su UUID corto."""
        def _buscar(nodo: Neurona | None) -> Neurona | None:
            if not nodo:
                return None
            if nodo.id == id_neurona:
                return nodo
            izq = _buscar(nodo.izq)
            if izq:
                return izq
            return _buscar(nodo.der)
        return _buscar(self.raiz)

    def a_dict(self) -> dict[str, Any] | None:
        """Serializa todo el árbol a JSON para enviarlo a Unity."""
        return self.raiz.a_dict(recursivo=True) if self.raiz else None


# ==============================================================================
# 3. GESTOR DE JUEGO (LÓGICA, COMBOS, OVERCLOCK Y AUDITORÍA)
# ==============================================================================
class GestorJuego:
    """
    Controla las reglas de negocio del implante neuronal:
    - Registro de combos consecutivos y modo Overclock (3 aciertos).
    - Aplicación de parches con validación matemática.
    - Manejo de penalizaciones por fallo o tiempo agotado.
    - Auditoría en tiempo real en archivo de log físico.
    """
    def __init__(self):
        self.arbol = ArbolAVL()
        self.combo_actual: int = 0
        self.overclock_activo: bool = False
        self.salud_implante: int = 100
        logger.info("SISTEMA CEREBRAL INICIALIZADO | Implante listo para procesar notificaciones.")

    def insertar_notificacion(self, toxicidad: int, mensaje: str = "", encriptado: bool | None = None) -> tuple[Neurona, dict[str, Any] | None]:
        """Procesa una nueva notificación que entra al implante neuronal."""
        neurona, alerta = self.arbol.insertar(toxicidad, mensaje, encriptado=encriptado)
        
        logger.info(f"INSERT | Neurona {neurona.id} (Tox: {toxicidad}, Enc: {neurona.encriptado}) - '{mensaje}'")

        if alerta:
            logger.warning(
                f"DESBALANCE DETECTADO | Neurona {alerta['id_neurona']} en estado {alerta['tipo_alerta']} "
                f"| Rotacion requerida: {alerta['rotacion_esperada']} | Tiempo limite: {alerta['tiempo_limite']}s"
            )

        return neurona, alerta

    def procesar_parche(self, id_neurona: str, rotacion_elegida: str) -> dict[str, Any]:
        """
        Procesa el intento del jugador desde Unity para solucionar un desbalance.
        Valida, calcula combos y activa Overclock si se llega a 3 aciertos seguidos.
        """
        _exito, motivo, _nodo = self.arbol.aplicar_parche(id_neurona, rotacion_elegida)

        if _exito:
            self.combo_actual += 1
            activar_overclock = False

            if self.combo_actual >= 3:
                self.overclock_activo = True
                activar_overclock = True
                logger.info(f"[OVERCLOCK ACTIVADO] Racha de {self.combo_actual} aciertos. Jugador con ventaja temporal.")
            else:
                logger.info(f"[HACKEO EXITOSO] Neurona {id_neurona} reparada con {rotacion_elegida}. Combo: {self.combo_actual}")

            respuesta = {
                "evento": "resultado_parche",
                "exito": True,
                "id_neurona": id_neurona,
                "combo": self.combo_actual,
                "overclock": self.overclock_activo,
                "mensaje": "Parche neuronal aplicado con éxito. Neurona estabilizada.",
                "arbol": self.arbol.a_dict()
            }
            if activar_overclock:
                respuesta["evento_adicional"] = {
                    "evento": "overclock_activado",
                    "duracion": 15,
                    "mensaje": "¡Modo Overclock Desatado!"
                }
            return respuesta
        else:
            # Fallo del jugador: se penaliza y se reinicia el combo
            self.combo_actual = 0
            self.overclock_activo = False
            self.salud_implante = max(0, self.salud_implante - 15)

            logger.warning(
                f"[FALLO DE SEGURIDAD] Intento '{rotacion_elegida}' en neurona {id_neurona} rechazado ({motivo}). "
                f"Combo reiniciado. Salud implante: {self.salud_implante}%"
            )

            return {
                "evento": "resultado_parche",
                "exito": False,
                "id_neurona": id_neurona,
                "combo": 0,
                "overclock": False,
                "salud_implante": self.salud_implante,
                "motivo": motivo,
                "mensaje": "Intento de parche inválido. Sobrecarga neuronal: -15% salud."
            }

    def procesar_tiempo_agotado(self, id_neurona: str) -> dict[str, Any]:
        """Penalización si el temporizador de una alerta en Unity llega a cero."""
        self.combo_actual = 0
        self.overclock_activo = False
        self.salud_implante = max(0, self.salud_implante - 25)

        logger.warning(
            f"[TIEMPO AGOTADO] Alerta en neurona {id_neurona} no fue resuelta a tiempo. "
            f"Penalización de salud: -25%. Salud restante: {self.salud_implante}%"
        )

        return {
            "evento": "penalizacion_tiempo",
            "id_neurona": id_neurona,
            "combo": 0,
            "salud_implante": self.salud_implante,
            "mensaje": "Colapso neuronal por tiempo límite agotado."
        }

    def reiniciar(self) -> None:
        """Reinicia el estado del juego para una nueva partida."""
        self.arbol = ArbolAVL()
        self.combo_actual = 0
        self.overclock_activo = False
        self.salud_implante = 100
        logger.info("REINICIO DEL SISTEMA | Árbol y parámetros restaurados.")


# ==============================================================================
# 4. CAPA DE RED: SERVIDOR TCP NO BLOQUEANTE (SELECT + LINE-DELIMITED JSON)
# ==============================================================================
class ServidorTCP:
    """
    Servidor TCP no bloqueante con multiplexación 'select'.
    Utiliza Line-Delimited JSON (NDJSON con terminador '\\n') para evitar
    el problema de pegado o fragmentación de paquetes (TCP Framing).
    """
    def __init__(self, host: str = "127.0.0.1", puerto: int = 5000):
        self.host = host
        self.puerto = puerto
        self.gestor = GestorJuego()

        # Socket maestro
        self.servidor = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.servidor.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self.servidor.setblocking(False)
        self.servidor.bind((self.host, self.puerto))
        self.servidor.listen(5)

        self.conexiones: list[socket.socket] = [self.servidor]
        self.buffers_recepcion: dict[socket.socket, str] = {}
        self.activo: bool = False

        logger.info(f"SERVIDOR TCP ACTIVO | Escuchando en {self.host}:{self.puerto} (Modo No Bloqueante)")

    def enviar_json(self, cliente: socket.socket, paquete: dict[str, Any]) -> bool:
        """Serializa un paquete a JSON y lo envía con terminador '\\n'."""
        try:
            mensaje = json.dumps(paquete) + "\n"
            cliente.sendall(mensaje.encode("utf-8"))
            return True
        except OSError as e:
            logger.error(f"Error al enviar datos a cliente: {e}")
            return False

    def emitir_a_todos(self, paquete: dict[str, Any]) -> None:
        """Difunde un paquete JSON a todos los clientes conectados (Unity)."""
        desconectados = []
        for sock in self.conexiones:
            if sock is not self.servidor and not self.enviar_json(sock, paquete):
                desconectados.append(sock)

        for sock in desconectados:
            self._cerrar_cliente(sock)

    def _cerrar_cliente(self, sock: socket.socket) -> None:
        """Limpia y cierra la conexión con un cliente."""
        if sock in self.conexiones:
            self.conexiones.remove(sock)
        if sock in self.buffers_recepcion:
            del self.buffers_recepcion[sock]
        try:
            sock.close()
        except OSError:
            pass
        logger.info("CLIENTE DESCONECTADO | Conexión cerrada y buffer liberado.")

    def procesar_comando(self, paquete: dict[str, Any], cliente: socket.socket) -> None:
        """
        Enruta y procesa los comandos JSON entrantes desde Unity:
        - 'aplicar_parche': Intento de rotación hacker.
        - 'insertar_neurona': Notificación tóxica generada.
        - 'tiempo_agotado': Vencimiento de temporizador de alerta.
        - 'obtener_arbol': Solicitud de la estructura del árbol para dibujarlo en Unity.
        - 'reiniciar': Reiniciar partida.
        """
        accion = paquete.get("accion")

        if accion == "aplicar_parche":
            id_neurona = paquete.get("id_neurona", "")
            rotacion = paquete.get("rotacion_elegida", "")
            resultado = self.gestor.procesar_parche(id_neurona, rotacion)
            
            # Notificar resultado a Unity
            self.enviar_json(cliente, resultado)

            # Si se activó Overclock, enviar evento adicional
            if "evento_adicional" in resultado:
                self.emitir_a_todos(resultado["evento_adicional"])

            # Enviar actualización del árbol completo para refrescar gráficos en Unity
            self.emitir_a_todos({
                "evento": "actualizacion_arbol",
                "arbol": self.gestor.arbol.a_dict()
            })

        elif accion == "insertar_neurona":
            toxicidad = int(paquete.get("toxicidad", 50))
            mensaje = paquete.get("mensaje", "Notificación anónima")
            encriptado = paquete.get("encriptado", None)

            neurona, alerta = self.gestor.insertar_notificacion(toxicidad, mensaje, encriptado=encriptado)

            # Notificar que se agregó una neurona
            self.emitir_a_todos({
                "evento": "neurona_insertada",
                "neurona": neurona.a_dict(recursivo=False),
                "arbol": self.gestor.arbol.a_dict()
            })

            # Si la inserción provocó desbalance, emitir alerta de inmediato
            if alerta:
                self.emitir_a_todos(alerta)

        elif accion == "tiempo_agotado":
            id_neurona = paquete.get("id_neurona", "")
            penalizacion = self.gestor.procesar_tiempo_agotado(id_neurona)
            self.emitir_a_todos(penalizacion)

        elif accion == "obtener_arbol":
            self.enviar_json(cliente, {
                "evento": "actualizacion_arbol",
                "arbol": self.gestor.arbol.a_dict(),
                "combo": self.gestor.combo_actual,
                "salud_implante": self.gestor.salud_implante
            })

        elif accion == "reiniciar":
            self.gestor.reiniciar()
            self.emitir_a_todos({
                "evento": "juego_reiniciado",
                "arbol": None,
                "mensaje": "El cerebro ha sido reiniciado a valores de fábrica."
            })

        else:
            logger.warning(f"COMANDO DESCONOCIDO | Recibido: {accion}")
            self.enviar_json(cliente, {
                "evento": "error",
                "mensaje": f"Comando no reconocido: '{accion}'"
            })

    def tick(self, timeout: float = 0.05) -> None:
        """
        Ejecuta un ciclo de multiplexación de red con 'select'.
        Debe llamarse periódicamente en el bucle principal.
        """
        try:
            listos_para_leer, _, _ = select.select(self.conexiones, [], [], timeout)
        except OSError as e:
            logger.error(f"Error en select.select: {e}")
            return

        for sock in listos_para_leer:
            if sock is self.servidor:
                # Nueva conexión entrante desde Unity
                try:
                    cliente, direccion = sock.accept()
                    cliente.setblocking(False)
                    self.conexiones.append(cliente)
                    self.buffers_recepcion[cliente] = ""
                    logger.info(f"NUEVA CONEXIÓN UNITY | Conectado desde {direccion}")
                    # Enviar bienvenida con estado actual
                    self.enviar_json(cliente, {
                        "evento": "conexion_establecida",
                        "mensaje": "Conectado al Cerebro Backend (Desconexión Neón)",
                        "arbol": self.gestor.arbol.a_dict()
                    })
                except OSError as e:
                    logger.error(f"Error al aceptar cliente: {e}")
            else:
                # Datos recibidos de cliente conectado
                try:
                    datos = sock.recv(2048)
                    if not datos:
                        self._cerrar_cliente(sock)
                        continue

                    # Acumular en buffer y procesar por líneas (\n)
                    self.buffers_recepcion[sock] += datos.decode("utf-8", errors="ignore")

                    while "\n" in self.buffers_recepcion[sock]:
                        linea, resto = self.buffers_recepcion[sock].split("\n", 1)
                        self.buffers_recepcion[sock] = resto
                        linea = linea.strip()
                        if linea:
                            try:
                                paquete = json.loads(linea)
                                self.procesar_comando(paquete, sock)
                            except json.JSONDecodeError as json_err:
                                logger.error(f"Error de parseo JSON: {json_err} | Línea: {linea}")
                                self.enviar_json(sock, {
                                    "evento": "error_json",
                                    "mensaje": "El paquete recibido no es un JSON válido."
                                })
                except (ConnectionResetError, OSError) as e:
                    logger.error(f"Error o desconexión en socket cliente: {e}")
                    self._cerrar_cliente(sock)

    def iniciar(self) -> None:
        """Inicia el bucle principal de ejecución del servidor."""
        self.activo = True
        print("=" * 70)
        print("  DESCONEXIÓN NEÓN - BACKEND EN LÍNEA")
        print(f"  Escuchando conexiones Unity en {self.host}:{self.puerto}")
        print("  Presiona Ctrl+C para detener el servidor.")
        print("=" * 70)

        try:
            while self.activo:
                self.tick(timeout=0.05)
        except KeyboardInterrupt:
            print("\nDeteniendo servidor backend...")
        finally:
            self.detener()

    def detener(self) -> None:
        """Detiene el servidor y cierra todas las conexiones."""
        self.activo = False
        for sock in list(self.conexiones):
            try:
                sock.close()
            except OSError:
                pass
        self.conexiones.clear()
        self.buffers_recepcion.clear()
        logger.info("SERVIDOR DETENIDO | Recursos liberados con éxito.")


# ==============================================================================
# PUNTO DE ENTRADA PRINCIPAL
# ==============================================================================
if __name__ == "__main__":
    activar_logs_consola()
    servidor = ServidorTCP(host="127.0.0.1", puerto=5000)
    servidor.iniciar()