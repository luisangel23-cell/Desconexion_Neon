using UnityEngine;

public class Proyectil : MonoBehaviour
{
    public float velocidad = 12f;
    private float dano;
    private Vector3 direccion;
    private ControladorCombate lanzador;
    private bool yaGolpeo = false;

    public void Inicializar(Vector3 dir, ControladorCombate quienLanza, float cantidadDano)
    {
        direccion = dir;
        lanzador = quienLanza;
        dano = cantidadDano;

        // Destruir la bola automáticamente a los 3 segundos si se sale del mapa
        Destroy(gameObject, 3f);
    }

    void Update()
    {
        if (yaGolpeo) return;

        // 1. Mover la bola hacia adelante
        transform.position += direccion * velocidad * Time.deltaTime;

        // 2. DETECCIÓN INFALIBLE: Creamos un radar invisible (esfera de radio 0.8) en cada frame
        Collider[] cosasGolpeadas = Physics.OverlapSphere(transform.position, 0.8f);
        
        foreach (Collider col in cosasGolpeadas)
        {
            // Buscamos si lo que tocamos es un personaje (buscamos en el objeto y en sus padres)
            ControladorCombate objetivo = col.GetComponentInParent<ControladorCombate>();
            
            // Si tocamos a un personaje y NO es el mismo que disparó la bola...
            if (objetivo != null && objetivo != lanzador)
            {
                objetivo.RecibirGolpe(dano); // Le bajamos la vida
                yaGolpeo = true;
                Destroy(gameObject); // Explotamos la bola
                break;
            }
        }
    }
}