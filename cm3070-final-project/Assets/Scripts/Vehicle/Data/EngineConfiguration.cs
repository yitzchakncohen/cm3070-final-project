using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
    [CreateAssetMenu(fileName = "EngineConfiguration", menuName = "Vehicle Simulator/EngineConfiguration")]
    public class EngineConfiguration : ScriptableObject
    {
        public EngineType Type => engineType;
        public float Power => enginePowerInNewtonMeters;
        [SerializeField] private EngineType engineType;
        [SerializeField] private float enginePowerInNewtonMeters = 220f;
    }
}
