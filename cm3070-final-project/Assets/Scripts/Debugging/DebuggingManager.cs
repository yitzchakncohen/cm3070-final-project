using UnityEngine;

namespace ModularVehicleSimulator.Debugging
{
    public class DebuggingManager : MonoBehaviour
    {
        public DebuggingTool[] Tools => tools;
        [SerializeField] private DebuggingMode debuggingModes;
        private DebuggingTool[] tools;

        private void Start()
        {
            EnableTools();
        }

        [ContextMenu("Update Tools")]
        public void EnableTools()
        {
            tools = FindObjectsByType<DebuggingTool>(FindObjectsSortMode.None);
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
