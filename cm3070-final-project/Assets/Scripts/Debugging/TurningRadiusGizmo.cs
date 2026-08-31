using System.Collections.Generic;
using ModularVehicleSimulator.Physics;
using ModularVehicleSimulator.Vehicle;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace ModularVehicleSimulator.Debugging
{
    public class TurningRadiusGizmo : DebuggingTool
    {
        private const float WIRE_SPHERE_RADIUS = 0.3f;
        private const float ARC_ANGLE = 90f;
        private Wheel[] wheels = null;
        private Wheel frontLeft = null;
        private Wheel frontRight = null;
        private Wheel backLeft = null;
        private Wheel backRight = null;
        private VehicleController vehicleController;

        private void Start()
        {
            vehicleController = GetComponentInParent<VehicleController>();
            GetWheelTransforms();            
        }

        private void OnDrawGizmos()
        {
            if(vehicleController == null) return;
            if(!isDebuggingEnabled) return;
            if(wheels == null) return;

#if UNITY_EDITOR
            Handles.color = debugColor;
#endif
            Gizmos.color = debugColor;

            if (frontRight != null && frontLeft != null && backRight != null && backLeft != null)
            {
                Vector3 front = (frontRight.transform.position + frontLeft.transform.position) / 2f;
                Vector3 back = (backRight.transform.position + backLeft.transform.position) / 2f;
                // Track
                Gizmos.DrawLine(frontRight.transform.position, frontLeft.transform.position);
                // Wheel Base
                Gizmos.DrawLine(front, back);
                DrawTurningRadius(back);
            }
        }

        private void DrawTurningRadius(Vector3 back)
        {
            if (Mathf.Abs(vehicleController.CurrentSteeringAngle) > 0f)
            {
                float wheelBase = Vector3.Distance(frontRight.transform.position, backRight.transform.position);
                float turningRadius = VehiclePhysics.GetTurningRadius(wheelBase, vehicleController.CurrentSteeringAngle);
                Vector3 turningRadiusCenter = back + transform.right * turningRadius;

                // Draw Turning Center
                Gizmos.DrawLine(backLeft.transform.position, turningRadiusCenter);
                Gizmos.DrawLine(frontRight.transform.position, turningRadiusCenter);
                Gizmos.DrawLine(frontLeft.transform.position, turningRadiusCenter);
                Gizmos.DrawWireSphere(turningRadiusCenter, WIRE_SPHERE_RADIUS);

                // Draw turning arcs
                foreach (Wheel wheel in wheels)
                {
                    float wheelTurningRadius = Vector3.Distance(wheel.transform.position, turningRadiusCenter);
                    Vector3 direction = (wheel.transform.position - turningRadiusCenter).normalized;

#if UNITY_EDITOR
                    if (vehicleController.CurrentSteeringAngle < 0f)
                    {
                        Handles.DrawWireArc(turningRadiusCenter, -transform.up, direction, ARC_ANGLE, wheelTurningRadius);
                    }
                    else
                    {
                        Handles.DrawWireArc(turningRadiusCenter, -transform.up, direction, -ARC_ANGLE, wheelTurningRadius);
                    }
#endif
                }
            }
        }

        private void GetWheelTransforms()
        {
            if (wheels == null)
            {
                wheels = GetComponentsInChildren<Wheel>();
            }
            foreach (Wheel wheel in wheels)
            {
                if (wheel.IsFront && wheel.IsLeft)
                {
                    frontLeft = wheel;
                }
                else if (wheel.IsFront && wheel.IsRight)
                {
                    frontRight = wheel;
                }
                else if (!wheel.IsFront && wheel.IsLeft)
                {
                    backLeft = wheel;
                }
                else if (!wheel.IsFront && wheel.IsRight)
                {
                    backRight = wheel;
                }
            }
        }

        public override Dictionary<string, string> GetDebugValues()
        {
            float wheelBase = Vector3.Distance(frontRight.transform.position, backRight.transform.position);
            float turningRadius = Mathf.Abs(VehiclePhysics.GetTurningRadius(wheelBase, vehicleController.CurrentSteeringAngle));
            Dictionary<string, string> debugValues = new Dictionary<string, string>
            {
                { "Turning Radius", $"{turningRadius} [m]" },
            };
            return debugValues;
        }
    }    
}
