using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
[CreateAssetMenu(fileName = "DriveTrain", menuName = "Vehicle Simulator/DriveTrain")]
    public class DriveTrain : ScriptableObject
    {
        public float Damping => damping;
        public float Loss => loss;
        public float Rigidity => damping * 100f * loss;
        [Tooltip("The gears and their ratios \nin this drivetrain.")]        
        [SerializeField] private List<GearRatio> gearRatios;
        [Tooltip("The ratio of lost force between the engine \nand the axle. Should be 1 for EVs.")]        
        [SerializeField] private float loss = 0.85f;
        [Tooltip("The final ratio applied by the drivetrain \nthrough the open differential.")]        
        [SerializeField] private float finalDriveRatio = 4.31f;
        [Tooltip("Dmmping applied by the drivetrain that slows \nwheels turning and affects the rigidity \nand force transfered from the engine")]        
        [SerializeField] private float damping = 2.5f;

        public float GetRatioForGear(Gear gear)
        {
            GearRatio gearRatio = gearRatios.Find(ratio => ratio.Gear == gear);
            if (gearRatio != null)
            {
                return gearRatio.Ratio * finalDriveRatio;                
            }
            else
            {
                return finalDriveRatio;
            }
        }

        public bool ContainsGear(int gear)
        {
            return gearRatios.Any((ratio) => ratio.Gear == (Gear)gear);
        }
    }

    [System.Serializable]
    public class GearRatio
    {
        public Gear Gear;
        public float Ratio;
    }
}
