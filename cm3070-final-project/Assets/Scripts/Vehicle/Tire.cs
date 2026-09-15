using ModularVehicleSimulator.Physics;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class Tire : MonoBehaviour
    {
        private const float VELOCITY_FLOOR = 0.2f;
        private const float SMOOTHING_TIME_STEP = 15f;
        private const float TORQUE_STOP_THRESHOLD = 1f;
        private const int CALCULATION_SUB_STEPS = 10;
        private float DT => Time.fixedDeltaTime / CALCULATION_SUB_STEPS;
        public WheelFrictionCurve ForwardFriction => forwardFrictionCurve;
        public WheelFrictionCurve SidewaysFriction => sidewaysFrictionCurve;
        public float RPM => currentRPM;
        public float ForwardSlip => forwardSlip;
        public float SidewaysSlip => sidewaysSlip;
        
        private Rigidbody chassisRigidbody;
        private WheelConfiguration wheelConfiguration;
        private WheelFrictionCurve forwardFrictionCurve;
        private WheelFrictionCurve sidewaysFrictionCurve;
        private float currentRPM = 0f;
        private float angularVelocity = 0f;
        private float forwardSlip = 0f;
        private float sidewaysSlip = 0f;

        public void Init(Rigidbody chassisRigidbody, WheelConfiguration wheelConfiguration)
        {
            this.chassisRigidbody = chassisRigidbody;
            this.wheelConfiguration = wheelConfiguration;
        }

        public void UpdateFriction(float deflection, float nominalDeflection, float surfaceFriction)
        {
            float deformationMultiplier = TORQUE_STOP_THRESHOLD + (deflection-nominalDeflection)/nominalDeflection * wheelConfiguration.DeflectionGrip;
            float temperature = Weather.Instance? Weather.Instance.Temperature : 20f;
            RoadSurfaceCondition roadSurfaceCondition = Weather.Instance? Weather.Instance.RoadSurfaceCondition : RoadSurfaceCondition.None;
            float forwardWeatherMultiplier = wheelConfiguration.GetForwardWeatherFrictionMultiplier(temperature, roadSurfaceCondition);
            float sidewaysWeatherMultiplier = wheelConfiguration.GetSidewaysWeatherFrictionMultiplier(temperature, roadSurfaceCondition);
            WheelFrictionCurve defaultForwardFriction = wheelConfiguration.GetDefaultForwardFrictionCurve();
            WheelFrictionCurve defaultSidewaysFriction = wheelConfiguration.GetDefaultSidewaysFrictionCurve();

            WheelFrictionCurve forwardFriction = defaultForwardFriction;
            forwardFriction.extremumValue = defaultForwardFriction.extremumValue * surfaceFriction * deformationMultiplier * forwardWeatherMultiplier;
            forwardFriction.asymptoteValue = defaultForwardFriction.asymptoteValue * surfaceFriction * deformationMultiplier * forwardWeatherMultiplier;
            forwardFrictionCurve = forwardFriction;

            WheelFrictionCurve sidewaysFriction = defaultSidewaysFriction;
            sidewaysFriction.extremumValue = defaultSidewaysFriction.extremumValue * surfaceFriction * deformationMultiplier * sidewaysWeatherMultiplier;
            sidewaysFriction.asymptoteValue = defaultSidewaysFriction.asymptoteValue * surfaceFriction * deformationMultiplier * sidewaysWeatherMultiplier;
            sidewaysFrictionCurve = sidewaysFriction;
        }

        public void ApplyFriction(
            RaycastHit raycastHit, 
            Vector3 forceAppPoint,
            float steerAngle, 
            float motorTorque, 
            float brakeTorque, 
            float normalLoad)
        {

            Vector3 wheelVelocity = chassisRigidbody.GetPointVelocity(raycastHit.point);

            Quaternion steerRotation = Quaternion.AngleAxis(steerAngle, transform.up);
            Vector3 wheelForward = steerRotation * transform.forward;
            Vector3 wheelRight = steerRotation * transform.right;

            Vector3 groundForward = Vector3.ProjectOnPlane(wheelForward, raycastHit.normal).normalized;
            Vector3 groundRight = Vector3.ProjectOnPlane(wheelRight, raycastHit.normal).normalized;

            CalculateSlip(motorTorque, brakeTorque, normalLoad, wheelVelocity, groundForward, groundRight);
            Vector3 totalFrictionForce = CalculateTotalFrictionForce(normalLoad, groundForward, groundRight);

            ApplyTireForce(raycastHit, forceAppPoint, totalFrictionForce, normalLoad, motorTorque, brakeTorque);
        }

        private void CalculateSlip(float motorTorque, float brakeTorque, float normalLoad, Vector3 wheelVelocity, Vector3 groundForward, Vector3 groundRight)
        {
            //https://en.wikipedia.org/wiki/Slip_(vehicle_dynamics)  
            float forwardVelocity = Vector3.Dot(groundForward, wheelVelocity);
            float lateralVelocity = Vector3.Dot(groundRight, wheelVelocity);

            // Dynamic smoothing to simulate tire carcass elasticity
            float rawSidewaysSlip = -Mathf.Atan2(lateralVelocity, Mathf.Abs(forwardVelocity) + VELOCITY_FLOOR);
            sidewaysSlip = Mathf.MoveTowards(sidewaysSlip, rawSidewaysSlip, SMOOTHING_TIME_STEP * Time.fixedDeltaTime);

            UpdateAngularVelocity(motorTorque, brakeTorque, normalLoad, forwardVelocity);
        }

        private void UpdateAngularVelocity(
            float motorTorque, 
            float brakeTorque, 
            float normalLoad,
            float forwardVelocity)
        {
            // Constants
            float radius = wheelConfiguration.Radius;
            float tireInertia = 0.5f * wheelConfiguration.Weight * (radius * radius);

            for (int i = 0; i < CALCULATION_SUB_STEPS; i++)
            {
                bool breakLock = AngularVelocitySubStep(motorTorque, brakeTorque, radius, tireInertia, normalLoad, forwardVelocity);                
                if (breakLock) break;
            }

            currentRPM = angularVelocity * Mathf.Rad2Deg / 6f;
        }

        private bool AngularVelocitySubStep(float motorTorque, float brakeTorque, float radius, float tireInertia, float normalLoad, float forwardVelocity)
        {
            // Substep velocity and slip
            float surfaceLinearVelocity = angularVelocity * radius;
            float rawForwardSlip = (surfaceLinearVelocity - forwardVelocity) / (Mathf.Abs(forwardVelocity) + VELOCITY_FLOOR);
            forwardSlip = Mathf.MoveTowards(forwardSlip, rawForwardSlip, SMOOTHING_TIME_STEP * DT);

            // Torque
            float frictionCoefficient = VehiclePhysics.EvaluateFrictionCurve(forwardFrictionCurve, Mathf.Abs(forwardSlip));
            float longitudinalForceN = Mathf.Sign(forwardSlip) * frictionCoefficient * normalLoad;
            float frictionTorque = longitudinalForceN * radius;
            float netTorque = motorTorque - frictionTorque;

            // Apply braking torque opposing current rotation direction
            if (Mathf.Abs(angularVelocity) > 0.001f)
            {
                float brakeDirection = Mathf.Sign(angularVelocity);
                float appliedBrakeTorque = Mathf.Min(brakeTorque, Mathf.Abs(netTorque) + (Mathf.Abs(angularVelocity) * tireInertia / DT));
                netTorque -= brakeDirection * appliedBrakeTorque;
            }

            // Sub-step Euler Integration
            float angularAcceleration = netTorque / tireInertia;
            float nextAngularVelocity = angularVelocity + (angularAcceleration * DT);

            // Wheel lock to prevent brakes creating positive acceleration.
            if (brakeTorque > 0f && Mathf.Abs(motorTorque) < TORQUE_STOP_THRESHOLD)
            {
                if (Mathf.Sign(nextAngularVelocity) != Mathf.Sign(angularVelocity) && Mathf.Abs(angularVelocity) < 2f)
                {
                    angularVelocity = 0f;
                    return true; // Stop remaining sub-steps for this frame
                }
            }

            angularVelocity = nextAngularVelocity;
            return false;
        }

        private Vector3 CalculateTotalFrictionForce(float normalLoad, Vector3 groundForward, Vector3 groundRight)
        {
            Vector3 longitudinalForce = CalculateTireForce(normalLoad, groundForward, forwardSlip, forwardFrictionCurve);
            Vector3 lateralForce = CalculateTireForce(normalLoad, groundRight, sidewaysSlip, sidewaysFrictionCurve);
            Vector3 totalFrictionForce = longitudinalForce + lateralForce;
            float maxFrictionForce = forwardFrictionCurve.extremumValue * normalLoad;
            if(totalFrictionForce.sqrMagnitude > maxFrictionForce * maxFrictionForce)
            {
                totalFrictionForce = totalFrictionForce.normalized * maxFrictionForce;
            }
            return totalFrictionForce;
        }

        private void ApplyTireForce(RaycastHit raycastHit, Vector3 forceAppPoint, Vector3 totalFriction, float normalLoad, float motorTorque, float brakeTorque)
        {
            float chassisSpeed = chassisRigidbody.linearVelocity.magnitude;

            // If the car is moving slowly, friction of the tires should hold it there. 
            if(chassisSpeed < VehiclePhysics.STOPPED_VELOCITY && Mathf.Abs(motorTorque) < TORQUE_STOP_THRESHOLD)
            {
                angularVelocity = 0f; // Kill wheel rotational chatter when parked
                currentRPM = 0f;

                if (brakeTorque > TORQUE_STOP_THRESHOLD)
                {
                    Vector3 contactVelocity = chassisRigidbody.GetPointVelocity(raycastHit.point);
                    Vector3 holdingForce = -contactVelocity * (chassisRigidbody.mass * 10f);
                    
                    float staticFrictionForce = forwardFrictionCurve.extremumValue * normalLoad;
                    totalFriction = Vector3.ClampMagnitude(holdingForce, staticFrictionForce);
                }
                else
                {
                    totalFriction = Vector3.zero;
                }
            }
            chassisRigidbody.AddForceAtPosition(totalFriction, forceAppPoint);
        }

        private Vector3 CalculateTireForce(float normalLoad, Vector3 groundDirection, float slip, WheelFrictionCurve frictionCurve)
        {
            float frictionCoefficient = VehiclePhysics.EvaluateFrictionCurve(frictionCurve, slip);
            Vector3 groundForce = Mathf.Sign(slip) * groundDirection * (frictionCoefficient * normalLoad);
            return groundForce;
        }
    }    
}
