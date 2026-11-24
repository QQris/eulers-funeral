using System;
using EulersFuneral.Core.MathParser;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EulersFuneral.UI
{
    /// <summary>
    /// RF-06: Entrada de fórmula f(x, y) con validación de sintaxis en tiempo real en español
    /// y generación automática de la superficie cuando la expresión es válida.
    /// </summary>
    public class FormulaInputController : MonoBehaviour
    {
        public const float AutoConfirmDelaySeconds = 0.35f;

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

                if (FormulaInputField.placeholder != null)
                    FormulaInputField.placeholder.gameObject.SetActive(false);
            }

            if (ConfirmFormulaButton != null)
            {
                ConfirmFormulaButton.onClick.AddListener(TryConfirmFormula);
                ConfirmFormulaButton.gameObject.SetActive(false);
            }

            if (LaunchBallButton != null)
            {
                LaunchBallButton.onClick.AddListener(() => OnLaunchRequested?.Invoke());
                LaunchBallButton.interactable = false;
            }

            if (ResetBallButton != null)
            {
                ResetBallButton.onClick.AddListener(() => OnResetRequested?.Invoke());
                ResetBallButton.interactable = true;
            }
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

        public void SetInteractable(bool interactable)
        {
            if (FormulaInputField != null) FormulaInputField.interactable = interactable;
            if (LaunchBallButton != null) LaunchBallButton.interactable = interactable && (ConfirmedFunction != null);
            if (ResetBallButton != null) ResetBallButton.interactable = true;
        }

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
                return;
            }

            if (MathParser.TryParse(text, out _, out string errorMsg))
            {
                IsCurrentSyntaxValid = true;
                if (ValidationFeedbackText != null)
                {
                    ValidationFeedbackText.text = "Sintaxis válida.";
                    ValidationFeedbackText.color = ValidSyntaxColor;
                }

                if (text != _lastConfirmedText)
                {
                    _autoConfirmTimer = AutoConfirmDelaySeconds;
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
            }
        }

        private void Update()
        {
            if (_autoConfirmTimer < 0f)
                return;

            _autoConfirmTimer -= Time.unscaledDeltaTime;
            if (_autoConfirmTimer <= 0f)
            {
                _autoConfirmTimer = -1f;
                TryConfirmFormula();
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

            _lastConfirmedText = text;
            ConfirmedFunction = new MathSurfaceFunction(text);
            if (ValidationFeedbackText != null)
            {
                ValidationFeedbackText.text = $"Superficie activa: f(x, y) = {text}";
                ValidationFeedbackText.color = ValidSyntaxColor;
            }

            OnFormulaConfirmed?.Invoke(ConfirmedFunction);
        }
    }
}
