using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EulersFuneral.UI
{
    /// <summary>
    /// Efecto estético de botón sin fondo con animación suave de subrayado al pasar el cursor por encima.
    /// </summary>
    public class UnderlineButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [Header("Referencias")]
        public RectTransform UnderlineRect;
        public Image UnderlineImage;
        public TextMeshProUGUI LabelText;

        [Header("Configuración de Animación")]
        public float AnimationSpeed = 9.0f;
        public float MaxUnderlineWidthNormalized = 0.85f;
        public Color NormalTextColor = new Color(0.82f, 0.82f, 0.82f, 1.0f);
        public Color HoverTextColor = Color.white;
        public Color UnderlineColor = Color.white;

        private float _currentProgress;
        private bool _isHovered;
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();

            // Asegurar que el fondo del botón sea totalmente transparente pero reciba clics
            var bgImage = GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.color = new Color(0f, 0f, 0f, 0f);
                bgImage.raycastTarget = true;
            }

            if (_button != null)
            {
                _button.transition = Selectable.Transition.None;
            }

            if (LabelText == null)
            {
                LabelText = GetComponentInChildren<TextMeshProUGUI>();
            }

            EnsureUnderlineExists();
            ApplyVisualState(0f);
        }

        private void OnEnable()
        {
            _isHovered = false;
            _currentProgress = 0f;
            ApplyVisualState(0f);
        }

        public void EnsureUnderlineExists()
        {
            if (UnderlineRect == null)
            {
                Transform existing = transform.Find("UnderlineBar");
                if (existing != null)
                {
                    UnderlineRect = existing.GetComponent<RectTransform>();
                    UnderlineImage = existing.GetComponent<Image>();
                }
                else
                {
                    var go = new GameObject("UnderlineBar");
                    go.transform.SetParent(transform, false);
                    UnderlineRect = go.AddComponent<RectTransform>();
                    UnderlineImage = go.AddComponent<Image>();
                }
            }

            if (UnderlineImage != null)
            {
                UnderlineImage.color = UnderlineColor;
                UnderlineImage.raycastTarget = false;
            }

            if (UnderlineRect != null)
            {
                UnderlineRect.anchorMin = new Vector2(0f, 0.06f);
                UnderlineRect.anchorMax = new Vector2(0f, 0.12f);
                UnderlineRect.offsetMin = Vector2.zero;
                UnderlineRect.offsetMax = Vector2.zero;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable)
                return;
            _isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (_button != null && !_button.interactable)
                return;
            _isHovered = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _isHovered = false;
        }

        private void Update()
        {
            bool canHover = _isHovered && (_button == null || _button.interactable);
            float target = canHover ? 1.0f : 0.0f;

            if (Mathf.Abs(_currentProgress - target) > 0.001f)
            {
                _currentProgress = Mathf.MoveTowards(_currentProgress, target, Time.unscaledDeltaTime * AnimationSpeed);
                ApplyVisualState(_currentProgress);
            }
        }

        private void ApplyVisualState(float t)
        {
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (UnderlineRect != null)
            {
                bool centerAligned = LabelText != null &&
                    (LabelText.alignment == TextAlignmentOptions.Center || LabelText.alignment == TextAlignmentOptions.Midline);

                if (centerAligned)
                {
                    float halfWidth = 0.5f * smoothT * MaxUnderlineWidthNormalized;
                    UnderlineRect.anchorMin = new Vector2(0.5f - halfWidth, 0.06f);
                    UnderlineRect.anchorMax = new Vector2(0.5f + halfWidth, 0.12f);
                }
                else
                {
                    UnderlineRect.anchorMin = new Vector2(0f, 0.06f);
                    UnderlineRect.anchorMax = new Vector2(smoothT * MaxUnderlineWidthNormalized, 0.12f);
                }
                UnderlineRect.offsetMin = Vector2.zero;
                UnderlineRect.offsetMax = Vector2.zero;
            }

            if (UnderlineImage != null)
            {
                Color c = UnderlineColor;
                c.a = smoothT;
                UnderlineImage.color = c;
            }

            if (LabelText != null)
            {
                bool interactable = _button == null || _button.interactable;
                if (!interactable)
                {
                    LabelText.color = new Color(0.45f, 0.45f, 0.45f, 0.7f);
                }
                else
                {
                    LabelText.color = Color.Lerp(NormalTextColor, HoverTextColor, smoothT);
                }
            }
        }
    }
}
