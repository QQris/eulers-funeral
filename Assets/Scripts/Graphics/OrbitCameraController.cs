using EulersFuneral.Core.Data;
using UnityEngine;

namespace EulersFuneral.Graphics
{
    /// <summary>
    /// RF-05 & P4: Cámara orbital controlada con clic derecho sostenido y zoom con la rueda.
    /// El pitch se restringe entre [5°, 85°] y la altura nunca desciende por debajo del plano de referencia (z >= 0).
    /// </summary>
    public class OrbitCameraController : MonoBehaviour
    {
        public const float MinPitchDegrees = 5.0f;
        public const float MaxPitchDegrees = 85.0f;
        public const float MinDistance = 3.5f;
        public const float MaxDistance = 24.0f;

        [Header("Objetivo y Órbita")]
        public Vector3 PivotTarget = Vector3.zero;
        public float YawDegrees = -45.0f;
        public float PitchDegrees = 32.0f;
        public float Distance = 12.5f;
        public float PlaneMinHeight = 0.05f;

        [Header("Sensibilidad")]
        public float OrbitSpeed = 180.0f;
        public float ZoomSpeed = 4.5f;

        private void Start()
        {
            ApplyCameraTransform();
        }

        private void LateUpdate()
        {
            float sensitivity = SaveSystem.Load().MouseSensitivity;

            // RF-05: Clic derecho sostenido para orbitar
            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");

                YawDegrees += mouseX * OrbitSpeed * sensitivity * Time.unscaledDeltaTime * 5.0f;
                PitchDegrees -= mouseY * OrbitSpeed * sensitivity * Time.unscaledDeltaTime * 5.0f;
            }

            // RF-05: Rueda del ratón para zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 1e-4f)
            {
                Distance -= scroll * ZoomSpeed;
            }

            ApplyCameraTransform();
        }

        public void ApplyCameraTransform()
        {
            Vector3 pos = ComputeOrbitPosition(
                PivotTarget,
                YawDegrees,
                PitchDegrees,
                Distance,
                PlaneMinHeight,
                out PitchDegrees,
                out Distance);

            transform.position = pos;
            transform.LookAt(PivotTarget, Vector3.up);
        }

        public static Vector3 ComputeOrbitPosition(
            Vector3 pivot,
            float yawDeg,
            float pitchDeg,
            float distance,
            float minHeight,
            out float clampedPitch,
            out float clampedDistance)
        {
            clampedPitch = Mathf.Clamp(pitchDeg, MinPitchDegrees, MaxPitchDegrees);
            clampedDistance = Mathf.Clamp(distance, MinDistance, MaxDistance);

            float pitchRad = clampedPitch * Mathf.Deg2Rad;
            float yawRad = yawDeg * Mathf.Deg2Rad;

            float horizDist = clampedDistance * Mathf.Cos(pitchRad);
            float vertHeight = pivot.y + clampedDistance * Mathf.Sin(pitchRad);

            // Garantizar que la cámara nunca baje del plano de referencia (RF-05, Criterio 3)
            if (vertHeight < minHeight)
            {
                vertHeight = minHeight;
                float sinPitch = Mathf.Clamp((vertHeight - pivot.y) / clampedDistance, 0.087f, 0.996f);
                clampedPitch = Mathf.Clamp(Mathf.Asin(sinPitch) * Mathf.Rad2Deg, MinPitchDegrees, MaxPitchDegrees);
                horizDist = clampedDistance * Mathf.Cos(clampedPitch * Mathf.Deg2Rad);
            }

            float x = pivot.x + horizDist * Mathf.Sin(yawRad);
            float z = pivot.z - horizDist * Mathf.Cos(yawRad);

            return new Vector3(x, vertHeight, z);
        }
    }
}
