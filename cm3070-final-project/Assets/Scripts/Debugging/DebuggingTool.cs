using System.Collections.Generic;
using UnityEngine;

namespace ModularVehicleSimulator.Debugging
{
    public abstract class DebuggingTool : MonoBehaviour
    {
        public Color DebugColor => debugColor;
        public bool IsEnable => isDebuggingEnabled;
        public DebuggingMode Mode => debuggingMode;
        [SerializeField] protected Color debugColor;
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

        public abstract Dictionary<string, string> GetDebugValues();
    }
}