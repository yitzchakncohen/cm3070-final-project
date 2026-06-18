using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
    [CreateAssetMenu(fileName = "VehicleConfiguration", menuName = "Vehicle Simulator/VehicleConfiguration")]
    public class VehicleConfiguration : ScriptableObject
    {
        public string Name => vehicleName;
        public EngineType EngineType => EngineType.Gas;
        public DriveTrain DriveTrain => driveTrain;
        public WheelConfiguration Wheels => wheelConfiguration;
        public SuspensionConfiguration Suspension => suspensionConfiguration;
        public ChassisConfiguration Chassis => chassisConfiguration;
        public EngineConfiguration Engine => engineConfiguration;
        [SerializeField] private string vehicleName;
        [Header("Engine")]
        [SerializeField] private EngineConfiguration engineConfiguration;
        [SerializeField] private DriveTrain driveTrain;
        [Header("Wheels")]
        [SerializeField] private WheelConfiguration wheelConfiguration;
        [SerializeField] private SuspensionConfiguration suspensionConfiguration;
        [Header("Chassis")]
        [SerializeField] private ChassisConfiguration chassisConfiguration;
    }    
}
