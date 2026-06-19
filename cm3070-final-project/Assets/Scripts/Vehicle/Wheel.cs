using System;
using System.Linq;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class Wheel : MonoBehaviour
    {
        public bool IsMotorized => isMotorized;
        public bool IsSteerable => isSteerable;
        public bool IsFront => isFront;
        public bool IsLeft => transform.localPosition.x < 0f;
        public bool IsRight => transform.localPosition.x > 0f;
        [SerializeField] private bool isMotorized = true;
        [SerializeField] private bool isSteerable = true;
        [SerializeField] private bool isFront = true;
        [SerializeField] private Transform wheelModel;
        private WheelCollider[] wheelColliders;
        private WheelConfiguration wheelConfiguration;
        private BrakesConfiguration brakesConfiguration;
        private SteeringConfiguration steeringConfiguration;
        private SuspensionConfiguration suspensionConfiguration;
        private ChassisConfiguration chassisConfiguration;
        private DriveTrain driveTrain;

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
        }

        public void Steer(float steeringInput, float currentSpeed)
        {
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;

            // Power Steering
            float targetSteeringAngle = GetTargetSteeringAngle(steeringInput, currentSpeed);
            GetSteeringAngles(chassisConfiguration.WheelBase, chassisConfiguration.Track, targetSteeringAngle, out float rightSteeringAngle, out float leftSteeringAngle);

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
                // TODO apply width
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
        }

        private float GetTargetSteeringAngle(float steeringInput, float currentSpeed)
        {
            float speedFactor = Mathf.InverseLerp(0f, steeringConfiguration.HighSpeedThreshold, currentSpeed);
            float allowableMaxSteer = Mathf.Lerp(steeringConfiguration.MaxSteeringAngleAtRest, steeringConfiguration.MaxSteeringAngleAtHighSpeed, speedFactor);
            float targetSteeringAngle = steeringInput * allowableMaxSteer;
            return targetSteeringAngle;
        }

        // Ackerman's Geometric Model
        private static void GetSteeringAngles(float wheelBase, float track, float targetAngle, out float rightSteeringAngle, out float leftSteeringAngle)
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
    }
}