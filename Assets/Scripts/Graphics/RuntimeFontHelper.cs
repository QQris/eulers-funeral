using TMPro;
using UnityEngine;

namespace EulersFuneral.Graphics
{
    /// <summary>
    /// Garantiza que todos los componentes TextMeshPro y TextMeshProUGUI tengan una fuente válida
    /// incluso si aún no se ha ejecutado 'Window > TextMeshPro > Import TMP Essential Resources'.
    /// </summary>
    public static class RuntimeFontHelper
    {
        private static TMP_FontAsset _fallbackFontAsset;

        public static TMP_FontAsset GetFontAsset()
        {
            // Si el usuario ya importó TMP Essential Resources, usar la fuente predeterminada de TMP
            if (TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null)
            {
                return TMP_Settings.defaultFontAsset;
            }

            if (_fallbackFontAsset != null)
            {
                return _fallbackFontAsset;
            }

            // Crear dinámicamente un TMP_FontAsset a partir de la fuente integrada de Unity
            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (builtinFont == null)
            {
                builtinFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            if (builtinFont == null)
            {
                string[] osFonts = Font.GetOSInstalledFontNames();
                if (osFonts != null && osFonts.Length > 0)
                {
                    builtinFont = Font.CreateDynamicFontFromOSFont(osFonts[0], 32);
                }
            }

            if (builtinFont != null)
            {
                _fallbackFontAsset = TMP_FontAsset.CreateFontAsset(builtinFont);
                _fallbackFontAsset.name = "RuntimeFallback_TMP_Font";
            }

            return _fallbackFontAsset;
        }

        public static void EnsureFont(TMP_Text textComponent)
        {
            if (textComponent == null) return;
            if (textComponent.font == null)
            {
                textComponent.font = GetFontAsset();
            }
        }
    }
}
