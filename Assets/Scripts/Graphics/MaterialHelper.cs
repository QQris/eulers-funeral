using UnityEngine;
using UnityEngine.Rendering;

namespace EulersFuneral.Graphics
{
    /// <summary>
    /// Crea materiales Unlit compatibles tanto con Built-in Render Pipeline como con URP,
    /// evitando que objetos se vean rosados (magenta) cuando m_CustomRenderPipeline no está asignado.
    /// </summary>
    public static class MaterialHelper
    {
        public static Material CreateUnlitColorMaterial(Color color, bool useVertexColors = false)
        {
            Shader shader = null;

            if (useVertexColors)
            {
                shader = Shader.Find("Sprites/Default")
                         ?? Shader.Find("EulersFuneral/WireframeUnlit");
            }
            else
            {
                shader = Shader.Find("EulersFuneral/WireframeUnlit");

                if (shader == null)
                {
                    bool isSrpActive = GraphicsSettings.currentRenderPipeline != null;
                    if (isSrpActive)
                    {
                        shader = Shader.Find("Universal Render Pipeline/Unlit");
                    }
                    if (shader == null)
                    {
                        shader = Shader.Find("Unlit/Color")
                                 ?? Shader.Find("Sprites/Default")
                                 ?? Shader.Find("Standard");
                    }
                }
            }

            var mat = new Material(shader)
            {
                color = color
            };

            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);

            return mat;
        }
    }
}
