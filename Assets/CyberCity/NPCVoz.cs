using UnityEngine;

public class NPCVoz : MonoBehaviour
{
    public Transform jugador; // Arrastra aquí tu First Person Controller
    public float distanciaDeActivacion = 4f;
    
    private AudioSource audioNPC;
    private bool yaHablo = false;

    void Start()
    {
        audioNPC = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (jugador == null || yaHablo) return;

        float distancia = Vector3.Distance(transform.position, jugador.position);

        if (distancia <= distanciaDeActivacion)
        {
            if (audioNPC != null && !audioNPC.isPlaying)
            {
                audioNPC.Play();
                yaHablo = true; // Se reproduce una sola vez al acercarte
            }
        }
    }
}