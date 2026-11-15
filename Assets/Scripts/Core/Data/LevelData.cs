using System;
using UnityEngine;

namespace EulersFuneral.Core.Data
{
    [Serializable]
    public struct LevelTitleStep
    {
        public string ExpressionText;
        public float DurationSeconds;

        public LevelTitleStep(string expressionText, float durationSeconds)
        {
            ExpressionText = expressionText;
            DurationSeconds = durationSeconds;
        }
    }

    [CreateAssetMenu(fileName = "LevelData", menuName = "EulersFuneral/LevelData")]
    public class LevelData : ScriptableObject
    {
        [Header("Identificación")]
        public int LevelIndex;
        public string LevelName;
        [TextArea(2, 4)]
        public string Description;
        public bool IsTutorial;

        [Header("Animación de Título con Video Manim (RF-04)")]
        public UnityEngine.Video.VideoClip TitleVideoClip;
        public LevelTitleStep[] TitleSequence;
        public string FinalDisplayedSymbol;

        [Header("Dominio Matemático")]
        public float MinX = -5f;
        public float MaxX = 5f;
        public float MinY = -5f;
        public float MaxY = 5f;

        [Header("Puntos de Simulación (RF-08, RF-09, RF-10)")]
        public Vector3 StartPointA;
        public Vector3 ObjectivePointB;
        public Vector3[] StarPositions = new Vector3[3];
        public float BallRadius = 0.25f;
        public float StarRadius = 0.20f;

        /// <summary>
        /// RF-10: El objetivo B es una esfera roja de radio r_t = 0.9 * r_bola.
        /// </summary>
        public float ObjectiveRadius => 0.9f * BallRadius;

        [Header("Física y Restricciones (RF-11, RF-13, RF-14)")]
        public float Gravity = 9.8f;
        public float Restitution = 0.0f;
        public float MaxTimeSeconds = 20.0f;
        public bool HasCeiling = false;
        public float CeilingHeight = 4.0f;
        public string CeilingFormula = "";
        public bool UsesFloorMechanic = false;
        public bool UsesExponentialBase = false;
        public string BaseTerrainFormula = "";

        [Header("Pista Conceptual (RF-07: Nunca revela la fórmula)")]
        [TextArea(2, 3)]
        public string ConceptualHint;
    }
}
