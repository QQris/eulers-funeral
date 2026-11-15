using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace EulersFuneral.UI
{
    /// <summary>
    /// RF-13: Cuando la gravedad cambia, el HUD muestra un aviso con el nuevo valor durante 2 s.
    /// </summary>
    public class GravityToast : MonoBehaviour
    {
        public const float DisplayDurationSeconds = 2.0f; // RF-13: 2 s

        public CanvasGroup ToastCanvasGroup;
        public TextMeshProUGUI ToastMessageText;

        private Coroutine _toastRoutine;

        public void ShowGravityNotice(float newGravity)
        {
            if (_toastRoutine != null)
            {
                StopCoroutine(_toastRoutine);
            }
            gameObject.SetActive(true);
            _toastRoutine = StartCoroutine(ShowToastRoutine(newGravity));
        }

        private IEnumerator ShowToastRoutine(float newGravity)
        {
            if (ToastMessageText != null)
            {
                ToastMessageText.text = string.Format(
                    CultureInfo.InvariantCulture,
                    "Aviso: Gravedad del nivel ajustada a g = {0:F2} m/s\u00B2",
                    newGravity);
            }

            if (ToastCanvasGroup != null)
            {
                ToastCanvasGroup.alpha = 1.0f;
            }

            yield return new WaitForSecondsRealtime(DisplayDurationSeconds);

            float fadeTime = 0.25f;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                if (ToastCanvasGroup != null)
                {
                    ToastCanvasGroup.alpha = 1.0f - Mathf.Clamp01(elapsed / fadeTime);
                }
                yield return null;
            }

            if (ToastCanvasGroup != null)
            {
                ToastCanvasGroup.alpha = 0.0f;
            }

            gameObject.SetActive(false);
        }
    }
}
