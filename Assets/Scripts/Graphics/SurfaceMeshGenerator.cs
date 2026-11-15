using System;
using System.Collections.Generic;
using EulersFuneral.Core.MathParser;
using UnityEngine;

namespace EulersFuneral.Graphics
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SurfaceMeshGenerator : MonoBehaviour
    {
        public const int GridResolution = 100; // RF-06: malla evaluada en 100 x 100 puntos
        public const int DefaultWireframeLineStride = 5; // Espaciado entre líneas internas de la malla (cada 5 celdas = 0.5 unidades)
        public const float AppearAnimationDuration = 1.5f; // RF-06: z(t) = t * f(x, y) en 1.5 s
        public const float MaxAbsHeight = 25.0f;

        [Header("Configuración Visual")]
        public Color WireframeColor = new Color(0.75f, 0.75f, 0.75f, 1.0f); // Gris claro sobre fondo negro
        [Range(1, 12)]
        public int WireframeLineStride = DefaultWireframeLineStride;

        private MeshFilter _wireMeshFilter;
        private MeshRenderer _wireMeshRenderer;
        private MeshCollider _raycastCollider;

        private Mesh _wireframeMesh;
        private Mesh _colliderMesh;

        private Vector3[] _baseVertices;
        private float[] _targetMathZ;
        private bool[] _validVertices;
        private Vector3[] _animatedVertices;

        private readonly List<int> _lineIndices = new List<int>(GridResolution * GridResolution * 4);
        private readonly List<int> _triangleIndices = new List<int>(GridResolution * GridResolution * 6);

        private float _animElapsed = AppearAnimationDuration;
        private bool _isAnimating;

        public bool IsAnimating => _isAnimating;
        public MathSurfaceFunction CurrentFunction { get; private set; }

        public event Action OnSurfaceAppearFinished;

        private void Awake()
        {
            _wireMeshFilter = GetComponent<MeshFilter>();
            _wireMeshRenderer = GetComponent<MeshRenderer>();

            _raycastCollider = GetComponent<MeshCollider>();
            if (_raycastCollider == null)
            {
                _raycastCollider = gameObject.AddComponent<MeshCollider>();
            }

            _wireframeMesh = new Mesh { name = "SurfaceWireframe_100x100" };
            _wireframeMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _wireMeshFilter.sharedMesh = _wireframeMesh;

            _colliderMesh = new Mesh { name = "SurfaceCollider_100x100" };
            _colliderMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            EnsureMaterial();
            AllocateBuffers();
        }

        private void EnsureMaterial()
        {
            if (_wireMeshRenderer.sharedMaterial == null)
            {
                _wireMeshRenderer.sharedMaterial = MaterialHelper.CreateUnlitColorMaterial(WireframeColor);
            }
        }

        private void AllocateBuffers()
        {
            int totalPoints = GridResolution * GridResolution;
            if (_baseVertices == null || _baseVertices.Length != totalPoints)
            {
                _baseVertices = new Vector3[totalPoints];
                _targetMathZ = new float[totalPoints];
                _validVertices = new bool[totalPoints];
                _animatedVertices = new Vector3[totalPoints];
            }
        }

        /// <summary>
        /// RF-06: Evalúa f(x, y) en una malla de 100 x 100 puntos y genera la superficie
        /// con animación z(t) = t * f(x, y) de t = 0 a 1 en 1.5 s.
        /// </summary>
        public void GenerateSurface(
            MathSurfaceFunction function,
            float minX, float maxX,
            float minY, float maxY,
            bool animateAppearance = true)
        {
            AllocateBuffers();
            CurrentFunction = function;

            _lineIndices.Clear();
            _triangleIndices.Clear();

            float stepX = (maxX - minX) / (GridResolution - 1);
            float stepY = (maxY - minY) / (GridResolution - 1);

            // Evaluate 100 x 100 grid
            for (int iy = 0; iy < GridResolution; iy++)
            {
                double mathY = minY + iy * stepY;
                int rowOffset = iy * GridResolution;

                for (int ix = 0; ix < GridResolution; ix++)
                {
                    double mathX = minX + ix * stepX;
                    int idx = rowOffset + ix;

                    double z = function != null ? function.Evaluate(mathX, mathY) : double.NaN;
                    bool isValid = !double.IsNaN(z) && !double.IsInfinity(z) && Math.Abs(z) <= MaxAbsHeight;

                    _validVertices[idx] = isValid;
                    float clampedZ = isValid ? (float)z : 0.0f;
                    _targetMathZ[idx] = clampedZ;
                    _baseVertices[idx] = MathCoords.ToUnity(mathX, mathY, 0.0);
                    _animatedVertices[idx] = MathCoords.ToUnity(mathX, mathY, animateAppearance ? 0.0f : clampedZ);
                }
            }

            int stride = Mathf.Max(1, WireframeLineStride);

            // Build line topology for wireframe (spaced by stride) and triangle topology for raycast collider
            for (int iy = 0; iy < GridResolution; iy++)
            {
                int rowOffset = iy * GridResolution;
                bool drawHorizontalRow = (iy % stride == 0) || (iy == GridResolution - 1);

                for (int ix = 0; ix < GridResolution; ix++)
                {
                    int i00 = rowOffset + ix;
                    if (!_validVertices[i00])
                        continue;

                    bool drawVerticalCol = (ix % stride == 0) || (ix == GridResolution - 1);

                    // Horizontal segment along X (only on spaced rows)
                    if (drawHorizontalRow && ix + 1 < GridResolution)
                    {
                        int i10 = i00 + 1;
                        if (_validVertices[i10])
                        {
                            _lineIndices.Add(i00);
                            _lineIndices.Add(i10);
                        }
                    }

                    // Vertical segment along Y (only on spaced columns)
                    if (drawVerticalCol && iy + 1 < GridResolution)
                    {
                        int i01 = i00 + GridResolution;
                        if (_validVertices[i01])
                        {
                            _lineIndices.Add(i00);
                            _lineIndices.Add(i01);
                        }
                    }

                    // Collider triangles for quad (ix, iy) -> (ix+1, iy+1)
                    if (ix + 1 < GridResolution && iy + 1 < GridResolution)
                    {
                        int i10 = i00 + 1;
                        int i01 = i00 + GridResolution;
                        int i11 = i01 + 1;

                        if (_validVertices[i10] && _validVertices[i01] && _validVertices[i11])
                        {
                            _triangleIndices.Add(i00);
                            _triangleIndices.Add(i01);
                            _triangleIndices.Add(i10);

                            _triangleIndices.Add(i10);
                            _triangleIndices.Add(i01);
                            _triangleIndices.Add(i11);
                        }
                    }
                }
            }

            _wireframeMesh.Clear();
            _wireframeMesh.vertices = _animatedVertices;
            _wireframeMesh.SetIndices(_lineIndices, MeshTopology.Lines, 0);
            _wireframeMesh.RecalculateBounds();

            if (animateAppearance)
            {
                _animElapsed = 0.0f;
                _isAnimating = true;
            }
            else
            {
                _animElapsed = AppearAnimationDuration;
                _isAnimating = false;
                UpdateColliderMesh();
                OnSurfaceAppearFinished?.Invoke();
            }
        }

        public void ClearSurface()
        {
            CurrentFunction = null;
            _isAnimating = false;
            if (_wireframeMesh != null) _wireframeMesh.Clear();
            if (_colliderMesh != null) _colliderMesh.Clear();
        }

        private void Update()
        {
            if (!_isAnimating)
                return;

            _animElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_animElapsed / AppearAnimationDuration);

            // RF-06: Animación de aparición: z(t) = t * f(x, y), con t de 0 a 1 en 1.5 s
            int totalPoints = GridResolution * GridResolution;
            for (int i = 0; i < totalPoints; i++)
            {
                Vector3 v = _baseVertices[i];
                v.y = t * _targetMathZ[i];
                _animatedVertices[i] = v;
            }

            _wireframeMesh.vertices = _animatedVertices;
            _wireframeMesh.RecalculateBounds();

            if (t >= 1.0f)
            {
                _isAnimating = false;
                UpdateColliderMesh();
                OnSurfaceAppearFinished?.Invoke();
            }
        }

        private void UpdateColliderMesh()
        {
            if (_colliderMesh == null) return;

            _colliderMesh.Clear();
            _colliderMesh.vertices = _animatedVertices;
            _colliderMesh.SetTriangles(_triangleIndices, 0);
            _colliderMesh.RecalculateNormals();
            _colliderMesh.RecalculateBounds();

            if (_raycastCollider != null)
            {
                _raycastCollider.sharedMesh = null;
                _raycastCollider.sharedMesh = _colliderMesh;
            }
        }
    }
}
