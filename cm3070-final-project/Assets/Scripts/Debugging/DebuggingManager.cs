using UnityEngine;

namespace ModularVehicleSimulator.Debugging
{
    public class DebuggingManager : MonoBehaviour
    {
        [SerializeField] private DebuggingMode debuggingModes;

        private void Start()
        {
            UpdateTools();
        }

        [ContextMenu("Update Tools")]
        public void UpdateTools()
        {
            DebuggingTool[] tools = FindObjectsByType<DebuggingTool>(FindObjectsSortMode.None);
            foreach (DebuggingTool tool in tools)
            {
                if (debuggingModes.HasFlag(tool.Mode))
                {
                    tool.Enable();
                }
                else
                {
                    tool.Disable();
                }
            }
        }
    }
    
    [System.Flags]
    public enum DebuggingMode
    {
        None     = 0,     
        CenterOfMass     = 1 << 0,   
        TireFriction    = 1 << 1,   
        TurningRadius    = 1 << 2,
        AirResistance    = 1 << 3,
    }
}
