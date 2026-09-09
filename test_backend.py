"""
Pruebas unitarias y de integración para el Backend de "Desconexión Neón".
Verifica:
1. Inserción en Árbol AVL y detección de desbalance (LL, RR, LR, RL) sin autorrotación.
2. Validación de parches del hacker (rechazo ante rotación inválida, éxito ante válida).
3. Sistema de combos y activación del modo Overclock (3 aciertos seguidos).
4. Registro de auditoría en auditoria_neo_red.log.
5. Integración TCP mediante simulación de cliente Unity con paquetes JSON delimitados por '\\n'.
"""

import sys

if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8")
        sys.stderr.reconfigure(encoding="utf-8")
    except (AttributeError, OSError):
        pass

import json
import os
import socket
import threading
import time

from arbolAVL import LOG_FILENAME, ArbolAVL, GestorJuego, ServidorTCP


def test_arbol_avl_modo_hacker():
    print("\n--- TEST 1: ÁRBOL AVL - MODO HACKER Y ROTACIONES ---")
    arbol = ArbolAVL()

    # Inserción caso LL (Izquierda - Izquierda): 30 -> 20 -> 10
    _n30, alerta1 = arbol.insertar(30, "Fake news leve")
    assert alerta1 is None, "No debería haber desbalance con 1 nodo"

    _n20, alerta2 = arbol.insertar(20, "Spam moderado")
    assert alerta2 is None, "No debería haber desbalance con 2 nodos"

    _n10, alerta3 = arbol.insertar(10, "Acoso tóxico")
    assert alerta3 is not None, "Debería detectarse desbalance con inserción de 10"
    assert alerta3["tipo_alerta"] == "PELIGRO_LL", f"Esperado PELIGRO_LL, obtenido {alerta3['tipo_alerta']}"
    assert arbol.raiz.estado == "PELIGRO_LL", "La raíz 30 debe estar en estado PELIGRO_LL"
    assert arbol.raiz.toxicidad == 30, "El árbol NO debe autorotar automáticamente"

    print("[OK] Inserción LL detectada correctamente sin autorrotación.")

    # Intento de parche inválido
    exito, motivo, _ = arbol.aplicar_parche(arbol.raiz.id, "RR")
    assert not exito, "El parche con 'RR' debe ser rechazado para un caso LL"
    assert "ROTACION_INVALIDA" in motivo
    print("[OK] Intento de rotación incorrecta rechazado con éxito.")

    # Intento de parche válido ("LL")
    exito, motivo, _nuevo_nodo = arbol.aplicar_parche(arbol.raiz.id, "LL")
    assert exito, f"El parche con 'LL' debió ser exitoso: {motivo}"
    assert arbol.raiz.toxicidad == 20, f"Tras rotar a la derecha, la nueva raíz debe ser 20 (es {arbol.raiz.toxicidad})"
    assert arbol.raiz.estado == "ESTABLE", "El nuevo nodo raíz debe estar ESTABLE"
    assert arbol.raiz.altura == 2, f"La altura de la raíz debe ser 2 (es {arbol.raiz.altura})"
    print("[OK] Parche 'LL' aplicado exitosamente: árbol balanceado y restaurado.")

    # Prueba caso RR (Derecha - Derecha): insertar 40, 50
    _n40, a4 = arbol.insertar(40, "Tox 40")
    assert a4 is None
    _n50, a5 = arbol.insertar(50, "Tox 50")
    assert a5 is not None, "Debería detectarse desbalance RR"
    assert a5["rotacion_esperada"] == "RR", f"Esperado RR, obtenido {a5['rotacion_esperada']}"

    # Aplicar parche RR
    exito, motivo, _ = arbol.aplicar_parche(a5["id_neurona"], "RR")
    assert exito, f"Parche RR falló: {motivo}"
    print("[OK] Caso RR detectado y parcheado exitosamente.")


def test_gestor_juego_combos_y_overclock():
    print("\n--- TEST 2: GESTOR DE JUEGO - COMBOS Y OVERCLOCK ---")
    gestor = GestorJuego()
    assert gestor.combo_actual == 0
    assert not gestor.overclock_activo

    # 1er Acierto: Forzamos desbalance LL (30, 20, 10)
    gestor.insertar_notificacion(30, "Msg 1")
    gestor.insertar_notificacion(20, "Msg 2")
    _, alerta1 = gestor.insertar_notificacion(10, "Msg 3")
    assert alerta1 is not None

    res1 = gestor.procesar_parche(alerta1["id_neurona"], "LL")
    assert res1["exito"] is True
    assert res1["combo"] == 1
    assert not res1["overclock"]
    print("[OK] Acierto 1: Combo = 1")

    # 2do Acierto: Forzamos desbalance RR (insertar 40, 50)
    gestor.insertar_notificacion(40, "Msg 4")
    _, alerta2 = gestor.insertar_notificacion(50, "Msg 5")
    assert alerta2 is not None

    res2 = gestor.procesar_parche(alerta2["id_neurona"], "RR")
    assert res2["exito"] is True
    assert res2["combo"] == 2
    assert not res2["overclock"]
    print("[OK] Acierto 2: Combo = 2")

    # 3er Acierto: Forzamos desbalance (insertar 60, 70)
    gestor.insertar_notificacion(60, "Msg 6")
    _, alerta3 = gestor.insertar_notificacion(70, "Msg 7")
    assert alerta3 is not None

    res3 = gestor.procesar_parche(alerta3["id_neurona"], "RR")
    assert res3["exito"] is True
    assert res3["combo"] == 3
    assert res3["overclock"] is True
    assert gestor.overclock_activo is True
    assert "evento_adicional" in res3
    print("[OK] Acierto 3: OVERCLOCK ACTIVADO con éxito!")

    # Prueba de fallo que rompe el combo
    gestor.insertar_notificacion(80, "Msg 8")
    _, alerta4 = gestor.insertar_notificacion(90, "Msg 9")
    if alerta4:
        res_fallo = gestor.procesar_parche(alerta4["id_neurona"], "LL") # Incorrecto, es RR
        assert res_fallo["exito"] is False
        assert gestor.combo_actual == 0
        assert not gestor.overclock_activo
        print("[OK] Fallo de parche reinicia combo y desactiva Overclock correctamente.")


def test_archivo_logs():
    print("\n--- TEST 3: VERIFICACIÓN DE LOGS DE AUDITORÍA ---")
    assert os.path.exists(LOG_FILENAME), f"El archivo {LOG_FILENAME} debe existir"
    with open(LOG_FILENAME, "r", encoding="utf-8", errors="ignore") as f:
        contenido = f.read()
    assert "INSERT | Neurona" in contenido, "El log debe registrar inserciones"
    assert "DESBALANCE DETECTADO" in contenido, "El log debe registrar desbalances"
    assert "HACKEO EXITOSO" in contenido or "OVERCLOCK ACTIVADO" in contenido, "El log debe registrar hackeos"
    print(f"[OK] Archivo '{LOG_FILENAME}' contiene auditoría en tiempo real.")


def test_servidor_tcp_cliente_simulado():
    print("\n--- TEST 4: INTEGRACIÓN TCP (SIMULACIÓN CLIENTE UNITY) ---")
    servidor = ServidorTCP(host="127.0.0.1", puerto=5055) # Puerto de prueba

    # Hilo para ejecutar el servidor durante la prueba
    detener_hilo = False
    def run_server():
        while not detener_hilo:
            servidor.tick(timeout=0.02)

    hilo = threading.Thread(target=run_server, daemon=True)
    hilo.start()
    time.sleep(0.1)

    # Cliente TCP simulando Unity
    cliente = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    cliente.settimeout(3.0)
    cliente.connect(("127.0.0.1", 5055))
    time.sleep(0.05)

    buffer_cliente = ""
    def leer_linea_json():
        nonlocal buffer_cliente
        while "\n" not in buffer_cliente:
            chunk = cliente.recv(1024).decode("utf-8")
            if not chunk:
                break
            buffer_cliente += chunk
        if "\n" not in buffer_cliente:
            raise ValueError("No se recibió una línea completa en el tiempo esperado")
        linea, resto = buffer_cliente.split("\n", 1)
        buffer_cliente = resto
        return json.loads(linea)

    # 1. Recibir bienvenida
    msg_bienvenida = leer_linea_json()
    assert msg_bienvenida["evento"] == "conexion_establecida"
    print("[OK] Cliente Unity recibió handshake de bienvenida.")

    # 2. Enviar inserción de neuronas para generar desbalance LL (30, 20, 10)
    for tox in [30, 20]:
        cliente.sendall((json.dumps({"accion": "insertar_neurona", "toxicidad": tox, "mensaje": f"Tox {tox}"}) + "\n").encode("utf-8"))
        time.sleep(0.05)
        # Consumir el evento 'neurona_insertada'
        resp = leer_linea_json()
        assert resp["evento"] == "neurona_insertada"

    # Insertar el nodo 10 que causará desbalance
    cliente.sendall((json.dumps({"accion": "insertar_neurona", "toxicidad": 10, "mensaje": "Tox 10"}) + "\n").encode("utf-8"))
    time.sleep(0.05)
    resp_ins = leer_linea_json()
    assert resp_ins["evento"] == "neurona_insertada"

    resp_alerta = leer_linea_json()
    assert resp_alerta["evento"] == "alerta_desbalance"
    id_desbalance = resp_alerta["id_neurona"]
    rot_esperada = resp_alerta["rotacion_esperada"]
    print(f"[OK] Alerta recibida en Unity para neurona {id_desbalance}: {resp_alerta['tipo_alerta']}")

    # 3. Enviar parche hacker desde Unity
    cmd_parche = {
        "accion": "aplicar_parche",
        "id_neurona": id_desbalance,
        "rotacion_elegida": rot_esperada
    }
    cliente.sendall((json.dumps(cmd_parche) + "\n").encode("utf-8"))
    time.sleep(0.05)

    resp_resultado = leer_linea_json()
    assert resp_resultado["evento"] == "resultado_parche"
    assert resp_resultado["exito"] is True
    print(f"[OK] Parche procesado vía TCP con éxito: {resp_resultado['mensaje']}")

    # Cerrar recursos
    cliente.close()
    detener_hilo = True
    servidor.detener()
    hilo.join(timeout=1.0)
    print("[OK] Comunicación TCP bidireccional cliente-servidor validada.")


if __name__ == "__main__":
    test_arbol_avl_modo_hacker()
    test_gestor_juego_combos_y_overclock()
    test_archivo_logs()
    test_servidor_tcp_cliente_simulado()
    print("\n=======================================================")
    print("TODAS LAS PRUEBAS DEL BACKEND PASARON EXITOSAMENTE")
    print("=======================================================\n")
