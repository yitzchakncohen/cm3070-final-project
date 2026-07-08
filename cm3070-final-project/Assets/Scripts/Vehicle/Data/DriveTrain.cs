using System.Collections.Generic;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
[CreateAssetMenu(fileName = "DriveTrain", menuName = "Vehicle Simulator/DriveTrain")]
    public class DriveTrain : ScriptableObject
    {
        public float Damping => damping;
        public float Loss => loss;
        public float Rigidity => damping * 100f * loss;
        [SerializeField] private List<GearRatio> gearRatios;
        [SerializeField] private float loss = 0.85f;
        [SerializeField] private float finalDriveRatio = 4.31f;
        [SerializeField] private float damping = 0.25f;

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
    }

    [System.Serializable]
    public class GearRatio
    {
        public Gear Gear;
        public float Ratio;
    }
}
