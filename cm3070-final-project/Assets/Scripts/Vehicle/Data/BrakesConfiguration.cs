using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
    [CreateAssetMenu(fileName = "BrakesConfiguration", menuName = "Vehicle Simulator/BrakesConfiguration")]
    public class BrakesConfiguration : ScriptableObject
    {
        public float Torque => brakeTorqueInNewtonMeters;
        public float FrontBias => fontBrakeBias;
        public bool ABSEnabled => enableABS;
        public float ABSSlipThreshhold => aBSSlipThreshold;
        public float ABSOscillationSpeed => aBSOscillationSpeed;
        [SerializeField] private float brakeTorqueInNewtonMeters = 3000f;
        [SerializeField] private float fontBrakeBias = 0.7f;
        [SerializeField] private bool enableABS = false;
        [SerializeField] private float aBSSlipThreshold = 0.4f;
        [SerializeField] private float aBSOscillationSpeed = 20f;
    }
}
