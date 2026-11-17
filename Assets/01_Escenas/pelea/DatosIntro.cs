using UnityEngine;

public class DatosIntro : MonoBehaviour
{
    [Header("Configuración de Intro")]
    public string nombreAnimacionIntro = "pose entrada";
    public AudioClip vozIntro;
    public float duracionIntro = 3.5f;

    [Header("Punto de Enfoque")]
    [Tooltip("Crea un GameObject vacío como hijo enfrente del rostro del personaje y arrástralo aquí")]
    public Transform puntoCamaraRostro; 
}