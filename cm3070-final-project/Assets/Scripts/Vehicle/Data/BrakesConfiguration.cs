using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
    [CreateAssetMenu(fileName = "BrakesConfiguration", menuName = "Vehicle Simulator/BrakesConfiguration")]
    public class BrakesConfiguration : ScriptableObject
    {
        [SerializeField] private float brakeTorqueInNewtonMeters = 3000f;
        [SerializeField] private float fontBrakeBias = 0.7f;
        [SerializeField] private bool enableABS = false;
        [SerializeField] private float aBSSlipThreshold = 0.15f;
    }
}
