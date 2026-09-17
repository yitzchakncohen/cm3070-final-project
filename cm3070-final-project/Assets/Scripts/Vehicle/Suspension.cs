using ModularVehicleSimulator.Physics;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class Suspension : MonoBehaviour
    {
        private const float SMOOTHING_RATE = 60f;
        private const float SUSPENSION_DISTANCE_BUFFER = 1.5f;
        private const int SUB_STEPS = 5;
        public float Offset => springDelta;
        private bool isFront;
        private Rigidbody chassisRigidBody;
        private SuspensionConfiguration suspensionConfiguration;
        private float springDelta;
        private float normalLoad = 0f;

        public void Init(
            Rigidbody chassisRigidBody, 
            SuspensionConfiguration suspensionConfiguration, 
            bool isFront)
        {
            this.chassisRigidBody = chassisRigidBody;
            this.suspensionConfiguration = suspensionConfiguration;
            this.isFront = isFront;
        }

        public void ApplySpringDamperForce(RaycastHit raycastHit, float forceAppPointDistance, float stepTime, Vector3 estimatedVelocity, ref float estimatedDistance, int subSteps = 1)
        {
            float currentSpringLength = estimatedDistance;
            float maxDistance = suspensionConfiguration.Distance * SUSPENSION_DISTANCE_BUFFER;
            springDelta = suspensionConfiguration.Distance - currentSpringLength;

            if(springDelta > 0)
            {
                JointSpring jointSpring = isFront ? suspensionConfiguration.GetFrontSuspensionSpring(0) : suspensionConfiguration.GetRearSuspensionSpring(0);
                float suspensionForce = VehiclePhysics.GetSpringDamperForce(estimatedVelocity, transform.up, springDelta, jointSpring, ref estimatedDistance, maxDistance, stepTime);
                float alignmentAngle  = Mathf.Clamp01(Vector3.Dot(transform.up, raycastHit.normal));
                normalLoad = Mathf.Lerp(normalLoad, suspensionForce * alignmentAngle, 1f - Mathf.Exp(-SMOOTHING_RATE * stepTime));

                // Apply force from suspension
                Vector3 forcePosition = transform.position - (transform.up * forceAppPointDistance);
                chassisRigidBody.AddForceAtPosition(raycastHit.normal * suspensionForce / subSteps, forcePosition);     
            }
        }

        public void IsFree(float stepTime)
        {
            normalLoad = Mathf.Lerp(normalLoad, 0f, 10f * stepTime);
        }

        public float GetNormalLoad(bool isGrounded)
        {
            if(isGrounded) return normalLoad;
            return 0f;
        }
    }    
}
