using System.Globalization;
using EulersFuneral.Core.Data;
using EulersFuneral.Core.MathParser;
using EulersFuneral.Graphics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EulersFuneral.UI
{
    /// <summary>
    /// RF-07: Panel opcional que muestra la pendiente y el gradiente en el punto bajo el cursor.
    /// Nunca muestra la fórmula del nivel.
    /// </summary>
    public class HintsController : MonoBehaviour
    {
        [Header("Referencias UI")]
        public GameObject HintsPanelRoot;
        public Button ToggleHintsButton;
        public TextMeshProUGUI CursorCoordinatesText;
        public TextMeshProUGUI GradientText;
        public TextMeshProUGUI SlopeText;
        public TextMeshProUGUI ConceptualHintText;

        private MathSurfaceFunction _activeSurface;
        private LevelData _currentLevel;
        private bool _isOpen;
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

            if (ToggleHintsButton != null)
            {
                ToggleHintsButton.onClick.AddListener(TogglePanel);
            }
            SetPanelOpen(false);
        }

        public void SetContext(LevelData level, MathSurfaceFunction surface)
        {
            _currentLevel = level;
            _activeSurface = surface;

            if (ConceptualHintText != null && level != null)
            {
                ConceptualHintText.text = level.ConceptualHint;
            }
        }

        public void TogglePanel()
        {
            SetPanelOpen(!_isOpen);
        }

        public void SetPanelOpen(bool open)
        {
            _isOpen = open;
            if (HintsPanelRoot != null)
            {
                HintsPanelRoot.SetActive(_isOpen);
            }
        }

        private void Update()
        {
            if (!_isOpen || Camera.main == null)
                return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            double mathX = 0, mathY = 0;
            bool foundPoint = false;

            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                MathCoords.FromUnity(hit.point, out mathX, out mathY, out _);
                foundPoint = true;
            }
            else
            {
                // Fallback: intersection with reference plane z = 0 (Unity Y = 0)
                Plane refPlane = new Plane(Vector3.up, Vector3.zero);
                if (refPlane.Raycast(ray, out float enter))
                {
                    Vector3 worldPt = ray.GetPoint(enter);
                    MathCoords.FromUnity(worldPt, out mathX, out mathY, out _);
                    if (_currentLevel == null ||
                        (mathX >= _currentLevel.MinX && mathX <= _currentLevel.MaxX &&
                         mathY >= _currentLevel.MinY && mathY <= _currentLevel.MaxY))
                    {
                        foundPoint = true;
                    }
                }
            }

            if (!foundPoint)
            {
                DisplayNoPoint();
                return;
            }

            if (_activeSurface == null)
            {
                if (CursorCoordinatesText != null)
                    CursorCoordinatesText.text = string.Format(CultureInfo.InvariantCulture, "Punto: ({0:F2}, {1:F2}, --)", mathX, mathY);
                if (GradientText != null)
                    GradientText.text = "Gradiente grad f: Confirma una superficie primero";
                if (SlopeText != null)
                    SlopeText.text = "Pendiente ||grad f||: --";
                return;
            }

            SurfaceDerivatives d = _activeSurface.EvaluateWithDerivatives(mathX, mathY);
            if (!d.IsDefined)
            {
                if (CursorCoordinatesText != null)
                    CursorCoordinatesText.text = string.Format(CultureInfo.InvariantCulture, "Punto: ({0:F2}, {1:F2}) [Vacío / Indefinido]", mathX, mathY);
                if (GradientText != null)
                    GradientText.text = "Gradiente grad f: No definido";
                if (SlopeText != null)
                    SlopeText.text = "Pendiente ||grad f||: No definida";
                return;
            }

            if (CursorCoordinatesText != null)
            {
                CursorCoordinatesText.text = string.Format(
                    CultureInfo.InvariantCulture,
                    "Punto (x, y, z): ({0:F2}, {1:F2}, {2:F2})",
                    mathX, mathY, d.Z);
            }

            if (GradientText != null)
            {
                GradientText.text = string.Format(
                    CultureInfo.InvariantCulture,
                    "Gradiente grad f(x, y) = ({0:F3}, {1:F3})",
                    d.Fx, d.Fy);
            }

            if (SlopeText != null)
            {
                double slopeAngleDeg = System.Math.Atan(d.GradientMagnitude) * (180.0 / System.Math.PI);
                SlopeText.text = string.Format(
                    CultureInfo.InvariantCulture,
                    "Pendiente ||grad f|| = {0:F3} ({1:F1}\u00B0)",
                    d.GradientMagnitude, slopeAngleDeg);
            }
        }

        private void DisplayNoPoint()
        {
            if (CursorCoordinatesText != null)
                CursorCoordinatesText.text = "Punto: Apunta con el cursor sobre la mesa 3D";
            if (GradientText != null)
                GradientText.text = "Gradiente grad f: (--, --)";
            if (SlopeText != null)
                SlopeText.text = "Pendiente ||grad f||: --";
        }
    }
}
