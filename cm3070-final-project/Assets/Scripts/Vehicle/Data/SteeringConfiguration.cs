using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
    [CreateAssetMenu(fileName = "SteeringConfiguration", menuName = "Vehicle Simulator/SteeringConfiguration")]
    public class SteeringConfiguration : ScriptableObject
    {
        public float MaxSteeringAngleAtRest => maxSteeringAngleAtRest;
        public float MaxSteeringAngleAtHighSpeed => maxSteeringAngleAtHighSpeed;
        public float HighSpeedThreshold => highSpeedThresholdInMetersPerSecond;
        public float SteeringSpeed => steeringSpeedInDegreesPerSecond;
        [SerializeField] private float maxSteeringAngleAtRest = 38f;
        [SerializeField] private float maxSteeringAngleAtHighSpeed = 8f;
        [SerializeField] private float highSpeedThresholdInMetersPerSecond = 30f;
        [SerializeField] private float steeringSpeedInDegreesPerSecond = 90f;
    }
}
