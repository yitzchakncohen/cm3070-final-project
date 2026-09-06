using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
    [CreateAssetMenu(fileName = "BrakesConfiguration", menuName = "Vehicle Simulator/BrakesConfiguration")]
    public class BrakesConfiguration : ScriptableObject
    {
        public float Torque => brakeTorqueInNewtonMeters;
        public float RegenerativeBrakeTorque => regenerativeBrakeTorqueInNewtonMeters;
        public float FrontBias => fontBrakeBias;
        public bool ABSEnabled => enableABS;
        public bool RegenerativeBrakingEnabled => enableRegenerativeBraking;
        public float ABSSlipThreshholdMultiplier => aBSSlipThresholdMultiplier;
        public float ABSOscillationSpeed => aBSOscillationSpeed;
        public bool IsBrakeTorqueVectoringEnabled => enableBrakeTorqueVectoring;
        public float VectoringBrakeTorque => vectoringBrakeTorqueInNewtonMeters;
        public float UndersteerThreshold => understeerThresholdInDegreesPerSecond;
        public float UndersteerSpeedMin => understeerSpeedMinInMetersPerSecond;
        public float UndersteerSpeedMax => understeerSpeedMaxInMetersPerSecond;

        [SerializeField] private float brakeTorqueInNewtonMeters = 3000f;
        [SerializeField] private float regenerativeBrakeTorqueInNewtonMeters = 600f;
        [SerializeField] private float fontBrakeBias = 0.7f;
        [SerializeField] private bool enableRegenerativeBraking = true;
        [Header("ABS")]
        [SerializeField] private bool enableABS = true;
        [SerializeField] private float aBSSlipThresholdMultiplier = 1.1f;
        [SerializeField] private float aBSOscillationSpeed = 20f;
        [Header("Brake Torque Vectoring")]
        [SerializeField] private bool enableBrakeTorqueVectoring = true;
        [SerializeField] private float vectoringBrakeTorqueInNewtonMeters = 250f;
        [SerializeField] private float understeerThresholdInDegreesPerSecond = 4.5f;
        [SerializeField] private float understeerSpeedMinInMetersPerSecond = 5f;
        [SerializeField] private float understeerSpeedMaxInMetersPerSecond = 5f;
        
    }
}
