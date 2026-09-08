using ModularVehicleSimulator.Physics;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class Suspension : MonoBehaviour
    {
        public float NormalLoad => normalLoad;
        public float Offset => springDelta;
        private bool isFront;
        private Rigidbody chassisRigidBody;
        private SuspensionConfiguration suspensionConfiguration;
        private WheelConfiguration wheelConfiguration;
        private ChassisConfiguration chassisConfiguration;
        private float springDelta;
        private float normalLoad;

        public void Init(
            Rigidbody chassisRigidBody, 
            SuspensionConfiguration suspensionConfiguration, 
            WheelConfiguration wheelConfiguration,
            ChassisConfiguration chassisConfiguration,
            bool isFront)
        {
            this.chassisRigidBody = chassisRigidBody;
            this.suspensionConfiguration = suspensionConfiguration;
            this.wheelConfiguration = wheelConfiguration;
            this.chassisConfiguration = chassisConfiguration;
            this.isFront = isFront;
        }

        public void ApplySpringDamperForce(RaycastHit raycastHit)
        {
            Vector3 wheelVelocity = chassisRigidBody.GetPointVelocity(transform.position);
            float currentSpringLength = raycastHit.distance - wheelConfiguration.Radius;
            springDelta = suspensionConfiguration.Distance - currentSpringLength;
            float normalizedSpringDelta = Mathf.Clamp01(springDelta / suspensionConfiguration.Distance);

            JointSpring jointSpring = isFront ? suspensionConfiguration.GetFrontSuspensionSpring(0) : suspensionConfiguration.GetRearSuspensionSpring(0);
            float suspensionForce = VehiclePhysics.GetSpringDamperForce(wheelVelocity, transform.up, springDelta, jointSpring);
            float alignmentAngle  = Mathf.Clamp01(Vector3.Dot(transform.up, raycastHit.normal));
            normalLoad = suspensionForce * alignmentAngle;

            // Apply force from suspension
            Vector3 forcePosition = transform.position - (transform.up * GetForceAppPointDistance());
            chassisRigidBody.AddForceAtPosition(transform.up * suspensionForce, forcePosition);
        }

        private float GetForceAppPointDistance()
        {
            Vector3 wheelLocalPosition = chassisRigidBody.transform.InverseTransformPoint(transform.position);
            float wheelOffsetFromGround = wheelConfiguration.Radius;
            float offsetFromGroundToCenterOfMass = chassisConfiguration.CenterOfMass.y - wheelLocalPosition.y + wheelOffsetFromGround;
            float offsetDistance = offsetFromGroundToCenterOfMass - suspensionConfiguration.ForceAppPointOffset;
            return Mathf.Max(0f, offsetDistance);
        }
    }    
}
