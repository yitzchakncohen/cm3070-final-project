using System.Collections.Generic;
using ModularVehicleSimulator.Physics;
using ModularVehicleSimulator.Vehicle;
using UnityEngine;

namespace ModularVehicleSimulator.Debugging
{
    [RequireComponent(typeof(AirResistance))]
    public class AirResistanceGizmo : DebuggingTool
    {
        private const float VERTEX_RADIUS = 0.05f;
        private const float FORCE_SCALING = 1f;
        private VehicleController vehicleController;
        private AirResistance airResistance;

        private void Start()
        {
            vehicleController = GetComponent<VehicleController>();
            airResistance = GetComponent<AirResistance>();
        }

        private void OnDrawGizmos()
        {
            if(!isDebuggingEnabled) return;
            if(airResistance == null) return;

            List<Vector2> crossSection = airResistance.CrossSection;
            if(crossSection.Count < 3) return;
            if(vehicleController.ChassisRigidBody.linearVelocity.sqrMagnitude < 0.01f) return;

            Vector3 direction = vehicleController.ChassisRigidBody.linearVelocity.normalized;
            Vector3 center = vehicleController.ChassisRigidBody.worldCenterOfMass;

            (Vector3 u, Vector3 v) = VehiclePhysics.Get2DBasisPlane(direction);

            Gizmos.color = debugColor;

            for (int i = 0; i < crossSection.Count; i++)
            {
                Vector2 p1 = crossSection[i];
                Vector2 p2 = crossSection[(i + 1) % crossSection.Count]; // Loop around to first vertex

                // Un-project 2D points back into 3D world space
                Vector3 worldP1 = center + (u * p1.x) + (v * p1.y);
                Vector3 worldP2 = center + (u * p2.x) + (v * p2.y);

                Gizmos.DrawLine(worldP1, worldP2);
                Gizmos.DrawSphere(worldP1, VERTEX_RADIUS);
            }
        }

        public override Dictionary<string, string> GetDebugValues()
        {
            Dictionary<string, string> debugValues = new Dictionary<string, string>
            {
                { "Drag Area", $"{airResistance.CrossSectionArea} [m^2]" },
                { "Drag Force", $"{airResistance.Drag} [N]" },
                { "Lift Area", $"{airResistance.TopDownArea} [N]" },
                { "Lift Force", $"{airResistance.Lift} [N]" }
            };
            return debugValues;
        }
    }
}
