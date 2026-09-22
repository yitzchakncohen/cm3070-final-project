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
        public Vector3 DragVector => -drag * velocity.normalized;
        public float Lift => lift;
        public Vector3 LiftFrontForce => lift * vehicleController.ChassisRigidBody.transform.up * chassisConfiguration.FrontLiftRatio;
        public Vector3 LiftBackForce => lift * vehicleController.ChassisRigidBody.transform.up * (1-chassisConfiguration.FrontLiftRatio);
        public Vector3 FrontPosition => frontPosition;
        public Vector3 BackPosition => backPosition;
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
            frontPosition = vehicleController.ChassisRigidBody.worldCenterOfMass + chassisConfiguration.WheelBase * 0.5f * vehicleController.ChassisRigidBody.transform.forward;
            backPosition = vehicleController.ChassisRigidBody.worldCenterOfMass - chassisConfiguration.WheelBase * 0.5f * vehicleController.ChassisRigidBody.transform.forward;
        }

        private void FixedUpdate()
        {
            Vector3 windVelocity = Weather.Instance != null ? Weather.Instance.WindVelocity : Vector3.zero;
            velocity = vehicleController.ChassisRigidBody.linearVelocity - windVelocity;

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
            vehicleController.ChassisRigidBody.AddForceAtPosition(DragVector, vehicleController.ChassisRigidBody.worldCenterOfMass);
        }

        private void ApplyLift()
        {
            if(isDragCalculatedLastFrame)
            {
                VehiclePhysics.GetCollidersCrossSectionPolygon(colliders, Vector3.up, velocity.normalized, vehicleController.ChassisRigidBody.worldCenterOfMass, topDownCrossSection);
                topDownArea = VehiclePhysics.GetAreaOfConvexHull(topDownCrossSection);
                lift = chassisConfiguration.LiftCoefficient * AIR_DENSITY * (velocity.sqrMagnitude / 2f) * topDownArea;
                frontPosition = vehicleController.ChassisRigidBody.worldCenterOfMass + chassisConfiguration.WheelBase * 0.5f * vehicleController.ChassisRigidBody.transform.forward;
                backPosition = vehicleController.ChassisRigidBody.worldCenterOfMass - chassisConfiguration.WheelBase * 0.5f * vehicleController.ChassisRigidBody.transform.forward;
            }
            vehicleController.ChassisRigidBody.AddForceAtPosition(LiftFrontForce, frontPosition);
            vehicleController.ChassisRigidBody.AddForceAtPosition(LiftBackForce, backPosition);
        }
    }    
}
