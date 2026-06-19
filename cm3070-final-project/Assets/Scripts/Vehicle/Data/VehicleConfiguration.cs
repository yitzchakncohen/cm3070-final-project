using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
    [CreateAssetMenu(fileName = "VehicleConfiguration", menuName = "Vehicle Simulator/VehicleConfiguration")]
    public class VehicleConfiguration : ScriptableObject
    {
        public string Name => vehicleName;
        public DriveTrain DriveTrain => driveTrain;
        public WheelConfiguration Wheels => wheelConfiguration;
        public SuspensionConfiguration Suspension => suspensionConfiguration;
        public ChassisConfiguration Chassis => chassisConfiguration;
        public BrakesConfiguration Brakes => brakesConfiguration;
        public EngineConfiguration Engine => engineConfiguration;
        public SteeringConfiguration Steering => steeringConfiguration;
        [SerializeField] private string vehicleName;
        [Header("Engine")]
        [SerializeField] private EngineConfiguration engineConfiguration;
        [SerializeField] private DriveTrain driveTrain;
        [Header("Wheels")]
        [SerializeField] private WheelConfiguration wheelConfiguration;
        [SerializeField] private SuspensionConfiguration suspensionConfiguration;
        [SerializeField] private BrakesConfiguration brakesConfiguration;
        [Header("Chassis")]
        [SerializeField] private ChassisConfiguration chassisConfiguration;
        [SerializeField] private SteeringConfiguration steeringConfiguration;
    }    
}
