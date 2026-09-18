using ModularVehicleSimulator.Physics;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class Suspension : MonoBehaviour
    {
        private const float SMOOTHING_RATE = 60f;
        private const float SUSPENSION_DISTANCE_BUFFER = 1.5f;
        public float Offset => springDelta;
        private bool isFront;
        private Rigidbody chassisRigidBody;
        private SuspensionConfiguration suspensionConfiguration;
        private WheelConfiguration wheelConfiguration;
        private float springDelta;
        private float normalLoad = 0f;

        public void Init(
            Rigidbody chassisRigidBody, 
            SuspensionConfiguration suspensionConfiguration, 
            WheelConfiguration wheelConfiguration,
            bool isFront)
        {
            this.chassisRigidBody = chassisRigidBody;
            this.suspensionConfiguration = suspensionConfiguration;
            this.wheelConfiguration = wheelConfiguration;
            this.isFront = isFront;
        }

        public void ApplySpringDamperForce(WheelContactData raycastHit, float forceAppPointDistance, float stepTime, Vector3 estimatedVelocity, ref float estimatedDistance, int subSteps = 1)
        {
            float currentSpringLength = estimatedDistance;
            float maxDistance = (suspensionConfiguration.Distance  + wheelConfiguration.Radius) * SUSPENSION_DISTANCE_BUFFER;
            springDelta = suspensionConfiguration.Distance - currentSpringLength;

            // Debug.Log($"springDelta {springDelta}");
            if(springDelta > 0)
            {
                JointSpring jointSpring = isFront ? suspensionConfiguration.GetFrontSuspensionSpring(0) : suspensionConfiguration.GetRearSuspensionSpring(0);
                float suspensionForce = VehiclePhysics.GetSpringDamperForce(estimatedVelocity, transform.up, springDelta, jointSpring, ref estimatedDistance, maxDistance, stepTime);
                float alignmentAngle  = Mathf.Clamp01(Vector3.Dot(transform.up, raycastHit.normal));
                normalLoad = Mathf.Lerp(normalLoad, suspensionForce * alignmentAngle, 1f - Mathf.Exp(-SMOOTHING_RATE * stepTime));

                // Apply force from suspension
                // Debug.Log($"raycastHit.normal * suspensionForce / subSteps {raycastHit.normal * suspensionForce / subSteps}");
                Vector3 forcePosition = transform.position - (transform.up * forceAppPointDistance);
                chassisRigidBody.AddForceAtPosition(raycastHit.normal * suspensionForce / subSteps, forcePosition);     
            }
        }

        public float GetNormalLoad(bool isGrounded)
        {
            if(isGrounded) return normalLoad;
            return 0f;
        }
    }    
}
