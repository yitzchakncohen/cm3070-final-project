using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
    [CreateAssetMenu(fileName = "SteeringConfiguration", menuName = "Vehicle Simulator/SteeringConfiguration")]
    public class SteeringConfiguration : ScriptableObject
    {
        public float MaxAngleAtRest => maxSteerAngleAtRest;
        [SerializeField] private float maxSteerAngleAtRest = 38f;
        [SerializeField] private float maxSteerAngleAtHighSpeed = 8f;
        [SerializeField] private float highSpeedThresholdInMetersPerSecond = 30f;
        [SerializeField] private float steeringSpeedInDegreesPerSecond = 90f;
    }
}
