using UnityEngine;

namespace EulersFuneral.Core.Data
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "EulersFuneral/LevelData")]
    public class LevelData : ScriptableObject
    {
        public const int TotalLevels = 6; // Niveles 0 a 5

        [Header("Identificación")]
        public int LevelIndex;
        public string LevelName;
        [TextArea(2, 4)]
        public string Description;
        public bool IsTutorial;

        [Header("Animación de Título con Video Manim (RF-04)")]
        public UnityEngine.Video.VideoClip TitleVideoClip;

        [Header("Dominio Matemático")]
        public float MinX = -5f;
        public float MaxX = 5f;
        public float MinY = -5f;
        public float MaxY = 5f;

        [Header("Puntos de Simulación (RF-08, RF-09)")]
        public Vector3 StartPointA;
        public Vector3[] StarPositions = new Vector3[3];
        public float BallRadius = 0.25f;
        public float StarRadius = 0.20f;

        [Header("Física y Restricciones (RF-11, RF-13, RF-14)")]
        public float Gravity = 9.8f;
        public float Restitution = 0.0f;
        public float MaxTimeSeconds = 20.0f;
        public bool HasCeiling = false;
        public float CeilingHeight = 4.0f;
        public string CeilingFormula = "";

        [Header("Pista Conceptual (RF-07: Nunca revela la fórmula)")]
        [TextArea(2, 3)]
        public string ConceptualHint;
    }
}
