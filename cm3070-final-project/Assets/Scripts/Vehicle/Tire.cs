using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class Tire : MonoBehaviour
    {
        public WheelFrictionCurve ForwardFriction => forwardFrictionCurve;
        public WheelFrictionCurve SidewaysFriction => sidewaysFrictionCurve;
        public float RPM => currentRPM;
        public float ForwardSlip => forwardSlip;
        public float SidewaysSlip => sidewaysSlip;
        
        private Rigidbody chassisRigidbody;
        private WheelConfiguration wheelConfiguration;
        private WheelFrictionCurve forwardFrictionCurve;
        private WheelFrictionCurve sidewaysFrictionCurve;
        private float currentRPM;
        private float angularVelocity;
        private float forwardSlip;
        private float sidewaysSlip;

        public void Init(Rigidbody chassisRigidbody, WheelConfiguration wheelConfiguration)
        {
            this.chassisRigidbody = chassisRigidbody;
            this.wheelConfiguration = wheelConfiguration;
        }

        public void UpdateFriction(float deflection, float nominalDeflection, float surfaceFriction)
        {
            float frictionMultiplier = 1f + (deflection-nominalDeflection)/nominalDeflection * wheelConfiguration.DeflectionGrip;
            float temperature = Weather.Instance? Weather.Instance.Temperature : 20f;
            RoadSurfaceCondition roadSurfaceCondition = Weather.Instance? Weather.Instance.RoadSurfaceCondition : RoadSurfaceCondition.None;
            float forwardWeatherMultiplier = wheelConfiguration.GetForwardWeatherFrictionMultiplier(temperature, roadSurfaceCondition);
            float sidewaysWeatherMultiplier = wheelConfiguration.GetSidewaysWeatherFrictionMultiplier(temperature, roadSurfaceCondition);
            WheelFrictionCurve defaultForwardFriction = wheelConfiguration.GetDefaultForwardFrictionCurve();
            WheelFrictionCurve defaultSidewaysFriction = wheelConfiguration.GetDefaultSidewaysFrictionCurve();

            forwardFrictionCurve.stiffness = defaultForwardFriction.stiffness;
            forwardFrictionCurve.extremumValue = defaultForwardFriction.extremumValue * surfaceFriction * frictionMultiplier * forwardWeatherMultiplier;
            forwardFrictionCurve.asymptoteValue = defaultForwardFriction.asymptoteValue * surfaceFriction * frictionMultiplier * forwardWeatherMultiplier;

            sidewaysFrictionCurve.stiffness = defaultSidewaysFriction.stiffness;
            sidewaysFrictionCurve.extremumValue = defaultSidewaysFriction.extremumValue * surfaceFriction * frictionMultiplier * sidewaysWeatherMultiplier;
            sidewaysFrictionCurve.asymptoteValue = defaultSidewaysFriction.asymptoteValue * surfaceFriction * frictionMultiplier * sidewaysWeatherMultiplier;
        }

        public void ApplyFriction(
            RaycastHit hit, 
            float steerAngle, 
            float motorTorque, 
            float brakeTorque, 
            float normalLoad)
        {

            Vector3 wheelVelocity = chassisRigidbody.GetPointVelocity(hit.point);

            Quaternion steerRotation = Quaternion.AngleAxis(steerAngle, transform.up);
            Vector3 wheelForward = steerRotation * transform.forward;
            Vector3 wheelRight = steerRotation * transform.right;

            Vector3 groundForward = Vector3.ProjectOnPlane(wheelForward, hit.normal).normalized;
            Vector3 groundRight = Vector3.ProjectOnPlane(wheelRight, hit.normal).normalized;

            CalculateSlip(motorTorque, brakeTorque, wheelVelocity, groundForward, groundRight);

            Vector3 longitudinalForce = CalculateTireForce(normalLoad, groundForward, forwardSlip, forwardFrictionCurve);
            Vector3 lateralForce = CalculateTireForce(normalLoad, groundRight, sidewaysSlip, sidewaysFrictionCurve);
            Vector3 totalFriction = Vector3.ClampMagnitude(longitudinalForce + lateralForce, normalLoad * Mathf.Max(forwardFrictionCurve.extremumValue, sidewaysFrictionCurve.extremumValue));

            chassisRigidbody.AddForceAtPosition(totalFriction, hit.point);
        }

        private Vector3 CalculateTireForce(float normalLoad, Vector3 groundDirection, float slip, WheelFrictionCurve frictionCurve)
        {
            float forwardForce = EvaluatePacejkaApproximation(slip, frictionCurve);
            Vector3 longitudinalForce = groundDirection * (forwardForce * normalLoad);
            return longitudinalForce;
        }

        private void CalculateSlip(float motorTorque, float brakeTorque, Vector3 wheelVelocity, Vector3 groundForward, Vector3 groundRight)
        {
            // Slip Velocities
            float forwardVelocity = Vector3.Dot(groundForward, wheelVelocity);
            float lateralVelocity = Vector3.Dot(groundRight, wheelVelocity);
            UpdateAngularVelocity(motorTorque, brakeTorque, forwardVelocity);

            // Calculate Slips
            float surfaceLinearVel = angularVelocity * wheelConfiguration.Radius;
            forwardSlip = (surfaceLinearVel - forwardVelocity) / Mathf.Max(Mathf.Abs(forwardVelocity), 0.1f);
            sidewaysSlip = -Mathf.Atan2(lateralVelocity, Mathf.Abs(forwardVelocity)) * Mathf.Rad2Deg;
        }

        private void UpdateAngularVelocity(float motorTorque, float brakeTorque, float forwardVelocity)
        {
            // Wheel rotational inertia integration: I = 0.5 * m * r^2
            float tireInertia = 0.5f * wheelConfiguration.Weight * (wheelConfiguration.Radius * wheelConfiguration.Radius);
            float netTorque = motorTorque - (Mathf.Sign(angularVelocity) * brakeTorque);

            angularVelocity += netTorque / tireInertia * Time.fixedDeltaTime;

            // Free rolling velocity alignment when no torque is applied
            if (Mathf.Abs(motorTorque) < 0.1f && Mathf.Abs(brakeTorque) < 0.1f)
            {
                angularVelocity = Mathf.Lerp(angularVelocity, forwardVelocity / wheelConfiguration.Radius, Time.fixedDeltaTime * 10f);
            }
            currentRPM = angularVelocity * Mathf.Rad2Deg / 6f;
        }

        // Same curved as used by Unity with piecewise linear approximation.
        // https://docs.unity3d.com/6000.6/Documentation/Manual/class-WheelCollider.html  
        private float EvaluatePacejkaApproximation(float slip, WheelFrictionCurve curve)
        {
            // Smoothstep interpolation (3t^2 - 2t^3) between pieces of function
            float absSlip = Mathf.Abs(slip);
            float force;

            // First section of curve from zero to extremum.
            if (absSlip < curve.extremumSlip)
            {
                float t = absSlip / curve.extremumSlip;
                float smoothT = t * t * (3f - 2f * t);
                force = smoothT * curve.extremumValue;
            }
            // Second section of curve from extremum to asymptote. 
            else if (absSlip < curve.asymptoteSlip)
            {
                float t = (absSlip - curve.extremumSlip) / (curve.asymptoteSlip - curve.extremumSlip);
                float smoothT = t * t * (3f - 2f * t);
                force = Mathf.Lerp(curve.extremumValue, curve.asymptoteValue, smoothT);
            }
            else
            {
                force = curve.asymptoteValue;
            }

            return Mathf.Sign(slip) * force * curve.stiffness;
        }
    }    
}
