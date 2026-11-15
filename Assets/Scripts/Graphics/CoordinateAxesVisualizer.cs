using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace EulersFuneral.Graphics
{
    /// <summary>
    /// RF-05: Mesa de graficación 3D con plano de referencia, ejes x, y, z, cuadrícula y etiquetas.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class CoordinateAxesVisualizer : MonoBehaviour
    {
        [Header("Dimensiones del Plano de Referencia")]
        public float MinX = -5f;
        public float MaxX = 5f;
        public float MinY = -5f;
        public float MaxY = 5f;
        public float MinZ = -3f;
        public float MaxZ = 5f;
        public float GridStep = 1.0f;

        [Header("Colores Minimalistas")]
        public Color GridColor = new Color(0.22f, 0.22f, 0.22f, 1.0f);
        public Color AxisColor = new Color(0.55f, 0.55f, 0.55f, 1.0f);
        public Color LabelColor = new Color(0.82f, 0.82f, 0.82f, 1.0f);

        private Mesh _gridMesh;
        private readonly List<GameObject> _labels = new List<GameObject>();

        private void Awake()
        {
            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = Color.black; // RF-05: Fondo negro
            }

            RebuildTable(MinX, MaxX, MinY, MaxY);
        }

        public void RebuildTable(float minX, float maxX, float minY, float maxY)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;

            BuildGridAndAxesMesh();
            BuildLabels();
        }

        private void BuildGridAndAxesMesh()
        {
            var mf = GetComponent<MeshFilter>();
            var mr = GetComponent<MeshRenderer>();

            if (_gridMesh == null)
            {
                _gridMesh = new Mesh { name = "GraphingTableGrid" };
                mf.sharedMesh = _gridMesh;
            }

            if (mr.sharedMaterial == null)
            {
                mr.sharedMaterial = MaterialHelper.CreateUnlitColorMaterial(Color.white, useVertexColors: true);
            }

            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var indices = new List<int>();

            void AddLine(Vector3 a, Vector3 b, Color c)
            {
                int idx = verts.Count;
                verts.Add(a);
                verts.Add(b);
                colors.Add(c);
                colors.Add(c);
                indices.Add(idx);
                indices.Add(idx + 1);
            }

            // Reference plane grid on z = 0
            for (float x = Mathf.Ceil(MinX); x <= Mathf.Floor(MaxX); x += GridStep)
            {
                bool isMainAxis = Mathf.Abs(x) < 1e-4f;
                Color col = isMainAxis ? AxisColor : GridColor;
                AddLine(MathCoords.ToUnity(x, MinY, 0f), MathCoords.ToUnity(x, MaxY, 0f), col);
            }

            for (float y = Mathf.Ceil(MinY); y <= Mathf.Floor(MaxY); y += GridStep)
            {
                bool isMainAxis = Mathf.Abs(y) < 1e-4f;
                Color col = isMainAxis ? AxisColor : GridColor;
                AddLine(MathCoords.ToUnity(MinX, y, 0f), MathCoords.ToUnity(MaxX, y, 0f), col);
            }

            // Vertical Z axis at (0, 0, z)
            AddLine(MathCoords.ToUnity(0f, 0f, MinZ), MathCoords.ToUnity(0f, 0f, MaxZ), AxisColor);

            // Z axis ticks
            for (float z = Mathf.Ceil(MinZ); z <= Mathf.Floor(MaxZ); z += GridStep)
            {
                if (Mathf.Abs(z) < 1e-4f) continue;
                AddLine(MathCoords.ToUnity(-0.12f, 0f, z), MathCoords.ToUnity(0.12f, 0f, z), AxisColor);
            }

            _gridMesh.Clear();
            _gridMesh.SetVertices(verts);
            _gridMesh.SetColors(colors);
            _gridMesh.SetIndices(indices, MeshTopology.Lines, 0);
            _gridMesh.RecalculateBounds();
        }

        private void BuildLabels()
        {
            _labels.Clear();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name.StartsWith("AxisLabel_"))
                {
                    if (Application.isPlaying)
                        Destroy(child.gameObject);
                    else
                        DestroyImmediate(child.gameObject);
                }
            }

            CreateAxisLabel("x", MathCoords.ToUnity(MaxX + 0.45f, 0f, 0f), 3.5f);
            CreateAxisLabel("y", MathCoords.ToUnity(0f, MaxY + 0.45f, 0f), 3.5f);
            CreateAxisLabel("z", MathCoords.ToUnity(0f, 0f, MaxZ + 0.45f), 3.5f);

            for (int x = Mathf.CeilToInt(MinX); x <= Mathf.FloorToInt(MaxX); x += 2)
            {
                if (x == 0) continue;
                CreateAxisLabel(x.ToString(), MathCoords.ToUnity(x, -0.35f, 0f), 2.2f);
            }

            for (int y = Mathf.CeilToInt(MinY); y <= Mathf.FloorToInt(MaxY); y += 2)
            {
                if (y == 0) continue;
                CreateAxisLabel(y.ToString(), MathCoords.ToUnity(-0.35f, y, 0f), 2.2f);
            }

            for (int z = Mathf.CeilToInt(MinZ); z <= Mathf.FloorToInt(MaxZ); z += 2)
            {
                if (z == 0) continue;
                CreateAxisLabel(z.ToString(), MathCoords.ToUnity(0.35f, 0f, z), 2.2f);
            }
        }

        private void CreateAxisLabel(string text, Vector3 worldPos, float fontSize)
        {
            var go = new GameObject($"AxisLabel_{text}");
            go.transform.SetParent(transform, false);
            go.transform.position = worldPos;

            var tmp = go.AddComponent<TextMeshPro>();
            RuntimeFontHelper.EnsureFont(tmp);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = LabelColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Italic;

            _labels.Add(go);
        }
    }
}
