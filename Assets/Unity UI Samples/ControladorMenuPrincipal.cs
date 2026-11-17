using UnityEngine;
using UnityEngine.SceneManagement;

public class ControladorMenuPrincipal : MonoBehaviour
{
    [Header("Nombres Exactos de tus Escenas")]
    public string escenaIntroHistoria = "Intro";
    public string escenaMultijugador = "EscenaPelea";
    public string escenaMultijugadorLAN = "seleccion lan";
    public string escenaVsBot = "pelea bot";

    void Start()
    {
        Time.timeScale = 1f;
    }

    public void IrAModoHistoria()
    {
        SceneManager.LoadScene(escenaIntroHistoria);
    }

    public void IrAMultijugador()
    {
        SceneManager.LoadScene(escenaMultijugador);
    }

    public void IrAMultijugadorLAN()
    {
        SceneManager.LoadScene(escenaMultijugadorLAN);
    }

    public void IrAVsBot()
    {
        SceneManager.LoadScene(escenaVsBot);
    }

    public void SalirDelJuego()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}