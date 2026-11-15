using System;
using EulersFuneral.Core.MathParser;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EulersFuneral.UI
{
    /// <summary>
    /// RF-06: Entrada de fórmula f(x, y) con validación de sintaxis en tiempo real en español
    /// y generación de superficie únicamente al confirmar (sin vista previa).
    /// </summary>
    public class FormulaInputController : MonoBehaviour
    {
        [Header("Referencias UI")]
        public TMP_InputField FormulaInputField;
        public TextMeshProUGUI ValidationFeedbackText;
        public Button ConfirmFormulaButton;
        public Button LaunchBallButton;
        public Button ResetBallButton;

        [Header("Colores de Estado")]
        public Color ValidSyntaxColor = new Color(0.55f, 0.90f, 0.55f, 1.0f);
        public Color InvalidSyntaxColor = new Color(0.95f, 0.35f, 0.35f, 1.0f);

        public MathSurfaceFunction ConfirmedFunction { get; private set; }
        public bool IsCurrentSyntaxValid { get; private set; }

        public event Action<MathSurfaceFunction> OnFormulaConfirmed;
        public event Action OnLaunchRequested;
        public event Action OnResetRequested;

        private bool _bindingsInitialized;
        private const float AutoConfirmDelay = 0.6f;
        private float _autoConfirmTimer = -1f;
        private string _lastConfirmedText;

        private void Start()
        {
            InitializeBindings();
        }

        public void InitializeBindings()
        {
            if (_bindingsInitialized)
                return;
            _bindingsInitialized = true;

            if (FormulaInputField != null)
            {
                FormulaInputField.onValueChanged.AddListener(HandleInputChanged);
                FormulaInputField.onSubmit.AddListener(_ => TryConfirmFormula());
            }

            if (ConfirmFormulaButton != null)
            {
                // La malla se genera automáticamente al escribir una fórmula válida: no se necesita botón.
                ConfirmFormulaButton.onClick.AddListener(TryConfirmFormula);
                ConfirmFormulaButton.gameObject.SetActive(false);
            }

            if (FormulaInputField != null)
            {
                if (FormulaInputField.placeholder != null)
                    FormulaInputField.placeholder.gameObject.SetActive(false);

                RectTransform inputRt = FormulaInputField.GetComponent<RectTransform>();
                if (inputRt != null)
                {
                    inputRt.anchorMin = new Vector2(0.12f, 0.28f);
                    inputRt.anchorMax = new Vector2(0.56f, 0.66f);
                }
            }

            if (LaunchBallButton != null)
            {
                LaunchBallButton.onClick.AddListener(() => OnLaunchRequested?.Invoke());
                LaunchBallButton.interactable = false;
                RectTransform launchRt = LaunchBallButton.GetComponent<RectTransform>();
                if (launchRt != null)
                {
                    launchRt.anchorMin = new Vector2(0.58f, 0.28f);
                    launchRt.anchorMax = new Vector2(0.74f, 0.66f);
                }
            }

            if (ResetBallButton == null && LaunchBallButton != null)
            {
                ResetBallButton = CreateRuntimeResetButtonNextToLaunch();
            }

            if (ResetBallButton != null)
            {
                ResetBallButton.onClick.AddListener(() => OnResetRequested?.Invoke());
                ResetBallButton.interactable = true;
                RectTransform resetRt = ResetBallButton.GetComponent<RectTransform>();
                if (resetRt != null)
                {
                    resetRt.anchorMin = new Vector2(0.75f, 0.28f);
                    resetRt.anchorMax = new Vector2(0.91f, 0.66f);
                }
            }
        }

        private Button CreateRuntimeResetButtonNextToLaunch()
        {
            GameObject resetGo = Instantiate(LaunchBallButton.gameObject, LaunchBallButton.transform.parent);
            resetGo.name = "ResetBtn";
            RectTransform resetRt = resetGo.GetComponent<RectTransform>();
            if (resetRt != null)
            {
                resetRt.anchorMin = new Vector2(0.75f, 0.28f);
                resetRt.anchorMax = new Vector2(0.91f, 0.66f);
                resetRt.offsetMin = Vector2.zero;
                resetRt.offsetMax = Vector2.zero;
            }

            TextMeshProUGUI label = resetGo.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = "Reiniciar (R)";
            }

            Button btn = resetGo.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.interactable = true;
            }
            return btn;
        }

        public void ResetForLevel(string initialExpression = "")
        {
            ConfirmedFunction = null;
            _lastConfirmedText = null;
            _autoConfirmTimer = -1f;
            if (FormulaInputField != null)
            {
                FormulaInputField.text = initialExpression;
                FormulaInputField.interactable = true;
            }
            if (LaunchBallButton != null)
            {
                LaunchBallButton.interactable = false;
            }
            HandleInputChanged(initialExpression);
        }

        private void Update()
        {
            if (_autoConfirmTimer < 0f) return;
            _autoConfirmTimer -= Time.unscaledDeltaTime;
            if (_autoConfirmTimer > 0f) return;
            _autoConfirmTimer = -1f;

            if (FormulaInputField == null || !FormulaInputField.interactable || !IsCurrentSyntaxValid) return;
            if (FormulaInputField.text == _lastConfirmedText) return;
            TryConfirmFormula();
        }

        public void SetInteractable(bool interactable, bool keepInputEditable = false)
        {
            if (FormulaInputField != null) FormulaInputField.interactable = interactable || keepInputEditable;
            if (ConfirmFormulaButton != null) ConfirmFormulaButton.interactable = interactable && IsCurrentSyntaxValid;
            if (LaunchBallButton != null) LaunchBallButton.interactable = interactable && (ConfirmedFunction != null);
            if (ResetBallButton != null) ResetBallButton.interactable = true;
        }

        /// <summary>
        /// RF-06: Valida la sintaxis en tiempo real y muestra errores comprensibles en español.
        /// No genera vista previa de la superficie.
        /// </summary>
        public void HandleInputChanged(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                IsCurrentSyntaxValid = false;
                _autoConfirmTimer = -1f;
                if (ValidationFeedbackText != null)
                {
                    ValidationFeedbackText.text = "Escribe una expresión f(x, y), p. ej.: -0.2x - 0.2y o 0.2y^2 - 0.25x";
                    ValidationFeedbackText.color = Color.gray;
                }
                if (ConfirmFormulaButton != null)
                {
                    ConfirmFormulaButton.interactable = false;
                }
                return;
            }

            if (MathParser.TryParse(text, out _, out string errorMsg))
            {
                IsCurrentSyntaxValid = true;
                _autoConfirmTimer = AutoConfirmDelay;
                if (ValidationFeedbackText != null)
                {
                    ValidationFeedbackText.text = "Sintaxis válida. Generando la malla 3D...";
                    ValidationFeedbackText.color = ValidSyntaxColor;
                }
                if (ConfirmFormulaButton != null)
                {
                    ConfirmFormulaButton.interactable = true;
                }
            }
            else
            {
                IsCurrentSyntaxValid = false;
                _autoConfirmTimer = -1f;
                if (ValidationFeedbackText != null)
                {
                    ValidationFeedbackText.text = errorMsg;
                    ValidationFeedbackText.color = InvalidSyntaxColor;
                }
                if (ConfirmFormulaButton != null)
                {
                    ConfirmFormulaButton.interactable = false;
                }
            }
        }

        public void TryConfirmFormula()
        {
            if (FormulaInputField == null) return;

            string text = FormulaInputField.text;
            if (!MathParser.TryParse(text, out _, out string errorMsg))
            {
                if (ValidationFeedbackText != null)
                {
                    ValidationFeedbackText.text = errorMsg;
                    ValidationFeedbackText.color = InvalidSyntaxColor;
                }
                return;
            }

            _autoConfirmTimer = -1f;
            _lastConfirmedText = text;
            ConfirmedFunction = new MathSurfaceFunction(text);
            if (ValidationFeedbackText != null)
            {
                ValidationFeedbackText.text = $"Superficie confirmada: f(x, y) = {text}";
                ValidationFeedbackText.color = ValidSyntaxColor;
            }

            OnFormulaConfirmed?.Invoke(ConfirmedFunction);
        }
    }
}
