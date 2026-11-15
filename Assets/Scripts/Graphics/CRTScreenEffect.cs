using UnityEngine;
using UnityEngine.SceneManagement;

namespace EulersFuneral.Graphics
{
    /// <summary>
    /// Opción A: Efecto de monitor CRT de pantalla completa (Curvatura de tubo, Aberración Cromática RGB,
    /// Scanlines, Viñeteado y Grano Analógico) aplicado sobre la cámara principal e integrando
    /// tanto la escena 3D como los Canvas de UI y los videos de Manim.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class CRTScreenEffect : MonoBehaviour
    {
        [Header("Parámetros del Monitor CRT")]
        [Range(0.0f, 0.12f)]
        public float Curvature = 0.018f;

        [Range(0.0f, 0.01f)]
        public float ChromaticAberration = 0.0009f;

        [Range(0.0f, 0.5f)]
        public float ScanlineIntensity = 0.11f;

        public float ScanlineCount = 520.0f;

        [Range(0.0f, 1.0f)]
        public float VignetteIntensity = 0.14f;

        [Range(0.0f, 0.1f)]
        public float NoiseIntensity = 0.012f;

        [Range(0.8f, 1.4f)]
        public float BrightnessBoost = 1.10f;

        private Camera _camera;
        private Material _crtMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoAttachOnLoad()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureOnMainCamera();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureOnMainCamera();
        }

        public static CRTScreenEffect EnsureOnMainCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = Object.FindFirstObjectByType<Camera>();
            }
            if (cam == null)
                return null;

            CRTScreenEffect crt = cam.GetComponent<CRTScreenEffect>();
            if (crt == null)
            {
                crt = cam.gameObject.AddComponent<CRTScreenEffect>();
            }
            crt.BindCanvasesToCamera();
            return crt;
        }

        private void OnEnable()
        {
            _camera = GetComponent<Camera>();
            if (_camera != null && _camera.nearClipPlane > 0.05f)
            {
                _camera.nearClipPlane = 0.05f;
            }
            EnsureMaterial();
            BindCanvasesToCamera();
        }

        private void Start()
        {
            BindCanvasesToCamera();
        }

        /// <summary>
        /// Configura los Canvas principales en ScreenSpaceCamera apuntando a esta cámara
        /// para que el post-procesado CRT (curvatura, scanlines, aberración RGB) afecte
        /// también al menú principal, los botones, el HUD y los videos de Manim.
        /// </summary>
        public void BindCanvasesToCamera()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }
            if (_camera == null)
                return;

            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Canvas c in canvases)
            {
                if (c != null && c.isRootCanvas)
                {
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = _camera;
                    c.planeDistance = 0.25f;
                    c.sortingOrder = 100;
                }
            }
        }

        private void EnsureMaterial()
        {
            if (_crtMaterial != null)
                return;

            Shader shader = Resources.Load<Shader>("Shaders/CRTPostProcess");
            if (shader == null)
            {
                shader = Shader.Find("EulersFuneral/CRTPostProcess");
            }

            if (shader != null && shader.isSupported)
            {
                _crtMaterial = new Material(shader)
                {
                    name = "CRTPostProcess_RuntimeMat",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            EnsureMaterial();
            if (_crtMaterial == null)
            {
                UnityEngine.Graphics.Blit(source, destination);
                return;
            }

            _crtMaterial.SetFloat("_Curvature", Curvature);
            _crtMaterial.SetFloat("_ChromaticAberration", ChromaticAberration);
            _crtMaterial.SetFloat("_ScanlineIntensity", ScanlineIntensity);
            _crtMaterial.SetFloat("_ScanlineCount", ScanlineCount);
            _crtMaterial.SetFloat("_VignetteIntensity", VignetteIntensity);
            _crtMaterial.SetFloat("_NoiseIntensity", NoiseIntensity);
            _crtMaterial.SetFloat("_BrightnessBoost", BrightnessBoost);

            UnityEngine.Graphics.Blit(source, destination, _crtMaterial);
        }

        private void OnDestroy()
        {
            if (_crtMaterial != null)
            {
                if (Application.isPlaying) Destroy(_crtMaterial);
                else DestroyImmediate(_crtMaterial);
            }
        }
    }
}
