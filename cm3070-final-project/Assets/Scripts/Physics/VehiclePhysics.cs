using UnityEngine;

namespace ModularVehicleSimulator.Physics
{
    public static class VehiclePhysics
    {
        #region Tires
        public static float GetNominalTireDeflection(float mass, float numberOfWheels, float stiffness)
        {
            float vehicleWeight =  mass * Mathf.Abs(UnityEngine.Physics.gravity.y);
            return  vehicleWeight / numberOfWheels / stiffness;
        }

        public static float GetTireDeflection(float verticalForce, float stiffness)
        {
            // Linear approximation of tire deformation
            return verticalForce / stiffness;
        }

        public static float GetSidewaysFriction(WheelFrictionCurve curve, float slip, ref WheelHit hit)
        {
            float sidewaysFrictionCoefficient = EvaluateFrictionCurve(curve, slip);
            return sidewaysFrictionCoefficient * hit.force * Mathf.Sign(hit.sidewaysSlip);
        }

        public static float GetForwardFriction(WheelFrictionCurve curve, float slip, ref WheelHit hit)
        {
            float forwardFrictionCoefficient = EvaluateFrictionCurve(curve, slip);
            return forwardFrictionCoefficient * hit.force * Mathf.Sign(hit.forwardSlip);
        }

        private static float EvaluateFrictionCurve(WheelFrictionCurve curve, float slip)
        {
            float absSlip = Mathf.Abs(slip);

            // 1. First spline section: from 0 to Extremum
            if (absSlip < curve.extremumSlip)
            {
                float t = absSlip / curve.extremumSlip;
                // Cubic spline interpolation with zero tangent at origin and extremum
                return Mathf.SmoothStep(0f, curve.extremumValue, t);
            }
            // 2. Second spline section: from Extremum to Asymptote
            else if (absSlip < curve.asymptoteSlip)
            {
                float range = curve.asymptoteSlip - curve.extremumSlip;
                float t = (absSlip - curve.extremumSlip) / range;
                // Cubic spline interpolation between Extremum Value and Asymptote Value
                return Mathf.SmoothStep(curve.extremumValue, curve.asymptoteValue, t);
            }
            // 3. Beyond Asymptote: returns the constant Asymptote Value
            else
            {
                return curve.asymptoteValue;
            }
        }
        #endregion

        #region Steering
        public static float GetTargetSteeringAngle(float steeringInput, float currentSpeed, float highSpeedThreshold, float maxSteeringAngleAtRest, float maxSteeringAngleAtHighSpeed)
        {
            float speedFactor = Mathf.InverseLerp(0f, highSpeedThreshold, currentSpeed);
            float allowableMaxSteer = Mathf.Lerp(maxSteeringAngleAtRest, maxSteeringAngleAtHighSpeed, speedFactor);
            float targetSteeringAngle = steeringInput * allowableMaxSteer;
            return targetSteeringAngle;
        }

        // Ackerman's Geometric Model //
        public static void GetAckermannSteeringAngles(float wheelBase, float track, float targetAngle, out float rightSteeringAngle, out float leftSteeringAngle)
        {
            // Handle small values
            if(Mathf.Abs(targetAngle) < 0.1f)
            {
                rightSteeringAngle = leftSteeringAngle = targetAngle;
                return;
            }

            float tanOfTargetAngle = Mathf.Tan(Mathf.Abs(targetAngle) * Mathf.Deg2Rad);
            if(targetAngle > 0)
            {
                rightSteeringAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / ((wheelBase / tanOfTargetAngle) + (track/2))) * Mathf.Sign(targetAngle);
                leftSteeringAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / ((wheelBase / tanOfTargetAngle) - (track/2))) * Mathf.Sign(targetAngle);
            }
            else
            {
                rightSteeringAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / ((wheelBase / tanOfTargetAngle) - (track/2))) * Mathf.Sign(targetAngle);
                leftSteeringAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / ((wheelBase / tanOfTargetAngle) + (track/2))) * Mathf.Sign(targetAngle);
            }
        }

        public static float GetTurningRadius(float wheelBase, float steeringAngle)
        {
            return wheelBase / Mathf.Tan(steeringAngle * Mathf.Deg2Rad);
        }
        #endregion
    }
}
