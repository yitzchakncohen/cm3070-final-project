using System.Collections.Generic;
using ModularVehicleSimulator.Physics;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    [RequireComponent(typeof(VehicleController))]
    public class AirResistance : MonoBehaviour
    {
        public List<Vector2> CrossSection => crossSection;
        public float Drag => drag;
        public float Lift => lift;
        public float CrossSectionArea => crossSectionArea;
        public float TopDownArea => topDownArea;
        private const float AIR_DENSITY = 1.229f; // kg/m^3
        private VehicleController vehicleController;
        private ChassisConfiguration chassisConfiguration;
        private Collider[] colliders;
        private Vector3 velocity = Vector3.zero;
        private List<Vector2> crossSection = new List<Vector2>();
        private List<Vector2> topDownCrossSection = new List<Vector2>();
        private Vector3 frontPosition;
        private Vector3 backPosition;
        private float topDownArea = 0f;
        private float crossSectionArea = 0f;
        private float drag = 0f;
        private float lift = 0f;
        // Only calculate drag or lift on each frame to improve perfomance. 
        private bool isDragCalculatedLastFrame = false;

        private void Start()
        {
            vehicleController = GetComponent<VehicleController>();
            chassisConfiguration = vehicleController.Chassis;
            colliders = GetComponentsInChildren<Collider>();
            VehiclePhysics.GetCollidersCrossSectionPolygon(colliders, velocity.normalized, vehicleController.ChassisRigidBody.transform.up, vehicleController.ChassisRigidBody.worldCenterOfMass, crossSection);
            VehiclePhysics.GetCollidersCrossSectionPolygon(colliders, Vector3.up, velocity.normalized, vehicleController.ChassisRigidBody.worldCenterOfMass, topDownCrossSection);
            frontPosition = vehicleController.ChassisRigidBody.centerOfMass + chassisConfiguration.WheelBase * 0.5f * Vector3.forward;
            backPosition = vehicleController.ChassisRigidBody.centerOfMass + chassisConfiguration.WheelBase * 0.5f * Vector3.forward;
        }

        private void FixedUpdate()
        {
            velocity = vehicleController.ChassisRigidBody.linearVelocity - Weather.Instance.WindVelocity;

            if (velocity.sqrMagnitude > 0.01f)
            {
                ApplyDrag();
                ApplyLift();
            }
        }

        private void ApplyDrag()
        {
            if(!isDragCalculatedLastFrame)
            {
                VehiclePhysics.GetCollidersCrossSectionPolygon(colliders, 
                                                                velocity.normalized, 
                                                                vehicleController.ChassisRigidBody.transform.up,
                                                                vehicleController.ChassisRigidBody.worldCenterOfMass,
                                                                crossSection);
                crossSectionArea = VehiclePhysics.GetAreaOfConvexHull(crossSection);
                // D = Cd * r * V^2/2 * A
                drag = chassisConfiguration.DragCoefficient * AIR_DENSITY * (velocity.sqrMagnitude / 2f) * crossSectionArea;
                isDragCalculatedLastFrame = true;                
            }
            else
            {
                isDragCalculatedLastFrame = false;
            }
            vehicleController.ChassisRigidBody.AddForce(-drag * velocity.normalized);
        }

        private void ApplyLift()
        {
            if(isDragCalculatedLastFrame)
            {
                VehiclePhysics.GetCollidersCrossSectionPolygon(colliders, Vector3.up, velocity.normalized, vehicleController.ChassisRigidBody.worldCenterOfMass, topDownCrossSection);
                topDownArea = VehiclePhysics.GetAreaOfConvexHull(topDownCrossSection);
                lift = chassisConfiguration.LiftCoefficient * AIR_DENSITY * (velocity.sqrMagnitude / 2f) * topDownArea;
                frontPosition = vehicleController.ChassisRigidBody.centerOfMass + chassisConfiguration.WheelBase * 0.5f * Vector3.forward;
                backPosition = vehicleController.ChassisRigidBody.centerOfMass + chassisConfiguration.WheelBase * 0.5f * Vector3.forward;
            }
            vehicleController.ChassisRigidBody.AddForceAtPosition(lift * -vehicleController.ChassisRigidBody.transform.up * chassisConfiguration.FrontLiftRatio, frontPosition);
            vehicleController.ChassisRigidBody.AddForceAtPosition(lift * -vehicleController.ChassisRigidBody.transform.up * (1-chassisConfiguration.FrontLiftRatio), backPosition);
        }
    }    
}
