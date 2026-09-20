using System.Collections.Generic;
using ModularVehicleSimulator.Vehicle;
using UnityEngine;


namespace ModularVehicleSimulator.Debugging
{
    public class WheelGroundGizmo : DebuggingTool
    {
        private Wheel[] wheels = null;

        private void Start()
        {
            GetWheelTransforms();            
        }

        private void OnDrawGizmos()
        {
            if(!isDebuggingEnabled) return;
            if(wheels == null) return;

            foreach (Wheel wheel in wheels)
            {
                Vector3[] rayOrigins = wheel.GetRayOrigins(0f);
                float maxDistance = wheel.SuspensionDistance;

                for (int i = 0; i < rayOrigins.Length; i++)
                {
                    Vector3 start = rayOrigins[i];
                    Vector3 direction = -transform.up;

                    if (wheel.IsGrounded)
                    {
                        debugColor = Color.greenYellow; 
                        Gizmos.color = Color.greenYellow;
                        Gizmos.DrawLine(start, start + direction * wheel.LastGroundHit.distance);
                        Gizmos.DrawSphere(wheel.LastGroundHit.point, 0.02f);
                    }
                    else
                    {
                        debugColor = Color.red; 
                        Gizmos.color = Color.red;
                        Gizmos.DrawRay(start, direction * maxDistance);
                    }
                }                
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
                debugValues.Add($"{forward}, {side} is Grounded", $"{wheel.IsGrounded}");
            }
            return debugValues;
        }
    }
}