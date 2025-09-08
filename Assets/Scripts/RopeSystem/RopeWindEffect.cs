using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RopeSystem
{
    /// <summary>
    /// WindowEffect Component
    /// Written by GoGoGaGa
    /// </summary>
    public class RopeWindEffect : MonoBehaviour
    {
        [Header("Wind Settings")]
        [Tooltip("Set wind direction perpendicular to the rope based on the start and end points")]
        public bool perpendicularWind;

        [Tooltip("Flip the direction of the wind")]
        public bool flipWindDirection;

        [Tooltip("Direction of the wind force in degrees")] [Range(-360f, 360f)]
        public float windDirectionDegrees;

        private Vector3 windDirection;

        [Tooltip("Magnitude of the wind force")] [Range(0.0f, 500f)]
        public float windForce;

        private float appliedWindForce;
        private float windSeed;
        private Rope rope;

        private void Awake() => rope = GetComponent<Rope>();

        private void Start() => windSeed = Random.Range(-0.3f, 0.3f);

        private void Update() => GenerateWind();

        private void FixedUpdate() => SimulatePhysics();

        private void GenerateWind()
        {
            if (perpendicularWind)
            {
                windDirection = Vector3
                    .Cross(rope.EndPoint.position - rope.StartPoint.position, Vector3.up).normalized;
                var num1 = (float)(Mathf.PerlinNoise(Time.time + windSeed, 0.0f) * 20.0 - 10.0);
                var num2 = Vector3.SignedAngle(Vector3.forward, windDirection, Vector3.up);
                var f = (float)((num2 + (double)num1) * (Math.PI / 180.0));
                windDirection = new Vector3(Mathf.Sin(f), 0.0f, Mathf.Cos(f)).normalized;
                windDirectionDegrees = num2;
            }
            else
            {
                var f = (float)((windDirectionDegrees +
                                 (Mathf.PerlinNoise(Time.time + windSeed, 0.0f) * 20.0 - 10.0)) *
                                (Math.PI / 180.0));
                windDirection = new Vector3(Mathf.Sin(f), 0.0f, Mathf.Cos(f)).normalized;
            }

            var num = Mathf.PerlinNoise(Time.time + windSeed, 0.0f) * Mathf.PerlinNoise(0.5f * Time.time, 0.0f);
            if (flipWindDirection)
                appliedWindForce = (float)(windForce * -1.0 * 5.0) * num;
            else
                appliedWindForce = windForce * 5f * num;
        }

        private void SimulatePhysics()
        {
            rope.LeftItem = windDirection.normalized * (appliedWindForce * Time.fixedDeltaTime);
        }
    }
}