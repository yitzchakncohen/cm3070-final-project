using System;
using System.Linq;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class Wheel : MonoBehaviour
    {
        public Vector3 WheelFriction => GetWheelFrictionVector();
        public Vector3 WheelContactPoint => GetWheelContactPoint();

        public bool IsMotorized => isMotorized;
        public bool IsSteerable => isSteerable;
        public bool IsFront => isFront;
        public bool IsLeft => transform.localPosition.x < 0f;
        public bool IsRight => transform.localPosition.x > 0f;
        public float SteeringAngle => currentTargetSteeringAngle;
        public float RightSteeringAngle => rightSteeringAngle;
        public float LeftSteeringAngle => leftSteeringAngle;
        [SerializeField] private bool isMotorized = true;
        [SerializeField] private bool isSteerable = true;
        [SerializeField] private bool isFront = true;
        [SerializeField] private Transform wheelModel;
        [SerializeField] private Transform tireModel;
        private WheelCollider[] wheelColliders;
        private WheelConfiguration wheelConfiguration;
        private BrakesConfiguration brakesConfiguration;
        private SteeringConfiguration steeringConfiguration;
        private SuspensionConfiguration suspensionConfiguration;
        private ChassisConfiguration chassisConfiguration;
        private DriveTrain driveTrain;
        private float currentTargetSteeringAngle = 0f;
        private float rightSteeringAngle = 0f;
        private float leftSteeringAngle = 0f;

        private void Awake()
        {
            wheelColliders = GetComponentsInChildren<WheelCollider>();
        }

        public void Init(WheelConfiguration wheels, 
                        BrakesConfiguration brakes, 
                        SteeringConfiguration steering, 
                        SuspensionConfiguration suspension,
                        ChassisConfiguration chassis,
                        DriveTrain driveTrain)
        {
            wheelConfiguration = wheels;
            brakesConfiguration = brakes;
            steeringConfiguration = steering;
            suspensionConfiguration = suspension;
            chassisConfiguration = chassis;
            this.driveTrain = driveTrain;
            ApplyWheelPhysicsParamters();
            UpdateWheelPositions();
            UpdateTireVisuals(wheelConfiguration.Radius, wheelConfiguration.Width);
        }

        public void FixedUpdate()
        {
            ApplyDeflection();
        }

        private void ApplyDeflection()
        {
            float verticalForce = 0f;
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                wheelCollider.GetGroundHit(out WheelHit hit);
                verticalForce += hit.force;
            }
            // Linear approximation of tire deformation
            float deflection = verticalForce / wheelConfiguration.RadialTireStiffness;
            float bulge = verticalForce / wheelConfiguration.LateralTireStiffness;
            float currentRadius = wheelConfiguration.Radius - deflection;
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                wheelCollider.radius = currentRadius;
            }
            float currentWidth = wheelConfiguration.Width + bulge;
            UpdateWheelWidth(currentWidth);
            UpdateTireVisuals(currentRadius, currentWidth);
            // TODO Update friction
        }

        private void UpdateTireVisuals(float currentRadius, float currentWidth)
        {
            tireModel.localScale = new Vector3(currentRadius * 2f, currentWidth / 2f, wheelConfiguration.Radius * 2f);
        }

        public void Steer(float steeringInput, float currentSpeed)
        {
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;

            // Power Steering, cache for debugging
            // TODO move to steering class.
            currentTargetSteeringAngle = GetTargetSteeringAngle(steeringInput, currentSpeed);
            GetSteeringAngles(chassisConfiguration.WheelBase, chassisConfiguration.Track, currentTargetSteeringAngle);

            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                if(IsLeft)
                {
                    wheelCollider.steerAngle = Mathf.MoveTowards(wheelCollider.steerAngle, rightSteeringAngle, steeringConfiguration.SteeringSpeed * Time.fixedDeltaTime);                    
                }
                else
                {
                    wheelCollider.steerAngle = Mathf.MoveTowards(wheelCollider.steerAngle, leftSteeringAngle, steeringConfiguration.SteeringSpeed * Time.fixedDeltaTime);                    
                }
                wheelCollider.GetWorldPose(out Vector3 wheelPosition, out rotation);
                position = position + wheelPosition;
            }
            wheelModel.position = position / wheelColliders.Count();
            wheelModel.rotation = rotation;
        }

        public void Accelerate(float torque)
        {
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                wheelCollider.brakeTorque = 0f;
                wheelCollider.motorTorque = torque / wheelColliders.Length;             
            }
        }

        public void Brake(float brakingInput)
        {
            float brakeTorque = brakesConfiguration.Torque * brakesConfiguration.FrontBias;
            if(!IsFront)
            {
                brakeTorque = brakesConfiguration.Torque * (1-brakesConfiguration.FrontBias);
            }
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                wheelCollider.motorTorque = 0f;
                wheelCollider.brakeTorque = brakingInput * brakeTorque / wheelColliders.Count();            
            }
        }

        private void ApplyWheelPhysicsParamters()
        {
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                wheelCollider.brakeTorque = brakesConfiguration.Torque;
                wheelCollider.radius = wheelConfiguration.Radius;
                wheelCollider.mass = wheelConfiguration.Weight;
                WheelFrictionCurve forwardFriction = new WheelFrictionCurve
                {
                    extremumSlip = wheelConfiguration.ForwardExtremeSlip,
                    extremumValue = wheelConfiguration.ForwardExtremeValue,
                    asymptoteSlip = wheelConfiguration.ForwardAsymptoteSlip,
                    asymptoteValue = wheelConfiguration.ForwardAsymptoteValue,
                    stiffness = wheelConfiguration.ForwardStiffness
                };
                wheelCollider.forwardFriction = forwardFriction;
                WheelFrictionCurve sidewaysFriction = new WheelFrictionCurve
                {
                    extremumSlip = wheelConfiguration.SideWaysExtremeSlip,
                    extremumValue = wheelConfiguration.SideWaysExtremeValue,
                    asymptoteSlip = wheelConfiguration.SideWaysAsymptoteSlip,
                    asymptoteValue = wheelConfiguration.SideWaysAsymptoteValue,
                    stiffness = wheelConfiguration.SideWaysStiffness
                };
                wheelCollider.sidewaysFriction = sidewaysFriction;
                wheelCollider.suspensionDistance = suspensionConfiguration.Distance;
                wheelCollider.wheelDampingRate = driveTrain.Damping;
                if(IsFront)
                {
                    wheelCollider.suspensionSpring = new JointSpring
                    {
                        spring = suspensionConfiguration.FrontSpring,
                        damper = suspensionConfiguration.Damper,
                        targetPosition = wheelCollider.suspensionSpring.targetPosition
                    };
                }
                else
                {
                    wheelCollider.suspensionSpring = new JointSpring
                    {
                        spring = suspensionConfiguration.BackSpring,
                        damper = suspensionConfiguration.Damper,
                        targetPosition = wheelCollider.suspensionSpring.targetPosition
                    };
                }
            }
            UpdateWheelWidth(wheelConfiguration.Width);
        }

        private void UpdateWheelPositions()
        {
            float wheelXPosition = chassisConfiguration.Track/2f;
            float wheelZPosition = chassisConfiguration.WheelBase/2f;
            float wheelYPosition = 0f;
            if(IsFront && IsLeft)
            {
                transform.localPosition = new Vector3(-wheelXPosition, wheelYPosition, wheelZPosition);
            }
            else if(IsFront && IsRight)
            {
                transform.localPosition = new Vector3(wheelXPosition, wheelYPosition, wheelZPosition);
            }
            else if(!IsFront && IsLeft)
            {
                transform.localPosition = new Vector3(-wheelXPosition, wheelYPosition, -wheelZPosition);
            }
            else if(!IsFront && IsRight)
            {
                transform.localPosition = new Vector3(wheelXPosition, wheelYPosition, -wheelZPosition);
            }
        }

        private void UpdateWheelWidth(float currentWidth)
        {
            float offset = -currentWidth/2;
            float increment = (wheelColliders.Length - 1) * currentWidth;
            for (int i = 0; i < wheelColliders.Length; i++)
            {
                wheelColliders[i].center = new Vector3(offset + i * increment, 0f, 0f);
            }
        }

        private float GetTargetSteeringAngle(float steeringInput, float currentSpeed)
        {
            float speedFactor = Mathf.InverseLerp(0f, steeringConfiguration.HighSpeedThreshold, currentSpeed);
            float allowableMaxSteer = Mathf.Lerp(steeringConfiguration.MaxSteeringAngleAtRest, steeringConfiguration.MaxSteeringAngleAtHighSpeed, speedFactor);
            float targetSteeringAngle = steeringInput * allowableMaxSteer;
            return targetSteeringAngle;
        }

        // Ackerman's Geometric Model
        private void GetSteeringAngles(float wheelBase, float track, float targetAngle)
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

        private Vector3 GetWheelFrictionVector()
        {
            Vector3 frictionVector = Vector3.zero;
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                wheelCollider.GetGroundHit(out WheelHit hit);
                float forwardFrictionCoefficient = EvaluateFrictionCurve(wheelCollider.forwardFriction, hit.forwardSlip);
                float sidewaysFrictionCoefficient = EvaluateFrictionCurve(wheelCollider.sidewaysFriction, hit.sidewaysSlip);
                float forwardFriction = forwardFrictionCoefficient * hit.force * Mathf.Sign(hit.forwardSlip);
                float sidewaysFriction = sidewaysFrictionCoefficient * hit.force * Mathf.Sign(hit.sidewaysSlip);
                frictionVector += (hit.forwardDir * forwardFriction) + (hit.sidewaysDir * sidewaysFriction);
            }
            return frictionVector;
        }

        private Vector3 GetWheelContactPoint()
        {
            Vector3 contactPoint = Vector3.zero;
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                wheelCollider.GetGroundHit(out WheelHit hit);
                contactPoint += hit.point;
            }
            return contactPoint / wheelColliders.Length;
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
    }
}