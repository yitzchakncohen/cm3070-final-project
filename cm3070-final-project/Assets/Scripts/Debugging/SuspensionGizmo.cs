using System.Collections.Generic;
using ModularVehicleSimulator.Vehicle;
using UnityEngine;

namespace ModularVehicleSimulator.Debugging
{
    public class SuspensionGizmo : DebuggingTool
    {
        private const float NEWTON_TO_METER_SCALING = 1000f;
        private Wheel[] wheels = null;

        private void Start()
        {
            GetWheelTransforms();            
        }

        private void OnDrawGizmos()
        {
            if(!isDebuggingEnabled) return;
            if(wheels == null) return;

            Gizmos.color = debugColor;      

            foreach (Wheel wheel in wheels)
            {
                Gizmos.DrawLine(wheel.WheelContactPoint, wheel.WheelContactPoint + (wheel.NormalForce / NEWTON_TO_METER_SCALING));
            }
        }

        private void GetWheelTransforms()
        {
            if (wheels == null)
            {
                wheels = GetComponentsInChildren<Wheel>();
            }
        }

        public override Dictionary<string, string> GetDebugValues()
        {
            Dictionary<string, string> debugValues = new Dictionary<string, string>();
            foreach (Wheel wheel in wheels)
            {
                string forward = wheel.IsFront ? "Front" : "Rear";
                string side = wheel.IsRight ? "Right" : "Left";
                debugValues.Add($"{forward}, {side} Normal Force", $"{wheel.NormalForce} [N]");
            }
            return debugValues;
        }
    }
}
