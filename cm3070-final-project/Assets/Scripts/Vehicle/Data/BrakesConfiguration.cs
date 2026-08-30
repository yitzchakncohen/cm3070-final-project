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
        public float ABSSlipThreshholdMultiplier => aBSSlipThresholdMultiplier;
        public float ABSOscillationSpeed => aBSOscillationSpeed;
        [SerializeField] private float brakeTorqueInNewtonMeters = 3000f;
        [SerializeField] private float regenerativeBrakeTorqueInNewtonMeters = 600f;
        [SerializeField] private float fontBrakeBias = 0.7f;
        [SerializeField] private bool enableABS = false;
        [SerializeField] private float aBSSlipThresholdMultiplier = 1.1f;
        [SerializeField] private float aBSOscillationSpeed = 20f;
    }
}
