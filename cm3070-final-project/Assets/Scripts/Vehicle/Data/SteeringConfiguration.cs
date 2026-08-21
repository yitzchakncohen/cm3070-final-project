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
        public float FrontStiffness => antiRollBarStiffnessFront;
        public float RearStiffness => antiRollBarStiffnessRear;
        [SerializeField] private float maxSteeringAngleAtRest = 38f;
        [SerializeField] private float maxSteeringAngleAtHighSpeed = 38f;
        [SerializeField] private float highSpeedThresholdInMetersPerSecond = 30f;
        [SerializeField] private float steeringSpeedInDegreesPerSecond = 90f;
        [SerializeField] private float antiRollBarStiffnessFront = 12000f;
        [SerializeField] private float antiRollBarStiffnessRear = 8000f;
    }
}
