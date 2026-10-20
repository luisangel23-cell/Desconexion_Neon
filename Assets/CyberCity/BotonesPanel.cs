using UnityEngine;

public class BotonesPanel : MonoBehaviour
{
    // Esta es la función que va a ejecutar tu botón
    public void ClickRotacionSimpleIzquierda()
    {
        Debug.Log("[BOTÓN] Clic detectado en Rotación Simple Izquierda");
        ControladorNodoUI.IntentarRotacion("SIMPLE_IZQUIERDA");
    }
}