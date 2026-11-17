"""
==============================================================================
PROYECTO: Desconexión Neón (Feria Gamer - Entrega 2: Grafos e IA)
ARCHIVO: test_grafos_dijkstra.py
DESCRIPCIÓN:
    Script de verificación para validar la lógica algorítmica de:
    1. Grafo No Dirigido con Lista de Adyacencia.
    2. Algoritmo de Dijkstra para la ruta más corta (Thought Police -> Jugador).
    3. Mecánica de Firewall (BreakEdge) y recálculo automático de ruta alternativa.
==============================================================================
"""

import math

class Vector3Sim:
    def __init__(self, x, y, z):
        self.x = x
        self.y = y
        self.z = z

    def distance_to(self, other):
        return math.sqrt((self.x - other.x)**2 + (self.y - other.y)**2 + (self.z - other.z)**2)

    def __repr__(self):
        return f"({self.x}, {self.y}, {self.z})"


class CityNodeSim:
    def __init__(self, node_id, position):
        self.node_id = node_id
        self.position = position

    def __repr__(self):
        return self.node_id


class CityGraphSim:
    def __init__(self):
        # Lista de adyacencia: nodo -> lista de tuplas (vecino, peso)
        self.adj = {}
        self.all_nodes = []

    def register_node(self, node):
        if node not in self.adj:
            self.adj[node] = []
            self.all_nodes.append(node)

    def add_edge(self, a, b, custom_cost=None):
        self.register_node(a)
        self.register_node(b)
        cost = custom_cost if custom_cost is not None else a.position.distance_to(b.position)

        # Evitar duplicados
        if not any(vecino == b for vecino, _ in self.adj[a]):
            self.adj[a].append((b, cost))
        if not any(vecino == a for vecino, _ in self.adj[b]):
            self.adj[b].append((a, cost))

    def break_edge(self, a, b):
        """Mecánica de Firewall: Elimina la arista bidireccional entre A y B."""
        rem_a = False
        rem_b = False
        if a in self.adj:
            orig_len = len(self.adj[a])
            self.adj[a] = [(v, w) for v, w in self.adj[a] if v != b]
            rem_a = len(self.adj[a]) < orig_len

        if b in self.adj:
            orig_len = len(self.adj[b])
            self.adj[b] = [(v, w) for v, w in self.adj[b] if v != a]
            rem_b = len(self.adj[b]) < orig_len

        return rem_a or rem_b

    def dijkstra(self, start_node, target_node):
        """Algoritmo de Dijkstra para encontrar la ruta más corta."""
        if start_node not in self.adj or target_node not in self.adj:
            return []
        if start_node == target_node:
            return [start_node]

        distances = {node: float('inf') for node in self.all_nodes}
        previous = {node: None for node in self.all_nodes}
        unvisited = set(self.all_nodes)

        distances[start_node] = 0.0

        while unvisited:
            # Seleccionar nodo no visitado con menor distancia tentativa
            current = min(unvisited, key=lambda n: distances[n])
            if distances[current] == float('inf') or current == target_node:
                break

            unvisited.remove(current)

            for neighbor, weight in self.adj[current]:
                if neighbor in unvisited:
                    alt = distances[current] + weight
                    if alt < distances[neighbor]:
                        distances[neighbor] = alt
                        previous[neighbor] = current

        if distances[target_node] == float('inf'):
            return []  # Inalcanzable

        path = []
        curr = target_node
        while curr:
            path.append(curr)
            if curr == start_node:
                break
            curr = previous[curr]

        path.reverse()
        return path


def test_city_graph_and_dijkstra():
    print("=================================================================")
    print("TEST: ENTREGA 2 - GRAFOS E INTELIGENCIA ARTIFICIAL (DIJKSTRA)")
    print("=================================================================")

    # Topología urbana simplificada de Neó-Red:
    # A (Thought Police) --- B (Plaza) --- C (Zona Hack) --- D (Jugador)
    #  \                                                     /
    #   \--------------- E (Bypass Subterráneo) ------------/
    node_a = CityNodeSim("A_ThoughtPolice", Vector3Sim(0, 0, 0))
    node_b = CityNodeSim("B_PlazaCentral", Vector3Sim(10, 0, 0))
    node_c = CityNodeSim("C_ZonaHack", Vector3Sim(20, 0, 0))
    node_d = CityNodeSim("D_Jugador", Vector3Sim(30, 0, 0))
    node_e = CityNodeSim("E_Subterraneo", Vector3Sim(15, 0, 20))

    graph = CityGraphSim()
    # Ruta principal: A -> B -> C -> D (Costo total = 10 + 10 + 10 = 30)
    graph.add_edge(node_a, node_b, custom_cost=10.0)
    graph.add_edge(node_b, node_c, custom_cost=10.0)
    graph.add_edge(node_c, node_d, custom_cost=10.0)

    # Ruta secundaria (más larga): A -> E -> D (Costo total = 25 + 25 = 50)
    graph.add_edge(node_a, node_e, custom_cost=25.0)
    graph.add_edge(node_e, node_d, custom_cost=25.0)

    print("\n1. Calculando ruta inicial de 'Thought Police' a 'Jugador'...")
    initial_path = graph.dijkstra(node_a, node_d)
    path_ids = [n.node_id for n in initial_path]
    print(f" -> Ruta inicial óptima: {' -> '.join(path_ids)}")
    assert path_ids == ["A_ThoughtPolice", "B_PlazaCentral", "C_ZonaHack", "D_Jugador"]
    print(" [OK] Dijkstra seleccionó la ruta más corta correctamente.")

    print("\n2. Jugador activa FIREWALL entre 'B_PlazaCentral' y 'C_ZonaHack'...")
    success = graph.break_edge(node_b, node_c)
    assert success, "La arista B-C debió ser cortada."
    print(" -> Arista B <-> C eliminada de la lista de adyacencia.")

    print("\n3. 'Thought Police' recalcula inmediatamente ruta con Dijkstra...")
    rerouted_path = graph.dijkstra(node_a, node_d)
    rerouted_ids = [n.node_id for n in rerouted_path]
    print(f" -> Nueva ruta recalculada: {' -> '.join(rerouted_ids)}")
    assert rerouted_ids == ["A_ThoughtPolice", "E_Subterraneo", "D_Jugador"]
    print(" [OK] Reenrutamiento exitoso por el Bypass Subterráneo.")

    print("\n4. Jugador activa segundo FIREWALL bloqueando 'E_Subterraneo' <-> 'D_Jugador'...")
    graph.break_edge(node_e, node_d)
    blocked_path = graph.dijkstra(node_a, node_d)
    print(f" -> Resultado de Dijkstra con red bloqueada: {blocked_path}")
    assert len(blocked_path) == 0, "No debe haber ruta disponible."
    print(" [OK] Red completamente aislada: Dijkstra retorna lista vacía sin errores.")

    print("\n=================================================================")
    print(" ¡TODOS LOS TESTS DE GRAFO Y DIJKSTRA PASARON SATISFACTORIAMENTE!")
    print("=================================================================\n")

if __name__ == "__main__":
    test_city_graph_and_dijkstra()
