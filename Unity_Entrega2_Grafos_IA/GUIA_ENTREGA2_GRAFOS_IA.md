# 🌆 Entrega 2: Grafos e Inteligencia Artificial - "Desconexión Neón"

Este módulo contiene la implementación completa y local en C# para Unity de la **Entrega 2: Grafos e Inteligencia Artificial**. 

> **Nota de Diseño**: No utiliza sockets ni dependencias externas; todo se procesa localmente dentro del ciclo de vida de Unity (`MonoBehaviour`), optimizado para ejecutarse de 30 a 60+ FPS.

---

## 📁 Archivos Implementados

| Archivo | Rol en el Sistema |
|---|---|
| [`CityNode.cs`](./CityNode.cs) | Representa intersecciones y esquinas como nodos en el espacio 3D/2D con Gizmos visuales. |
| [`CityEdge.cs`](./CityEdge.cs) | Modelo de datos de arista bidireccional con costo/distancia euclidiana. |
| [`CityGraph.cs`](./CityGraph.cs) | Grafo no dirigido con Lista de Adyacencia, Algoritmo de Dijkstra y método `BreakEdge`. |
| [`ThoughtPoliceController.cs`](./ThoughtPoliceController.cs) | IA de la "Thought Police" con Dijkstra, contra-hackeo de bypass y visualización LineRenderer. |
| [`FirewallTrigger.cs`](./FirewallTrigger.cs) | Componente interactivo para cortar aristas fijas mediante teclado o zonas de colisión. |
| [`PlayerCityController.cs`](./PlayerCityController.cs) | Movimiento fluido WASD del jugador y despliegue dinámico de Firewalls en la calle actual (`[F]`). |
| [`MinigameManager.cs`](./MinigameManager.cs) | Game Loop completo: cargas de Firewall, condiciones de Victoria/Derrota y HUD cibernético. |
| [`CityGridGenerator.cs`](./CityGridGenerator.cs) | Generador en 1 clic de una red urbana 3x3/4x4 con túneles de escape para pruebas inmediatas. |

---

## 🧠 Arquitectura Teórica y Algorítmica

### 1. Grafo No Dirigido con Lista de Adyacencia
En lugar de una matriz de adyacencia de $O(V^2)$ que desperdiciaría memoria en mapas urbanos dispersos, utilizamos una **Lista de Adyacencia** basada en `Dictionary<CityNode, List<CityEdge>>`:
- **Complejidad Espacial:** $O(V + E)$, donde $V$ es el número de esquinas/intersecciones y $E$ el número de calles.
- **Bidireccional:** Para cada calle entre $A$ y $B$, se registran las transiciones $A \rightarrow B$ y $B \rightarrow A$ con el costo correspondiente (distancia euclidiana calculada automáticamente con `Vector3.Distance`).

### 2. Algoritmo de Dijkstra (Ruta Más Corta)
Calcula el camino de menor costo desde la posición actual de la **Thought Police** hasta la posición del jugador:
1. **Inicialización:** Todas las distancias acumuladas se fijan en $\infty$ (`float.MaxValue`), excepto el nodo de partida que inicia en $0$.
2. **Selección del Mínimo:** En cada iteración se toma el nodo no visitado con menor distancia tentativa.
3. **Relajación de Aristas:** Para cada vecino adyacente:
   $$\text{distancia\_tentativa} = \text{distancia}[u] + \text{peso}(u, v)$$
   Si $\text{distancia\_tentativa} < \text{distancia}[v]$, se actualiza la distancia mínima y se registra a $u$ como el predecesor de $v$ en el diccionario `previous`.
4. **Reconstrucción del Camino:** Se retrocede desde el nodo del jugador hacia atrás usando el diccionario `previous` y se invierte la lista, retornando la secuencia exacta `List<CityNode>`.

### 3. Mecánica de Firewall (`BreakEdge`) y Patrón Observer
- Al activar un Firewall sobre la calle que conecta los nodos $A$ y $B$, se invoca:
  ```csharp
  cityGraph.BreakEdge(nodeA, nodeB);
  ```
- Este método remueve las aristas en ambas direcciones de la lista de adyacencia y dispara el evento en C#:
  ```csharp
  public event Action OnGraphTopologyChanged;
  ```
- La `ThoughtPoliceController` está suscrita a este evento. Al ser notificada en tiempo real, interrumpe su trayectoria actual y ejecuta inmediatamente `GetShortestPathDijkstra()` desde su posición actual hacia el jugador, encontrando una calle alternativa sin detener bruscamente la experiencia de juego.

---

## 🛠️ Guía Rápida de Integración en Unity

1. **Copiar la carpeta** `Unity_Entrega2_Grafos_IA` dentro de la carpeta `Assets/Scripts/` de tu proyecto de Unity.
2. **Crear la Red Urbana (Nodos):**
   - Crea un GameObject vacío llamado `--- CITY_GRAPH ---` y agrégale el componente `CityGraph`.
   - Crea GameObjects hijos como esquinas (ej: `Nodo_A`, `Nodo_B`, `Nodo_C`) y agrégales el script `CityNode`.
   - En el Inspector de cada `CityNode`, arrastra los nodos vecinos en la lista `Initial Neighbors`.
3. **Configurar la Thought Police:**
   - En el GameObject de tu enemigo, adjunta `ThoughtPoliceController`.
   - Asigna el `CityGraph` y el `Transform` de tu jugador en el Inspector.
4. **Configurar el Firewall:**
   - En cualquier punto de corte o en un trigger interactivo, adjunta `FirewallTrigger`.
   - Asigna los dos nodos que conectan la calle que el jugador puede bloquear.
   - Presiona `[F]` en Play Mode para ver cómo la policía se reubica automáticamente.
5. **Configuración Rápida en 1 Clic (Recomendado):**
   - Crea un GameObject vacío llamado `RedUrbana`.
   - Adjunta el componente `CityGridGenerator`.
   - Haz clic derecho sobre el componente en el Inspector y selecciona **"Generar Cuadrícula Urbana Neón"**.
   - ¡Listo! Se creará instantáneamente una red 3x3 de 9 esquinas interconectadas con calles y túneles subterráneos.
6. **Iniciar el Minijuego Completo:**
   - Agrega un GameObject con `MinigameManager`.
   - Asigna a tu jugador el script `PlayerCityController` para moverte con WASD y desplegar Firewalls dinámicos presionando `[F]` o `[Espacio]`.
   - En Play Mode, verás el **HUD Neón automático** en pantalla con tus cargas de Firewall, la distancia a la Thought Police en tiempo real y la condición de Victoria al llegar a la extracción o Derrota al ser interceptado.

