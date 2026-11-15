using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EulersFuneral.UI
{
    /// <summary>
    /// Botón minimalista sin fondo que muestra una animación estética de subrayado
    /// al pasar el cursor por encima (hover).
    /// </summary>
    public class HoverUnderlineButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Referencias")]
        public RectTransform UnderlineRect;
        public Image UnderlineImage;
        public TextMeshProUGUI LabelText;

        [Header("Parámetros de Animación")]
        public float AnimationSpeed = 7.0f;
        public Color IdleTextColor = new Color(0.84f, 0.84f, 0.84f, 1.0f);
        public Color HoverTextColor = Color.white;
        public Color UnderlineColor = Color.white;

        private bool _isHovered;
        private float _progress;
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            ApplyVisualState(0f);
        }

        private void OnEnable()
        {
            _isHovered = false;
            _progress = 0f;
            ApplyVisualState(0f);
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

        private void Update()
        {
            bool canHover = _isHovered && (_button == null || _button.interactable);
            float target = canHover ? 1.0f : 0.0f;

            if (!Mathf.Approximately(_progress, target))
            {
                _progress = Mathf.MoveTowards(_progress, target, Time.unscaledDeltaTime * AnimationSpeed);
                float eased = Mathf.SmoothStep(0f, 1f, _progress);
                ApplyVisualState(eased);
            }
        }

        private void ApplyVisualState(float easedProgress)
        {
            if (UnderlineRect != null)
            {
                UnderlineRect.localScale = new Vector3(easedProgress, 1f, 1f);
            }

            if (UnderlineImage != null)
            {
                Color c = UnderlineColor;
                c.a = easedProgress;
                UnderlineImage.color = c;
            }

            if (LabelText != null)
            {
                if (_button != null && !_button.interactable)
                {
                    LabelText.color = new Color(0.45f, 0.45f, 0.45f, 0.7f);
                }
                else
                {
                    LabelText.color = Color.Lerp(IdleTextColor, HoverTextColor, easedProgress);
                }
            }
        }
    }
}
