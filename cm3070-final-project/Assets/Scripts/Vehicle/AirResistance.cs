using System.Collections.Generic;
using ModularVehicleSimulator.Physics;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    [RequireComponent(typeof(VehicleController))]
    public class AirResistance : MonoBehaviour
    {
        public List<Vector2> CrossSection => crossSection;
        private const float AIR_DENSITY = 1.229f; // kg/m^3
        private VehicleController vehicleController;
        private ChassisConfiguration chassisConfiguration;
        private Collider[] colliders;
        private Vector3 velocity = Vector3.zero;
        List<Vector2> crossSection;

        private void Start()
        {
            vehicleController = GetComponent<VehicleController>();
            chassisConfiguration = vehicleController.Chassis;
            colliders = GetComponentsInChildren<Collider>();
            crossSection = VehiclePhysics.GetCollidersCrossSectionPolygon(colliders, velocity.normalized);
        }

        private void FixedUpdate()
        {
            // TODO Add Wind Velocity
            velocity = vehicleController.ChassisRigidBody.linearVelocity;

            if (velocity.sqrMagnitude > 0.01f)
            {
                List<Vector2> crossSection = VehiclePhysics.GetCollidersCrossSectionPolygon(colliders, velocity.normalized);
                float area = VehiclePhysics.GetAreaOfConvexHull(crossSection);
                // D = Cd * r * V^2/2 * A
                float drag = chassisConfiguration.DragCoefficient * AIR_DENSITY * (velocity.sqrMagnitude / 2f) * area;
                vehicleController.ChassisRigidBody.AddForce(-drag * velocity.normalized);                
            }
        }
    }    
}
