using ModularVehicleSimulator.Physics;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class Tire : MonoBehaviour
    {
        private const float KINEMATIC_SMOOTHING = 50f;
        private const float VELOCITY_FLOOR = 0.1f;
        private const float SMOOTHING_TIME_STEPS = 15f;
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
            float normalLoad,
            bool isGrounded)
        {
            if(!isGrounded) normalLoad = 0f;

            Vector3 wheelVelocity = chassisRigidbody.GetPointVelocity(raycastHit.point);

            Quaternion steerRotation = Quaternion.AngleAxis(steerAngle, transform.up);
            Vector3 wheelForward = steerRotation * transform.forward;
            Vector3 wheelRight = steerRotation * transform.right;

            Vector3 groundForward = Vector3.ProjectOnPlane(wheelForward, raycastHit.normal).normalized;
            Vector3 groundRight = Vector3.ProjectOnPlane(wheelRight, raycastHit.normal).normalized;

            CalculateSlip(motorTorque, brakeTorque, normalLoad, wheelVelocity, groundForward, groundRight);

            if(isGrounded)
            {
                Vector3 totalFrictionForce = CalculateTotalFrictionForce(normalLoad, groundForward, groundRight);
                ApplyTireForce(raycastHit, forceAppPoint, totalFrictionForce, groundForward, groundRight, wheelVelocity, normalLoad, motorTorque, brakeTorque);                
            }
        }

        private void CalculateSlip(float motorTorque, float brakeTorque, float normalLoad, Vector3 wheelVelocity, Vector3 groundForward, Vector3 groundRight)
        {
            UpdateAngularVelocity(motorTorque, brakeTorque, normalLoad, wheelVelocity, groundForward, groundRight);
        }

        private void UpdateAngularVelocity(
            float motorTorque, 
            float brakeTorque, 
            float normalLoad,
            Vector3 wheelVelocity,
            Vector3 groundForward, 
            Vector3 groundRight)
        {
            // Low Velocities
            float radius = wheelConfiguration.Radius;
            float forwardVelocity = Vector3.Dot(groundForward, wheelVelocity);
            float staticFrictionTorqueLimit = normalLoad * radius * forwardFrictionCurve.extremumValue;
            bool isLowVelocity = forwardVelocity < DYNAMIC_SPEED_THRESHOLD;
            float lateralVelocity = Vector3.Dot(groundRight, wheelVelocity);
            bool isRolling = Mathf.Abs(angularVelocity * radius - forwardVelocity) < 0.5f;
            
            if (isLowVelocity && isRolling && Mathf.Abs(motorTorque) < staticFrictionTorqueLimit && brakeTorque < TORQUE_STOP_THRESHOLD)
            {
                float targetAngularVelocity = forwardVelocity / radius;
                angularVelocity = Mathf.MoveTowards(angularVelocity, targetAngularVelocity, KINEMATIC_SMOOTHING * Time.fixedDeltaTime);
                currentRPM = angularVelocity * Mathf.Rad2Deg / 6f;
                UpdateSidewaysSlip(forwardVelocity, lateralVelocity);
                return;
            }

            // Loop Initialization
            float tireInertia = 0.5f * wheelConfiguration.Weight * (radius * radius);
            float stepTime = Time.fixedDeltaTime / HIGHVELOCITY_SUB_STEPS;
            float drivenMass = normalLoad / Mathf.Abs(UnityEngine.Physics.gravity.y);

            for (int i = 0; i < HIGHVELOCITY_SUB_STEPS; i++)
            {
                bool breakLock = AngularVelocitySubStep(
                    motorTorque,
                    brakeTorque,
                    radius,
                    tireInertia,
                    normalLoad,
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
            // Dynamic smoothing to simulate tire carcass elasticity
            sidewaysSlip = Mathf.MoveTowards(sidewaysSlip, rawSidewaysSlip, SMOOTHING_TIME_STEPS * Time.fixedDeltaTime);
        }

        private bool AngularVelocitySubStep(
            float motorTorque, 
            float brakeTorque, 
            float radius, 
            float tireInertia, 
            float normalLoad, 
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
            float longitudinalForceN = Mathf.Sign(forwardSlip) * frictionCoefficient * normalLoad;
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

        private void ApplyTireForce(
            RaycastHit raycastHit, 
            Vector3 forceAppPoint, 
            Vector3 totalFriction, 
            Vector3 groundForward, 
            Vector3 groundRight, 
            Vector3 wheelVelocity, 
            float normalLoad, 
            float motorTorque, 
            float brakeTorque
            )
        {
            float chassisSpeed = chassisRigidbody.linearVelocity.magnitude;
            float forwardVelocity = Vector3.Dot(groundForward, wheelVelocity);
            float sidewaysVelocity = Vector3.Dot(groundRight, wheelVelocity);
            float staticFrictionTorqueLimit = normalLoad * wheelConfiguration.Radius * forwardFrictionCurve.extremumValue;
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
                    Vector3 holdingForce = -contactVelocity * (chassisRigidbody.mass * 10f);
                    
                    float staticFrictionForce = forwardFrictionCurve.extremumValue * normalLoad;
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
            else if(forwardVelocity < DYNAMIC_SPEED_THRESHOLD && Mathf.Abs(motorTorque) < staticFrictionTorqueLimit) // Slow velocity with no slip
            {
                float drivenMass = normalLoad / Mathf.Abs(UnityEngine.Physics.gravity.y);
                float dt = Time.fixedDeltaTime;
                float absForwardVelocity = Mathf.Abs(forwardVelocity);

                float engineForce = motorTorque / wheelConfiguration.Radius;
                float rawBrakeForce = brakeTorque / wheelConfiguration.Radius;

                float maxChassisStoppingForce = drivenMass * absForwardVelocity / dt;
                float actualBrakeForce = absForwardVelocity > 0.001f ? Mathf.Min(rawBrakeForce, maxChassisStoppingForce) : 0f;
                float longitudinalForce = engineForce - (actualBrakeForce * Mathf.Sign(forwardVelocity));
                float maxStaticForce = staticFrictionTorqueLimit / wheelConfiguration.Radius;
                longitudinalForce = Mathf.Clamp(longitudinalForce, -maxStaticForce, maxStaticForce);
                float maxStaticSidewaysForce = sidewaysFrictionCurve.extremumValue * normalLoad;
                float requiredLateralStoppingForce = -(drivenMass * sidewaysVelocity) / dt;
                Vector3 lateralFriction = Mathf.Clamp(requiredLateralStoppingForce, -maxStaticSidewaysForce, maxStaticSidewaysForce)* groundRight;
                Vector3 staticFriction = (longitudinalForce * groundForward) + lateralFriction;

                float t = Mathf.InverseLerp(KINEMATIC_SPEED_THRESHOLD, DYNAMIC_SPEED_THRESHOLD, absForwardVelocity);
                Vector3 appliedForce = Vector3.Lerp(staticFriction, totalFriction, t);

                Debug.Log($"{gameObject.name}: forwardSlip: {forwardSlip} | sidewaysSlip: {sidewaysSlip} | appliedForce: {appliedForce} | angularVelocity {angularVelocity} | motorTorque {motorTorque} | brakeTorque {brakeTorque}");
                chassisRigidbody.AddForceAtPosition(appliedForce, forceAppPoint);
                return;
            }
            
            Debug.Log($"{gameObject.name}: forwardSlip: {forwardSlip} | sidewaysSlip: {sidewaysSlip} | totalFriction: {totalFriction} | angularVelocity {angularVelocity} | motorTorque {motorTorque} | brakeTorque {brakeTorque}");
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
