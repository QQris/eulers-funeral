using System;
using System.Collections;
using System.IO;
using EulersFuneral.Core.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace EulersFuneral.UI
{
    /// <summary>
    /// RF-04: Reproduce el video de transición Manim correspondiente al nivel (Nivel0.mp4 .. Nivel5.mp4)
    /// y aplica una transición suave de opacidad al pasar a la fase de edición.
    /// </summary>
    public class LaTeXTitleAnimation : MonoBehaviour
    {
        public const float DefaultTotalDuration = 3.0f;

        [Header("Referencias UI y Video Manim")]
        public CanvasGroup OverlayGroup;
        public RawImage VideoDisplayRawImage;
        public VideoPlayer ManimVideoPlayer;
        public VideoClip[] LevelVideoClips = new VideoClip[6];

        public RawImage ManimVideoRawImage
        {
            get => VideoDisplayRawImage;
            set => VideoDisplayRawImage = value;
        }

        public VideoPlayer VideoPlayerComponent
        {
            get => ManimVideoPlayer;
            set => ManimVideoPlayer = value;
        }

        [Header("Respaldo Tipográfico (Opcional)")]
        public TextMeshProUGUI PrimaryGlyphText;
        public TextMeshProUGUI SecondaryGlyphText;
        public TextMeshProUGUI SubtitleText;

        private RenderTexture _renderTexture;
        private Coroutine _activeRoutine;
        private bool _videoFinished;

        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            EnsureVideoComponents();
        }

        private void OnDestroy()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }
        }

        private void EnsureVideoComponents()
        {
            if (ManimVideoPlayer == null)
            {
                ManimVideoPlayer = GetComponent<VideoPlayer>();
                if (ManimVideoPlayer == null)
                {
                    ManimVideoPlayer = gameObject.AddComponent<VideoPlayer>();
                }
            }

            ManimVideoPlayer.playOnAwake = false;
            ManimVideoPlayer.isLooping = false;
            ManimVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            ManimVideoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            ManimVideoPlayer.skipOnDrop = true;

            if (VideoDisplayRawImage == null)
            {
                VideoDisplayRawImage = GetComponentInChildren<RawImage>(true);
            }
        }

        public void PlayLevelTitle(LevelData level, Action onFinished)
        {
            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
            }
            gameObject.SetActive(true);
            _activeRoutine = StartCoroutine(PlayManimTransitionRoutine(level, onFinished));
        }

        private IEnumerator PlayManimTransitionRoutine(LevelData level, Action onFinished)
        {
            IsPlaying = true;
            EnsureVideoComponents();

            if (OverlayGroup != null)
            {
                OverlayGroup.alpha = 1f;
                OverlayGroup.blocksRaycasts = true;
            }

            // Ocultar textos antiguos cuando se usa el video de Manim
            if (PrimaryGlyphText != null) PrimaryGlyphText.gameObject.SetActive(false);
            if (SecondaryGlyphText != null) SecondaryGlyphText.gameObject.SetActive(false);
            if (SubtitleText != null) SubtitleText.gameObject.SetActive(false);

            VideoClip clip = level != null ? level.TitleVideoClip : null;
            if (clip == null && level != null && LevelVideoClips != null &&
                level.LevelIndex >= 0 && level.LevelIndex < LevelVideoClips.Length)
            {
                clip = LevelVideoClips[level.LevelIndex];
            }

            string fallbackFilePath = string.Empty;
            if (level != null)
            {
                string webmCandidate = Path.Combine(Application.dataPath, "Transitions", $"Nivel{level.LevelIndex}.webm");
                string mp4Candidate = Path.Combine(Application.dataPath, "Transitions", $"Nivel{level.LevelIndex}.mp4");
                fallbackFilePath = File.Exists(webmCandidate) ? webmCandidate : mp4Candidate;
            }

            bool canPlayVideo = (clip != null) || (!string.IsNullOrEmpty(fallbackFilePath) && File.Exists(fallbackFilePath));

            if (canPlayVideo && ManimVideoPlayer != null && VideoDisplayRawImage != null)
            {
                if (_renderTexture == null)
                {
                    _renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
                    _renderTexture.Create();
                }

                // Limpiar el RenderTexture a negro antes de reproducir
                RenderTexture prevActive = RenderTexture.active;
                RenderTexture.active = _renderTexture;
                GL.Clear(true, true, Color.black);
                RenderTexture.active = prevActive;

                ManimVideoPlayer.targetTexture = _renderTexture;
                VideoDisplayRawImage.texture = _renderTexture;
                VideoDisplayRawImage.color = Color.white;
                VideoDisplayRawImage.gameObject.SetActive(true);

                if (clip != null)
                {
                    ManimVideoPlayer.source = VideoSource.VideoClip;
                    ManimVideoPlayer.clip = clip;
                }
                else
                {
                    ManimVideoPlayer.source = VideoSource.Url;
                    ManimVideoPlayer.url = fallbackFilePath;
                }

                _videoFinished = false;
                ManimVideoPlayer.loopPointReached += OnVideoLoopPointReached;
                ManimVideoPlayer.errorReceived += OnVideoErrorReceived;

                ManimVideoPlayer.Prepare();
                float prepareTimeout = 4.0f;
                while (!ManimVideoPlayer.isPrepared && !_videoFinished && prepareTimeout > 0f)
                {
                    prepareTimeout -= Time.unscaledDeltaTime;
                    yield return null;
                }

                if (ManimVideoPlayer.isPrepared && !_videoFinished)
                {
                    ManimVideoPlayer.Play();

                    float maxPlayDuration = clip != null ? (float)clip.length + 1.5f : 12.0f;
                    float elapsedPlay = 0f;
                    while (!_videoFinished && elapsedPlay < maxPlayDuration)
                    {
                        elapsedPlay += Time.unscaledDeltaTime;
                        if (elapsedPlay > 0.3f && !ManimVideoPlayer.isPlaying)
                        {
                            break;
                        }
                        yield return null;
                    }
                }

                ManimVideoPlayer.loopPointReached -= OnVideoLoopPointReached;
                ManimVideoPlayer.errorReceived -= OnVideoErrorReceived;
                ManimVideoPlayer.Stop();
            }
            else
            {
                // Respaldo tipográfico solo si no existiera el archivo de video
                yield return AnimateFallbackTextRoutine(level);
            }

            // Transición suave de aumento de opacidad del contenido presente (fade-out del overlay de video)
            const float fadeOutDuration = 0.55f;
            float fadeElapsed = 0f;
            while (fadeElapsed < fadeOutDuration)
            {
                fadeElapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(fadeElapsed / fadeOutDuration));
                if (OverlayGroup != null)
                {
                    OverlayGroup.alpha = 1f - t;
                }
                yield return null;
            }

            if (OverlayGroup != null)
            {
                OverlayGroup.alpha = 0f;
                OverlayGroup.blocksRaycasts = false;
            }

            IsPlaying = false;
            gameObject.SetActive(false);
            onFinished?.Invoke();
        }

        private void OnVideoLoopPointReached(VideoPlayer vp)
        {
            _videoFinished = true;
        }

        private void OnVideoErrorReceived(VideoPlayer vp, string message)
        {
            Debug.LogWarning($"[LaTeXTitleAnimation] Error reproduciendo video: {message}");
            _videoFinished = true;
        }

        private IEnumerator AnimateFallbackTextRoutine(LevelData level)
        {
            if (PrimaryGlyphText != null)
            {
                PrimaryGlyphText.gameObject.SetActive(true);
                PrimaryGlyphText.text = FormatLaTeXToRichText(level != null ? level.FinalDisplayedSymbol : "");
                PrimaryGlyphText.alpha = 1f;
            }
            yield return new WaitForSecondsRealtime(2.0f);
        }

        public static string FormatLaTeXToRichText(string latex)
        {
            if (string.IsNullOrEmpty(latex))
                return string.Empty;

            return latex
                .Replace("e^{i\\pi}", "<i>e</i><sup><i>i</i>\u00B7pi</sup>")
                .Replace("e^{\\ln 4}", "<i>e</i><sup>ln 4</sup>")
                .Replace("e^4", "<i>e</i><sup>4</sup>")
                .Replace("\\lfloor e \\rfloor", "floor(<i>e</i>)")
                .Replace("\\lceil e \\rceil", "ceil(<i>e</i>)")
                .Replace("\\lim_{n\\to\\infty} (1 + 4/n)^n", "lim<sub><i>n</i>->inf</sub> (1 + 4/<i>n</i>)<sup><i>n</i></sup>")
                .Replace("\\pi", "pi");
        }
    }
}
