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
        public float SteeringAssistMin  => steeringAssistMin;
        public float SteeringAssistMax => steeringAssistMax;
        public float FrontStiffness => antiRollBarStiffnessFront;
        public float RearStiffness => antiRollBarStiffnessRear;
        [Tooltip("The maximum steering angle of \nthe wheels at rest.")]
        [SerializeField] private float maxSteeringAngleAtRest = 38.6f;
        [Tooltip("The maximum steering angle of \nthe wheels at maximum speed.")]
        [SerializeField] private float maxSteeringAngleAtHighSpeed = 38.6f;
        [Tooltip("The maximum speed threshold \naffecting turning angle.")]
        [SerializeField] private float highSpeedThresholdInMetersPerSecond = 30f;
        [Tooltip("The nominal speed the wheel can turn.")]
        [SerializeField] private float steeringSpeedInDegreesPerSecond = 90f;
        [Tooltip("The minimum speed multiplier applied \nby the power steering system.")]
        [SerializeField] private float steeringAssistMin = 0.75f;
        [Tooltip("The maximum speed multiplier applied \nby the power steering system.")]
        [SerializeField] private float steeringAssistMax= 1.25f;
        [Tooltip("The stiffness of the anti roll bar on the front axle. \nHigher stiffness values increase understeer \nbut prevent the vehicle from rolling.")]
        [SerializeField] private float antiRollBarStiffnessFront = 12000f;
        [Tooltip("The stiffness of the anti roll bar on the rear axle. \nHigher stiffness values prevent the vehicle from rolling. \nThe ratio compared to the front \naxle stiffness affects steering.")]
        [SerializeField] private float antiRollBarStiffnessRear = 8000f;
    }
}
