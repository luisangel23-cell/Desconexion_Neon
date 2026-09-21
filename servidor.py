import socket
import json
import time
import threading
from arbolAVL import ArbolAVL

arbol_juego = ArbolAVL()
secuencia_forzada = [50, 40, 30, 20, 10, 60] 

conexion_principal = None 


def quitar_candados(nodo):
    if not nodo:
        return
    nodo.encriptado = False
    quitar_candados(nodo.izq)
    quitar_candados(nodo.der)


def aplicar_rotacion(nodo, id_objetivo, tipo_rot, arbol):
    if not nodo:
        return None
    
    if nodo.id == id_objetivo:
        print(f"[SERVIDOR] Procesando rotación '{tipo_rot}' en nodo {id_objetivo}")
        
        # Validamos que el comando pertenezca a la rotación
        if tipo_rot in ["SIMPLE_DERECHA", "LL", "RR"]:
            if nodo.izq:
                
                hijo_izquierdo = nodo.izq 
                
                
                hijos_sueltos = hijo_izquierdo.der 
                
                
                nueva_raiz = hijo_izquierdo 
                
                
                nueva_raiz.der = nodo 
                
                nodo.izq = hijos_sueltos 
                
               
                nueva_raiz.estado = "ESTABLE"
                nodo.estado = "ESTABLE"
                
                return nueva_raiz 
            else:
                
                print("[SERVIDOR] Estructura ya desplazada. Forzando estabilidad final.")
                nodo.estado = "ESTABLE"
                return nodo
        
        return nodo

   
    nodo.izq = aplicar_rotacion(nodo.izq, id_objetivo, tipo_rot, arbol)
    nodo.der = aplicar_rotacion(nodo.der, id_objetivo, tipo_rot, arbol)
    
    if arbol: 
        arbol.actualizar_altura(nodo)
        
    return nodo

def manejar_cliente(conn, addr, es_gestor_principal):
    global conexion_principal
    
    if es_gestor_principal:
        conexion_principal = conn 
        print(f"[SERVIDOR] Iniciando partida y construyendo árbol para {addr}...")
        for i, tox in enumerate(secuencia_forzada):
            arbol_juego.insertar(tox, f"Nodo_{i+1}")
            if arbol_juego.raiz:
                quitar_candados(arbol_juego.raiz) 
                mensaje = json.dumps(arbol_juego.raiz.a_dict()) + "\n"
                try:
                    conn.sendall(mensaje.encode('utf-8'))
                except:
                    break
            time.sleep(2)
        print("[SERVIDOR] Árbol en estado crítico. Esperando rotaciones...")

    conn.settimeout(None)
    while True:
        try:
            data = conn.recv(1024)
            if not data:
                break 
            
            comando = data.decode('utf-8').strip()
            print(f"[SERVIDOR] Comando recibido: {comando}")
            
            if "RESOLVER_ROTACION" in comando:
                partes = comando.split(":")
                if len(partes) >= 3:
                    id_nodo = partes[1]
                    tipo_rotacion = partes[2]
                    
                    
                    arbol_juego.raiz = aplicar_rotacion(arbol_juego.raiz, id_nodo, tipo_rotacion, arbol_juego)
                    
                    if conexion_principal is not None and arbol_juego.raiz:
                        quitar_candados(arbol_juego.raiz) 
                        nuevo_estado = json.dumps(arbol_juego.raiz.a_dict()) + "\n"
                        try:
                            conexion_principal.sendall(nuevo_estado.encode('utf-8'))
                            print("[SERVIDOR] ¡Árbol actualizado y enviado a Unity con éxito!")
                        except Exception as e:
                            print(f"[SERVIDOR] Error devolviendo el árbol: {e}")
                            
        except (ConnectionResetError, BrokenPipeError):
            break
    
    conn.close()

if __name__ == "__main__":
    host = '127.0.0.1'
    port = 5000
    
    servidor = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    servidor.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1) 
    servidor.bind((host, port))
    servidor.listen(10) 
    
    print("--- MOTOR AVL EN TIEMPO REAL ---")
    print(f"Servidor activo en el puerto {port}. Dale Play en Unity...")
    
    es_primer_cliente = True

    while True:
        try:
            conn, addr = servidor.accept()
            hilo = threading.Thread(target=manejar_cliente, args=(conn, addr, es_primer_cliente))
            hilo.start()
            es_primer_cliente = False 
            
        except KeyboardInterrupt:
            print("\nServidor detenido.")
            break