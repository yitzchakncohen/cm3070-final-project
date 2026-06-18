using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
    [CreateAssetMenu(fileName = "VehicleConfiguration", menuName = "Scriptable Objects/VehicleConfiguration")]
    public class VehicleConfiguration : ScriptableObject
    {
        public EngineType EngineType => EngineType.Gas;
        public string Name => vehicleName;
        public DriveTrain DriveTrain => driveTrain;
        [SerializeField] private string vehicleName;
        [SerializeField] private EngineType engineType;
        [SerializeField] private DriveTrain driveTrain;
    }    
}
