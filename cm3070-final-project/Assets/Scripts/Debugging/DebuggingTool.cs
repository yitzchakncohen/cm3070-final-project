using UnityEngine;

namespace ModularVehicleSimulator.Debugging
{
    public class DebuggingTool : MonoBehaviour
    {
        public DebuggingMode Mode => debuggingMode;
        protected bool isDebuggingEnabled = true;
        [SerializeField] private DebuggingMode debuggingMode = DebuggingMode.None;

        public void Enable()
        {
            isDebuggingEnabled = true;
        }

        public void Disable()
        {
            isDebuggingEnabled = false;
        }
    }
}