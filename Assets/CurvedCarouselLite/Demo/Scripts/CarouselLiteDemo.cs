using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using DesconexionNeon.Networking;

namespace CodeStartAI.CarouselMenu.Lite.Demo
{
    [System.Serializable]
    public class PersonajeAudioData
    {
        public string nombrePersonaje;
        public AudioClip sonidoSeleccionUnico;
    }

    [AddComponentMenu("CodeStart-AI/Demo/Carousel Lite Demo")]
    public class CarouselLiteDemo : MonoBehaviour
    {
        [SerializeField] CurvedCarouselLite carousel = null;
        [SerializeField] TMP_Text centerTitle = null;
        [SerializeField] TMP_Text centerSubtitle = null;
        [SerializeField] TMP_Text statusText = null;

        [Header("Sonido General de Movimiento")]
        public AudioSource audioSourceMenu;
        public AudioClip clipSonidoCambio;

        [Header("Sonidos Únicos por Personaje al Seleccionar")]
        public PersonajeAudioData[] sonidosPorPersonaje;

        [Header("Configuración de Escena de Pelea")]
        public string nombreEscenaPelea = "EscenaPelea";

        // Modo Local (2 turnos en el mismo PC)
        private int faseSeleccion = 1;
        private string personajeJ1 = "";

        // Modo LAN (1 personaje por PC)
        private bool yaElegiEnLAN = false;
        private string personajeLocalLAN = "";
        private string personajeRivalLAN = "";
        private bool cargandoEscenaLAN = false;

        private bool EsModoLAN => NetworkManager.Instance != null;

        void OnEnable()
        {
            if (carousel == null) return;
            carousel.OnCenterChanged.AddListener(ShowCenter);
            carousel.OnItemSelected.AddListener(ShowSelected);
        }

        void OnDisable()
        {
            if (carousel == null) return;
            carousel.OnCenterChanged.RemoveListener(ShowCenter);
            carousel.OnItemSelected.RemoveListener(ShowSelected);
        }

        void Update()
        {
            if (!EsModoLAN || cargandoEscenaLAN) return;

            // Revisar si el rival ya envió su personaje por red
            if (!string.IsNullOrEmpty(NetworkManager.Instance.PersonajeRivalElegido) && string.IsNullOrEmpty(personajeRivalLAN))
            {
                personajeRivalLAN = NetworkManager.Instance.PersonajeRivalElegido;
                if (statusText != null && !yaElegiEnLAN)
                {
                    statusText.text = $"¡Rival eligió a {personajeRivalLAN}! Elige tu personaje";
                }
                VerificarAmbosListosLAN();
            }
        }

        void ShowCenter(int index)
        {
            CarouselItemDataLite item = carousel.GetItem(index);
            if (centerTitle != null) centerTitle.text = item != null ? item.title : "";
            if (centerSubtitle != null) centerSubtitle.text = item != null ? item.subtitle : "";

            if (audioSourceMenu != null && clipSonidoCambio != null)
            {
                audioSourceMenu.PlayOneShot(clipSonidoCambio);
            }
        }

        void ShowSelected(int index)
        {
            CarouselItemDataLite item = carousel.GetItem(index);
            if (item == null) return;

            // --- FLUJO MULTIJUGADOR LAN (1 PERSONAJE POR PC) ---
            if (EsModoLAN)
            {
                if (!NetworkManager.Instance.IsConnected || !NetworkManager.Instance.AmbosJugadoresConectados)
                {
                    if (statusText != null) statusText.text = "Conecta ambos PCs primero antes de elegir.";
                    return;
                }

                if (yaElegiEnLAN) return;

                yaElegiEnLAN = true;
                personajeLocalLAN = item.title;

                AudioClip audioLAN = BuscarSonidoUnico(item.title);
                if (audioSourceMenu != null && audioLAN != null)
                {
                    audioSourceMenu.PlayOneShot(audioLAN);
                }

                // Enviar mi elección al otro PC
                NetworkManager.Instance.EnviarSeleccionPersonaje(personajeLocalLAN);

                if (statusText != null)
                {
                    statusText.text = $"Elegiste a {personajeLocalLAN}. Esperando al rival...";
                }

                VerificarAmbosListosLAN();
                return;
            }

            // --- FLUJO MULTIJUGADOR LOCAL ORIGINAL (MISMO PC) ---
            AudioClip audioAEmitir = BuscarSonidoUnico(item.title);
            if (audioSourceMenu != null && audioAEmitir != null)
            {
                audioSourceMenu.PlayOneShot(audioAEmitir);
            }

            if (faseSeleccion == 1)
            {
                personajeJ1 = item.title;
                PlayerPrefs.SetString("P1_Personaje", personajeJ1);

                if (statusText != null)
                    statusText.text = "¡Jugador 1 listo! Ahora elige el Jugador 2";

                faseSeleccion = 2;
            }
            else if (faseSeleccion == 2)
            {
                string personajeJ2 = item.title;
                PlayerPrefs.SetString("P2_Personaje", personajeJ2);
                PlayerPrefs.Save();

                if (statusText != null)
                    statusText.text = "¡Cargando combate...";

                float tiempoEspera = (audioAEmitir != null) ? audioAEmitir.length + 0.2f : 1.5f;
                Invoke(nameof(CargarEscenaCombate), tiempoEspera);
            }
        }

        private void VerificarAmbosListosLAN()
        {
            if (cargandoEscenaLAN) return;
            if (string.IsNullOrEmpty(personajeLocalLAN) || string.IsNullOrEmpty(personajeRivalLAN)) return;

            cargandoEscenaLAN = true;

            // Guardar P1 y P2 correctamente según quién sea el Host (ID 1) y quién el Cliente (ID 2)
            if (NetworkManager.Instance.LocalPlayerId == 1)
            {
                PlayerPrefs.SetString("P1_Personaje", personajeLocalLAN);
                PlayerPrefs.SetString("P2_Personaje", personajeRivalLAN);
            }
            else
            {
                PlayerPrefs.SetString("P1_Personaje", personajeRivalLAN);
                PlayerPrefs.SetString("P2_Personaje", personajeLocalLAN);
            }
            PlayerPrefs.Save();

            if (statusText != null)
            {
                statusText.text = "¡Ambos listos! Entrando a la arena...";
            }

            Invoke(nameof(CargarEscenaCombate), 1.5f);
        }

        void CargarEscenaCombate()
        {
            SceneManager.LoadScene(nombreEscenaPelea);
        }

        AudioClip BuscarSonidoUnico(string nombre)
        {
            if (sonidosPorPersonaje == null) return null;
            foreach (var p in sonidosPorPersonaje)
            {
                if (p.nombrePersonaje == nombre) return p.sonidoSeleccionUnico;
            }
            return null;
        }
    }
}