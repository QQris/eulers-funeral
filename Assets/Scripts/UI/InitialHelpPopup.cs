using EulersFuneral.Core.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EulersFuneral.UI
{
    /// <summary>
    /// RF-03: Cuadro de ayuda inicial. Aparece la primera vez que se abre el juego.
    /// Explica los controles (clic derecho para orbitar, rueda para zoom, teclado para escribir la fórmula)
    /// y el objetivo general: llevar la pelota blanca hasta el objetivo rojo recogiendo las estrellas rojas.
    /// </summary>
    public class InitialHelpPopup : MonoBehaviour
    {
        public GameObject PopupRoot;
        public TextMeshProUGUI HelpBodyText;
        public Button CloseButton;

        public const string DefaultHelpMessage =
            "<b>BIENVENIDO A EULER'S FUNERAL</b>\n\n" +
            "<b>Objetivo General:</b>\n" +
            "Guía la pelota blanca desde el marcador de inicio (A) para recolectar las <b>3 estrellas rojas</b> " +
            "distribuidas en el espacio 3D (ejes X, Y y altura Z).\n\n" +
            "<b>Controles:</b>\n" +
            "• <b>Clic derecho sostenido:</b> Orbitar la cámara 3D (sin bajar del plano).\n" +
            "• <b>Rueda del ratón:</b> Zoom (acercar / alejar).\n" +
            "• <b>Teclado:</b> Escribe la ecuación de dos variables <i>f(x, y)</i> en español; la malla de 100×100 puntos se genera automáticamente al ser válida " +
            "(admite multiplicación implícita como <i>-0.2x - 0.2y</i>, <i>0.2y^2 - 0.25x</i>, <i>sen(x)</i>, <i>floor(x)</i>, <i>ceil(x)</i>, <i>e^x</i>).\n" +
            "• <b>Lanzar:</b> Inicia la simulación física Lagrangiana (RK4).";

        private bool _bindingsInitialized;

        private void Start()
        {
            InitializeBindings();
        }

        public void InitializeBindings()
        {
            if (_bindingsInitialized)
                return;
            _bindingsInitialized = true;

            if (HelpBodyText != null && string.IsNullOrEmpty(HelpBodyText.text))
            {
                HelpBodyText.text = DefaultHelpMessage;
            }

            if (CloseButton != null)
            {
                CloseButton.onClick.AddListener(CloseHelp);
            }
        }

        public void ShowIfFirstTime()
        {
            SaveProgressData data = SaveSystem.Load();
            if (!data.HasSeenInitialHelp)
            {
                ShowPopup();
            }
            else if (PopupRoot != null)
            {
                PopupRoot.SetActive(false);
            }
        }

        public void ShowPopup()
        {
            if (HelpBodyText != null)
            {
                HelpBodyText.text = DefaultHelpMessage;
            }
            if (PopupRoot != null)
            {
                PopupRoot.SetActive(true);
            }
        }

        public void CloseHelp()
        {
            SaveSystem.MarkInitialHelpSeen();
            if (PopupRoot != null)
            {
                PopupRoot.SetActive(false);
            }
        }
    }
}
