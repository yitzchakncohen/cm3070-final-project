using System.Collections.Generic;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
[CreateAssetMenu(fileName = "DriveTrain", menuName = "Scriptable Objects/DriveTrain")]
    public class DriveTrain : ScriptableObject
    {
        [SerializeField] private List<GearRatio> gearRatios;
        [SerializeField] private float loss = 0.85f;
        [SerializeField] private float finalDriveRatio = 4.31f;

        public float GetRatioForGear(Gear gear)
        {
            GearRatio gearRatio = gearRatios.Find(ratio => ratio.Gear == gear);
            if (gearRatio != null)
            {
                return gearRatio.Ratio * finalDriveRatio * loss;                
            }
            else
            {
                return finalDriveRatio * loss;
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
