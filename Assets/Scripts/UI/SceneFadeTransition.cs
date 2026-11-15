using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EulersFuneral.UI
{
    /// <summary>
    /// Controla la transición de aumento progresivo de opacidad del contenido presente
    /// al cambiar de escena o al mostrar paneles principales.
    /// </summary>
    public class SceneFadeTransition : MonoBehaviour
    {
        public static SceneFadeTransition Instance { get; private set; }

        [Header("Configuración de Transición")]
        public CanvasGroup ContentCanvasGroup;
        public CanvasGroup CurtainOverlayGroup;
        public float FadeDuration = 0.75f;

        private Coroutine _activeRoutine;
        private Coroutine _panelRoutine;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // Al iniciar cualquier escena, animar el aumento de opacidad del contenido presente (0 -> 1)
            PlaySceneEnterFadeIn();
        }

        public void PlaySceneEnterFadeIn(Action onComplete = null)
        {
            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
            }
            gameObject.SetActive(true);
            _activeRoutine = StartCoroutine(FadeInContentRoutine(FadeDuration, onComplete));
        }

        public void TransitionToScene(string sceneName)
        {
            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
            }
            gameObject.SetActive(true);
            _activeRoutine = StartCoroutine(TransitionToSceneRoutine(sceneName));
        }

        public void AnimatePanelOpacityIn(GameObject panelObject, float duration = 0.55f)
        {
            if (panelObject == null) return;
            CanvasGroup cg = panelObject.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = panelObject.AddComponent<CanvasGroup>();
            }

            if (!Application.isPlaying)
            {
                cg.alpha = 1f;
                return;
            }

            if (_panelRoutine != null)
            {
                StopCoroutine(_panelRoutine);
            }
            _panelRoutine = StartCoroutine(AnimateSingleGroupInRoutine(cg, duration));
        }

        private IEnumerator FadeInContentRoutine(float duration, Action onComplete)
        {
            if (ContentCanvasGroup != null)
            {
                ContentCanvasGroup.alpha = 0f;
            }

            if (CurtainOverlayGroup != null)
            {
                CurtainOverlayGroup.gameObject.SetActive(true);
                CurtainOverlayGroup.alpha = 1f;
                CurtainOverlayGroup.blocksRaycasts = true;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

                if (ContentCanvasGroup != null)
                {
                    ContentCanvasGroup.alpha = t;
                }
                if (CurtainOverlayGroup != null)
                {
                    CurtainOverlayGroup.alpha = 1f - t;
                }
                yield return null;
            }

            if (ContentCanvasGroup != null)
            {
                ContentCanvasGroup.alpha = 1f;
            }

            if (CurtainOverlayGroup != null)
            {
                CurtainOverlayGroup.alpha = 0f;
                CurtainOverlayGroup.blocksRaycasts = false;
                CurtainOverlayGroup.gameObject.SetActive(false);
            }

            onComplete?.Invoke();
        }

        private IEnumerator TransitionToSceneRoutine(string sceneName)
        {
            float outDuration = FadeDuration * 0.55f;

            if (CurtainOverlayGroup != null)
            {
                CurtainOverlayGroup.gameObject.SetActive(true);
                CurtainOverlayGroup.alpha = 0f;
                CurtainOverlayGroup.blocksRaycasts = true;
            }

            float elapsed = 0f;
            while (elapsed < outDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / outDuration));

                if (ContentCanvasGroup != null)
                {
                    ContentCanvasGroup.alpha = 1f - t;
                }
                if (CurtainOverlayGroup != null)
                {
                    CurtainOverlayGroup.alpha = t;
                }
                yield return null;
            }

            SceneManager.LoadScene(sceneName);
        }

        private static IEnumerator AnimateSingleGroupInRoutine(CanvasGroup cg, float duration)
        {
            cg.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                cg.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            cg.alpha = 1f;
        }
    }
}
