using ModularVehicleSimulator.Physics;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class Suspension : MonoBehaviour
    {
        private const float SMOOTHING_RATE = 60f;
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

        public void ApplySpringDamperForce(RaycastHit raycastHit, float forceAppPointDistance)
        {
            Vector3 wheelVelocity = chassisRigidBody.GetPointVelocity(raycastHit.point);
            float currentSpringLength = raycastHit.distance;
            springDelta = suspensionConfiguration.Distance - currentSpringLength;

            if(springDelta > 0)
            {
                JointSpring jointSpring = isFront ? suspensionConfiguration.GetFrontSuspensionSpring(0) : suspensionConfiguration.GetRearSuspensionSpring(0);
                float suspensionForce = VehiclePhysics.GetSpringDamperForce(wheelVelocity, transform.up, springDelta, jointSpring);
                float alignmentAngle  = Mathf.Clamp01(Vector3.Dot(transform.up, raycastHit.normal));
                normalLoad = Mathf.Lerp(normalLoad, suspensionForce * alignmentAngle, 1f - Mathf.Exp(-SMOOTHING_RATE * Time.fixedDeltaTime));

                // Apply force from suspension
                Vector3 forcePosition = transform.position - (transform.up * forceAppPointDistance);
                chassisRigidBody.AddForceAtPosition(raycastHit.normal * suspensionForce, forcePosition);     
            }
        }
    }    
}
