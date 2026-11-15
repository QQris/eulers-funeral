using System;
using UnityEngine;

namespace EulersFuneral.Core.Data
{
    public static class LevelDefinitions
    {
        public const int TotalLevels = 6; // Niveles 0 a 5

        public static LevelData[] CreateAllLevels()
        {
            var levels = new LevelData[TotalLevels];
            levels[0] = CreateLevel0_TutorialSlope();
            levels[1] = CreateLevel1_Paraboloid();
            levels[2] = CreateLevel2_Floor();
            levels[3] = CreateLevel3_Ceiling();
            levels[4] = CreateLevel4_Sinusoidal();
            levels[5] = CreateLevel5_EulerExponential();
            return levels;
        }

        private static LevelData CreateLevel0_TutorialSlope()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.LevelIndex = 0;
            level.LevelName = "Nivel 0: Identidad Diagonal";
            level.Description = "Tutorial: Construye un plano inclinado en X e Y para que la pelota recoja las 3 estrellas rojas en diagonal.";
            level.IsTutorial = true;

            // RF-04: e^(iπ) + 1 = 0, luego + 0 -> 0
            level.TitleSequence = new[]
            {
                new LevelTitleStep("e^{i\\pi} + 1 = 0", 1.2f),
                new LevelTitleStep("+ 0", 0.9f),
                new LevelTitleStep("0", 0.9f)
            };
            level.FinalDisplayedSymbol = "0";

            level.MinX = -5f;
            level.MaxX = 5f;
            level.MinY = -5f;
            level.MaxY = 5f;

            // Solución de referencia bivariada: -0.2*x - 0.2*y
            level.StartPointA = new Vector3(-2.5f, -2.5f, 1.0f);
            level.StarPositions = new[]
            {
                new Vector3(-1.6666f, -1.6666f, 0.6667f),
                new Vector3(-0.3433f, -0.3433f, 0.1373f),
                new Vector3(1.2824f, 1.2824f, -0.5130f)
            };
            level.ObjectivePointB = level.StarPositions[2];

            level.BallRadius = 0.25f;
            level.StarRadius = 0.22f;
            level.Gravity = 9.8f;
            level.Restitution = 0.0f;
            level.MaxTimeSeconds = 20.0f;
            level.HasCeiling = false;
            level.ConceptualHint = "Las 3 estrellas avanzan en diagonal (y = x). Necesitas un plano con pendiente negativa tanto en x como en y (-ax - ay).";
            return level;
        }

        private static LevelData CreateLevel1_Paraboloid()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.LevelIndex = 1;
            level.LevelName = "Nivel 1: Valle Parabólico";
            level.Description = "Guía la partícula con un canal cuadrático en Y inclinado en X para recoger las 3 estrellas.";
            level.IsTutorial = false;

            // RF-04: e -> e (símbolo)
            level.TitleSequence = new[]
            {
                new LevelTitleStep("e", 1.5f),
                new LevelTitleStep("e", 1.5f)
            };
            level.FinalDisplayedSymbol = "e";

            level.MinX = -5f;
            level.MaxX = 5f;
            level.MinY = -5f;
            level.MaxY = 5f;

            // Solución de referencia bivariada: 0.2*y^2 - 0.25*x
            level.StartPointA = new Vector3(-3.0f, 1.5f, 1.20f);
            level.StarPositions = new[]
            {
                new Vector3(-2.3801f, 0.2619f, 0.6087f),
                new Vector3(-0.8601f, -1.1840f, 0.4954f),
                new Vector3(1.0499f, -1.4756f, 0.1730f)
            };
            level.ObjectivePointB = level.StarPositions[2];

            level.BallRadius = 0.25f;
            level.StarRadius = 0.22f;
            level.Gravity = 9.8f;
            level.Restitution = 0.15f;
            level.MaxTimeSeconds = 20.0f;
            level.HasCeiling = false;
            level.ConceptualHint = "La pelota inicia en y = 1.5 y oscila de un lado a otro del eje Y mientras avanza en X. Combina un término cuadrático en y^2 con una pendiente lineal en x.";
            return level;
        }

        private static LevelData CreateLevel2_Floor()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.LevelIndex = 2;
            level.LevelName = "Nivel 2: Escalones";
            level.Description = "Utiliza la función piso (floor) junto con pendientes en X e Y para descender por las terrazas recolectando las 3 estrellas.";
            level.IsTutorial = false;

            // RF-04 & P1: ⌊e⌋ -> 2
            level.TitleSequence = new[]
            {
                new LevelTitleStep("e", 0.8f),
                new LevelTitleStep("\\lfloor e \\rfloor", 1.1f),
                new LevelTitleStep("2", 1.1f)
            };
            level.FinalDisplayedSymbol = "2";

            level.MinX = -5f;
            level.MaxX = 5f;
            level.MinY = -5f;
            level.MaxY = 5f;

            // Solución de referencia bivariada: 0.25*floor(-0.5*x) - 0.15*x - 0.15*y
            level.StartPointA = new Vector3(-3.0f, -2.5f, 1.075f);
            level.StarPositions = new[]
            {
                new Vector3(-2.2967f, -1.7967f, 0.8640f),
                new Vector3(-0.8846f, -0.3846f, 0.1904f),
                new Vector3(0.8909f, 1.3909f, -0.5889f)
            };
            level.ObjectivePointB = level.StarPositions[2];

            level.BallRadius = 0.25f;
            level.StarRadius = 0.22f;
            level.Gravity = 9.8f;
            level.Restitution = 0.15f;
            level.MaxTimeSeconds = 25.0f;
            level.UsesFloorMechanic = true;
            level.HasCeiling = false;
            level.ConceptualHint = "Combina escalones discretos en X mediante floor(-0.5x) con inclinaciones continuas en ambos ejes (-0.15x - 0.15y).";
            return level;
        }

        private static LevelData CreateLevel3_Ceiling()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.LevelIndex = 3;
            level.LevelName = "Nivel 3: Confinamiento";
            level.Description = "La partícula está confinada bajo un techo superior mientras oscila en el eje Y y desciende con la función ceil.";
            level.IsTutorial = false;

            // RF-04 & P1: ⌈e⌉ -> 3
            level.TitleSequence = new[]
            {
                new LevelTitleStep("e", 0.8f),
                new LevelTitleStep("\\lceil e \\rceil", 1.1f),
                new LevelTitleStep("3", 1.1f)
            };
            level.FinalDisplayedSymbol = "3";

            level.MinX = -5f;
            level.MaxX = 5f;
            level.MinY = -5f;
            level.MaxY = 5f;

            // Solución de referencia bivariada: -0.25*x + 0.2*ceil(-0.4*x) + 0.15*y^2
            level.StartPointA = new Vector3(-3.0f, -1.6f, 1.534f);
            level.StarPositions = new[]
            {
                new Vector3(-2.1675f, -0.2390f, 0.7561f),
                new Vector3(-0.1963f, 1.2263f, 0.4746f),
                new Vector3(2.3054f, 1.5247f, -0.2276f)
            };
            level.ObjectivePointB = level.StarPositions[2];

            level.BallRadius = 0.25f;
            level.StarRadius = 0.22f;
            level.Gravity = 9.8f;
            level.Restitution = 0.20f;
            level.MaxTimeSeconds = 25.0f;
            level.HasCeiling = true;
            level.CeilingHeight = 2.4f;
            level.CeilingFormula = "2.4 - 0.2*x";
            level.ConceptualHint = "Usa ceil(-0.4x) y una pendiente en x junto con una parábola lateral en y^2 para cruzar de y = -1.6 hacia y > 0 sin tocar el techo.";
            return level;
        }

        private static LevelData CreateLevel4_Sinusoidal()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.LevelIndex = 4;
            level.LevelName = "Nivel 4: Armonía Senoidal";
            level.Description = "Modela una onda trigonométrica inclinada en X e Y que recoja las 3 estrellas por conservación de energía.";
            level.IsTutorial = false;

            // RF-04: lim (1 + 4/n)^n = e^4, luego e^(ln 4) -> 4
            level.TitleSequence = new[]
            {
                new LevelTitleStep("\\lim_{n\\to\\infty} (1 + 4/n)^n = e^4", 1.1f),
                new LevelTitleStep("e^{\\ln 4}", 1.0f),
                new LevelTitleStep("4", 0.9f)
            };
            level.FinalDisplayedSymbol = "4";

            level.MinX = -5f;
            level.MaxX = 5f;
            level.MinY = -5f;
            level.MaxY = 5f;

            // Solución de referencia bivariada: 0.5*sen(x) - 0.3*x - 0.2*y
            level.StartPointA = new Vector3(-3.0f, -2.0f, 1.2294f);
            level.StarPositions = new[]
            {
                new Vector3(-1.1339f, -1.4598f, 0.1791f),
                new Vector3(1.2950f, -0.0215f, 0.0969f),
                new Vector3(3.3007f, 1.4041f, -1.3502f)
            };
            level.ObjectivePointB = level.StarPositions[2];

            level.BallRadius = 0.25f;
            level.StarRadius = 0.22f;
            level.Gravity = 9.8f;
            level.Restitution = 0.15f;
            level.MaxTimeSeconds = 25.0f;
            level.HasCeiling = false;
            level.ConceptualHint = "Combina una onda sen(x) con términos lineales negativos tanto en x como en y para desplazar la trayectoria hacia +Y mientras supera la cresta.";
            return level;
        }

        private static LevelData CreateLevel5_EulerExponential()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.LevelIndex = 5;
            level.LevelName = "Nivel 5: Euler's Funeral";
            level.Description = "La superficie exponencial e^x acoplada con una curvatura en Y bajo gravedad incrementada.";
            level.IsTutorial = false;

            // RF-04: e^(iπ) + 1 + 5 -> 5
            level.TitleSequence = new[]
            {
                new LevelTitleStep("e^{i\\pi} + 1 + 5", 1.5f),
                new LevelTitleStep("5", 1.5f)
            };
            level.FinalDisplayedSymbol = "5";

            level.MinX = -5f;
            level.MaxX = 5f;
            level.MinY = -5f;
            level.MaxY = 5f;

            // Solución de referencia bivariada: e^x + 0.2*y^2
            level.StartPointA = new Vector3(1.2f, 1.4f, 3.7121f);
            level.StarPositions = new[]
            {
                new Vector3(0.9362f, 1.3536f, 2.9167f),
                new Vector3(0.0930f, 1.1662f, 1.3695f),
                new Vector3(-1.9488f, 0.3983f, 0.1742f)
            };
            level.ObjectivePointB = level.StarPositions[2];

            level.BallRadius = 0.25f;
            level.StarRadius = 0.22f;
            level.Gravity = 12.5f; // Dispara el aviso de gravedad en HUD durante 2 s (RF-13)
            level.Restitution = 0.10f;
            level.MaxTimeSeconds = 20.0f;
            level.UsesExponentialBase = true;
            level.BaseTerrainFormula = "e^x + 0.2*y^2";
            level.HasCeiling = false;
            level.ConceptualHint = "Combina la función exponencial e^x (que impulsa hacia -X) con un valle parabólico suave en y^2 que dirija la pelota desde y = 1.4 hacia el eje central.";
            return level;
        }
    }
}
