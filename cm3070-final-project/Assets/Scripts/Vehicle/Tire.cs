using ModularVehicleSimulator.Physics;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class Tire : MonoBehaviour
    {
        private const float KINEMATIC_SMOOTHING = 50f;
        private const float VELOCITY_FLOOR = 0.1f;
        private const float SMOOTHING_TIME_STEPS = 30f;
        private const float TORQUE_STOP_THRESHOLD = 1f;
        private const float KINEMATIC_SPEED_THRESHOLD = 2f;
        private const float DYNAMIC_SPEED_THRESHOLD = 5f;
        private const int HIGHVELOCITY_SUB_STEPS = 10;
        public WheelFrictionCurve ForwardFriction => forwardFrictionCurve;
        public WheelFrictionCurve SidewaysFriction => sidewaysFrictionCurve;
        public float RPM => currentRPM;
        public float ForwardSlip => forwardSlip;
        public float SidewaysSlip => sidewaysSlip;
        
        private Rigidbody chassisRigidbody;
        private Suspension suspension;
        private WheelConfiguration wheelConfiguration;
        private WheelFrictionCurve forwardFrictionCurve;
        private WheelFrictionCurve sidewaysFrictionCurve;
        private float currentRPM = 0f;
        private float angularVelocity = 0f;
        private float forwardSlip = 0f;
        private float sidewaysSlip = 0f;

        public void Init(Rigidbody chassisRigidbody, WheelConfiguration wheelConfiguration, Suspension suspension)
        {
            this.chassisRigidbody = chassisRigidbody;
            this.wheelConfiguration = wheelConfiguration;
            this.suspension = suspension;
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
            float forceAppPointDistance,
            float steerAngle, 
            float motorTorque, 
            float brakeTorque, 
            bool isGrounded)
        {
            Vector3 wheelVelocity = chassisRigidbody.GetPointVelocity(raycastHit.point);

            Quaternion steerRotation = Quaternion.AngleAxis(steerAngle, transform.up);
            Vector3 wheelForward = steerRotation * transform.forward;
            Vector3 wheelRight = steerRotation * transform.right;

            Vector3 groundForward = Vector3.ProjectOnPlane(wheelForward, raycastHit.normal).normalized;
            Vector3 groundRight = Vector3.ProjectOnPlane(wheelRight, raycastHit.normal).normalized;

            CalculateSlip(motorTorque, brakeTorque, isGrounded, raycastHit, wheelVelocity, groundForward, groundRight, forceAppPointDistance);

            if(isGrounded)
            {
                Vector3 totalFrictionForce = CalculateTotalFrictionForce(groundForward, groundRight, isGrounded);
                Vector3 forceAppPoint = transform.position - (transform.up * forceAppPointDistance);
                ApplyTireForce(raycastHit, forceAppPoint, totalFrictionForce, groundForward, groundRight, wheelVelocity, isGrounded, motorTorque, brakeTorque);                
            }
        }

        private void CalculateSlip(float motorTorque, float brakeTorque, bool isGrounded, RaycastHit raycastHit, Vector3 wheelVelocity, Vector3 groundForward, Vector3 groundRight, float forceAppPointDistance)
        {
            UpdateAngularVelocity(motorTorque, brakeTorque, forceAppPointDistance, isGrounded, raycastHit, wheelVelocity, groundForward, groundRight);
        }

        private void UpdateAngularVelocity(
            float motorTorque, 
            float brakeTorque,
            float forceAppPointDistance,
            bool isGrounded, 
            RaycastHit hit,
            Vector3 wheelVelocity,
            Vector3 groundForward, 
            Vector3 groundRight)
        {
            // Low Velocities
            float radius = wheelConfiguration.Radius;
            float forwardVelocity = Vector3.Dot(groundForward, wheelVelocity);
            float staticFrictionTorqueLimit = suspension.GetNormalLoad(isGrounded) * radius * forwardFrictionCurve.extremumValue;
            bool isLowVelocity = Mathf.Abs(forwardVelocity) < KINEMATIC_SPEED_THRESHOLD;
            float lateralVelocity = Vector3.Dot(groundRight, wheelVelocity);
            bool isRolling = Mathf.Abs(angularVelocity * radius - forwardVelocity) < 0.5f;
            float estimatedDistance = hit.distance;
            // Debug.Log($"{gameObject.name}: forwardVelocity: {forwardVelocity} | isLowVelocity: {isLowVelocity} | isRolling: {isRolling} | Mathf.Abs(motorTorque) < staticFrictionTorqueLimit: {Mathf.Abs(motorTorque) < staticFrictionTorqueLimit} | brakeTorque < TORQUE_STOP_THRESHOLD {brakeTorque < TORQUE_STOP_THRESHOLD}");
            
            if (isLowVelocity && isRolling && Mathf.Abs(motorTorque) < staticFrictionTorqueLimit && brakeTorque < TORQUE_STOP_THRESHOLD)
            {
                suspension.ApplySpringDamperForce(hit, forceAppPointDistance, Time.fixedDeltaTime, wheelVelocity, ref estimatedDistance);                    
                float targetAngularVelocity = forwardVelocity / radius;
                angularVelocity = Mathf.MoveTowards(angularVelocity, targetAngularVelocity, KINEMATIC_SMOOTHING * Time.fixedDeltaTime);
                currentRPM = angularVelocity * Mathf.Rad2Deg / 6f;
                UpdateSidewaysSlip(forwardVelocity, lateralVelocity);
                return;
            }

            // Loop Initialization
            float tireInertia = 0.5f * wheelConfiguration.Weight * (radius * radius);
            float stepTime = Time.fixedDeltaTime / HIGHVELOCITY_SUB_STEPS;
            float drivenMass = Mathf.Max(wheelConfiguration.Weight, suspension.GetNormalLoad(isGrounded) / Mathf.Abs(UnityEngine.Physics.gravity.y));

            for (int i = 0; i < HIGHVELOCITY_SUB_STEPS; i++)
            {
                suspension.ApplySpringDamperForce(hit, forceAppPointDistance, stepTime, wheelVelocity, ref estimatedDistance, HIGHVELOCITY_SUB_STEPS);
            }
            for (int i = 0; i < HIGHVELOCITY_SUB_STEPS; i++)
            {
                bool breakLock = AngularVelocitySubStep(
                    motorTorque,
                    brakeTorque,
                    radius,
                    tireInertia,
                    isGrounded,
                    ref forwardVelocity,
                    stepTime,
                    drivenMass);
                if (breakLock) break;
            }

            if (Mathf.Abs(forwardVelocity) >= KINEMATIC_SPEED_THRESHOLD 
                && Mathf.Abs(forwardVelocity) < DYNAMIC_SPEED_THRESHOLD 
                && Mathf.Abs(motorTorque) < staticFrictionTorqueLimit
                && brakeTorque < TORQUE_STOP_THRESHOLD)
            {
                float t = Mathf.InverseLerp(KINEMATIC_SPEED_THRESHOLD, DYNAMIC_SPEED_THRESHOLD, Mathf.Abs(forwardVelocity));
                angularVelocity = Mathf.Lerp(forwardVelocity / radius, angularVelocity, t);
            }

            currentRPM = angularVelocity * Mathf.Rad2Deg / 6f;
            UpdateSidewaysSlip(forwardVelocity, lateralVelocity);
        }

        private void UpdateSidewaysSlip(float forwardVelocity, float lateralVelocity)
        {
            float rawSidewaysSlip = -Mathf.Atan2(lateralVelocity, Mathf.Abs(forwardVelocity) + VELOCITY_FLOOR);
            float maxSlipAngle = wheelConfiguration.GetDefaultSidewaysFrictionCurve().asymptoteSlip; 
            rawSidewaysSlip = Mathf.Clamp(rawSidewaysSlip, -maxSlipAngle, maxSlipAngle);
            // Dynamic smoothing to simulate tire carcass elasticity
            sidewaysSlip = rawSidewaysSlip;
        }

        private bool AngularVelocitySubStep(
            float motorTorque, 
            float brakeTorque, 
            float radius, 
            float tireInertia, 
            bool isGrounded,
            ref float forwardVelocity,
            float dt,
            float drivenMass
            )
        {
            // Substep velocity and slip
            float surfaceLinearVelocity = angularVelocity * radius;
            float rawForwardSlip = (surfaceLinearVelocity - forwardVelocity) / (Mathf.Abs(forwardVelocity) + VELOCITY_FLOOR);
            forwardSlip = float.IsNaN(rawForwardSlip) ? 0f : rawForwardSlip;

            // Torque
            float frictionCoefficient = VehiclePhysics.EvaluateFrictionCurve(forwardFrictionCurve, Mathf.Abs(forwardSlip));
            float longitudinalForceN = Mathf.Sign(forwardSlip) * frictionCoefficient * suspension.GetNormalLoad(isGrounded);
            float frictionTorque = longitudinalForceN * radius;
            float netTorque = motorTorque - frictionTorque;

            // Apply braking torque opposing current rotation direction
            if (Mathf.Abs(angularVelocity) > 0.001f)
            {
                float brakeDirection = Mathf.Sign(angularVelocity);
                float maxStoppingTorque = Mathf.Abs(angularVelocity) * tireInertia / dt;
                float appliedBrakeTorque = Mathf.Min(brakeTorque, maxStoppingTorque);
                netTorque -= brakeDirection * appliedBrakeTorque;
            }

            // Sub-step Euler Integration
            float angularAcceleration = netTorque / tireInertia;
            float nextAngularVelocity = angularVelocity + (angularAcceleration * dt);
            float estimatedChassisAcceleration = longitudinalForceN / drivenMass;

            // Wheel lock to prevent brakes creating positive acceleration.
            if (brakeTorque > 0f && Mathf.Abs(motorTorque) < TORQUE_STOP_THRESHOLD)
            {
                if (Mathf.Sign(nextAngularVelocity) != Mathf.Sign(angularVelocity) && Mathf.Abs(angularVelocity) < 2f)
                {
                    angularVelocity = 0f;
                    forwardVelocity += estimatedChassisAcceleration * dt;
                    forwardSlip = 0f;
                    sidewaysSlip = 0f;
                    return true; // Stop remaining sub-steps for this frame
                }
            }

            angularVelocity = nextAngularVelocity;
            forwardVelocity += estimatedChassisAcceleration * dt;
            return false;
        }

        private Vector3 CalculateTotalFrictionForce(Vector3 groundForward, Vector3 groundRight, bool isGrounded)
        {
            Vector3 longitudinalForce = CalculateTireForce(groundForward, forwardSlip, forwardFrictionCurve, isGrounded);
            Vector3 lateralForce = CalculateTireForce(groundRight, sidewaysSlip, sidewaysFrictionCurve, isGrounded);
            Vector3 totalFrictionForce = longitudinalForce + lateralForce;
            float maxLongitudinal = forwardFrictionCurve.extremumValue;
            float maxLateral = sidewaysFrictionCurve.extremumValue;
            float maxFrictionForce = Mathf.Max(maxLongitudinal, maxLateral) * suspension.GetNormalLoad(isGrounded);
            if(totalFrictionForce.sqrMagnitude > maxFrictionForce * maxFrictionForce)
            {
                totalFrictionForce = totalFrictionForce.normalized * maxFrictionForce;
            }
            return totalFrictionForce;
        }

        private void ApplyTireForce(
            RaycastHit raycastHit, 
            Vector3 forceAppPoint, 
            Vector3 totalFriction, 
            Vector3 groundForward, 
            Vector3 groundRight, 
            Vector3 wheelVelocity, 
            bool isGrounded,
            float motorTorque, 
            float brakeTorque
            )
        {
            float chassisSpeed = chassisRigidbody.linearVelocity.magnitude;
            float forwardVelocity = Vector3.Dot(groundForward, wheelVelocity);
            float sidewaysVelocity = Vector3.Dot(groundRight, wheelVelocity);
            float staticFrictionTorqueLimit = suspension.GetNormalLoad(isGrounded) * wheelConfiguration.Radius * forwardFrictionCurve.extremumValue;
            // Debug.Log($"motorTorque {motorTorque}, staticFrictionTorqueLimit {staticFrictionTorqueLimit}, angularVelocity {angularVelocity}, forwardVelocity {forwardVelocity}, forwardSlip {forwardSlip}");

            // If the car is moving slowly, friction of the tires should hold it there. 
            if(chassisSpeed < VehiclePhysics.STOPPED_VELOCITY && Mathf.Abs(motorTorque) < TORQUE_STOP_THRESHOLD)
            {
                angularVelocity = 0f; // Kill wheel rotational chatter when parked
                currentRPM = 0f;
                forwardSlip = 0f;
                sidewaysSlip = 0f;

                if (brakeTorque > TORQUE_STOP_THRESHOLD)
                {
                    Vector3 contactVelocity = chassisRigidbody.GetPointVelocity(raycastHit.point);
                    Vector3 normalContactVelocity = Vector3.ProjectOnPlane(contactVelocity, raycastHit.normal);
                    Vector3 holdingForce = -normalContactVelocity * (chassisRigidbody.mass * 10f);
                    
                    float staticFrictionForce = forwardFrictionCurve.extremumValue * suspension.GetNormalLoad(isGrounded);
                    totalFriction = Vector3.ClampMagnitude(holdingForce, staticFrictionForce);
                }
                else
                {
                    totalFriction = Vector3.zero;
                }
            }
            else if(chassisSpeed < VehiclePhysics.STOPPED_VELOCITY && brakeTorque > TORQUE_STOP_THRESHOLD)
            {
                // Clamp the brakes
                forwardSlip = 0f;
                sidewaysSlip = 0f;
                angularVelocity = 0f;
                totalFriction = Vector3.zero;
            }
            else if(Mathf.Abs(forwardVelocity) < DYNAMIC_SPEED_THRESHOLD && Mathf.Abs(motorTorque) < staticFrictionTorqueLimit) // Slow velocity with no slip
            {
                float drivenMass = Mathf.Max(wheelConfiguration.Weight, suspension.GetNormalLoad(isGrounded) / Mathf.Abs(UnityEngine.Physics.gravity.y));
                float dt = Time.fixedDeltaTime;
                float absForwardVelocity = Mathf.Abs(forwardVelocity);

                float engineForce = motorTorque / wheelConfiguration.Radius;
                float rawBrakeForce = brakeTorque / wheelConfiguration.Radius;

                float maxChassisStoppingForce = drivenMass * absForwardVelocity / dt;
                float actualBrakeForce = absForwardVelocity > 0.001f ? Mathf.Min(rawBrakeForce, maxChassisStoppingForce) : 0f;
                float longitudinalForce = engineForce - (actualBrakeForce * Mathf.Sign(forwardVelocity));
                float maxStaticForce = staticFrictionTorqueLimit / wheelConfiguration.Radius;
                longitudinalForce = Mathf.Clamp(longitudinalForce, -maxStaticForce, maxStaticForce);
                float maxStaticSidewaysForce = sidewaysFrictionCurve.extremumValue * suspension.GetNormalLoad(isGrounded);
                float requiredLateralStoppingForce = -(drivenMass * sidewaysVelocity) / dt;
                Vector3 lateralFriction = Mathf.Clamp(requiredLateralStoppingForce, -maxStaticSidewaysForce, maxStaticSidewaysForce)* groundRight;
                Vector3 staticFriction = (longitudinalForce * groundForward) + lateralFriction;

                float t = Mathf.InverseLerp(KINEMATIC_SPEED_THRESHOLD, DYNAMIC_SPEED_THRESHOLD, absForwardVelocity);
                Vector3 appliedForce = Vector3.Lerp(staticFriction, totalFriction, t);

                // Debug.Log($"{gameObject.name}: forwardSlip: {forwardSlip} | sidewaysSlip: {sidewaysSlip} | appliedForce: {appliedForce} | angularVelocity {angularVelocity} | motorTorque {motorTorque} | brakeTorque {brakeTorque} | groundRight {groundRight} | groundForward {groundForward}");
                chassisRigidbody.AddForceAtPosition(appliedForce, forceAppPoint);
                return;
            }
            
            // Debug.Log($"{gameObject.name}: forwardSlip: {forwardSlip} | sidewaysSlip: {sidewaysSlip} | totalFriction: {totalFriction} | angularVelocity {angularVelocity} | motorTorque {motorTorque} | brakeTorque {brakeTorque} | groundRight {groundRight} | groundForward {groundForward} ");
            chassisRigidbody.AddForceAtPosition(totalFriction, forceAppPoint);
        }

        private Vector3 CalculateTireForce(Vector3 groundDirection, float slip, WheelFrictionCurve frictionCurve, bool isGrounded)
        {
            float frictionCoefficient = VehiclePhysics.EvaluateFrictionCurve(frictionCurve, slip);
            Vector3 groundForce = Mathf.Sign(slip) * groundDirection * (frictionCoefficient * suspension.GetNormalLoad(isGrounded));
            return groundForce;
        }
    }    
}
