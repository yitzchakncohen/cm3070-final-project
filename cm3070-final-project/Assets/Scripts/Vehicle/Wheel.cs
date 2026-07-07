using System;
using System.Linq;
using ModularVehicleSimulator.Physics;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class Wheel : MonoBehaviour
    {
        public const float RADIUS_SMOOTH_STEP = 5f;
        public Vector3 WheelFriction => GetWheelFrictionVector();
        public Vector3 WheelContactPoint => GetWheelContactPoint();

        public bool IsMotorized => isMotorized;
        public bool IsSteerable => isSteerable;
        public bool IsFront => isFront;
        public bool IsLeft => transform.localPosition.x < 0f;
        public bool IsRight => transform.localPosition.x > 0f;
        public float SteeringAngle => currentTargetSteeringAngle;
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
        private float currentWheelRadius;
        private float nominalDeflection = 0.02f;

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
            nominalDeflection = VehiclePhysics.GetNominalTireDeflection(
                chassisConfiguration.Mass, 
                chassisConfiguration.NumberOfWheels, 
                wheelConfiguration.RadialTireStiffness
            );
            ApplyWheelPhysicsParamters();
            UpdateWheelPositions();
            UpdateTireVisuals(wheelConfiguration.Radius, wheelConfiguration.Width);
        }

        public void FixedUpdate()
        {
            ApplyDeflection();
        }

        public void Steer(float steeringInput, float currentSpeed)
        {
            // Power Steering, cache for debugging
            currentTargetSteeringAngle = VehiclePhysics.GetTargetSteeringAngle(
                steeringInput,
                currentSpeed,
                steeringConfiguration.HighSpeedThreshold,
                steeringConfiguration.MaxSteeringAngleAtRest,
                steeringConfiguration.MaxSteeringAngleAtHighSpeed
            );
            VehiclePhysics.GetAckermannSteeringAngles(
                chassisConfiguration.WheelBase,
                chassisConfiguration.Track,
                currentTargetSteeringAngle,
                out rightSteeringAngle,
                out leftSteeringAngle
            );
            UpdateWheelAngles();
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

        private void UpdateWheelAngles()
        {
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                if (IsLeft)
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

        private void ApplyWheelPhysicsParamters()
        {
            float numberOfColliders = wheelColliders.Length;
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                wheelCollider.brakeTorque = brakesConfiguration.Torque / numberOfColliders;
                wheelCollider.radius = wheelConfiguration.Radius;
                wheelCollider.mass = wheelConfiguration.Weight / numberOfColliders;
                WheelFrictionCurve forwardFriction = wheelConfiguration.GetForwardFrictionCurve();
                wheelCollider.forwardFriction = forwardFriction;
                WheelFrictionCurve sidewaysFriction = wheelConfiguration.GetSidewaysFrictionCurve(); 
                wheelCollider.sidewaysFriction = sidewaysFriction;
                wheelCollider.suspensionDistance = suspensionConfiguration.Distance;
                wheelCollider.wheelDampingRate = driveTrain.Damping / numberOfColliders;
                if(IsFront)
                {
                    wheelCollider.suspensionSpring = suspensionConfiguration.GetFrontSuspectionSpring(wheelCollider.suspensionSpring.targetPosition, numberOfColliders);
                }
                else
                {
                    wheelCollider.suspensionSpring = suspensionConfiguration.GetBackSuspectionSpring(wheelCollider.suspensionSpring.targetPosition, numberOfColliders);
                }
            }
            UpdateWheelWidth(wheelConfiguration.Width);
        }

        private void ApplyDeflection()
        {
            // TODO factor in weight distribution
            float verticalForce = 0f;
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                wheelCollider.GetGroundHit(out WheelHit hit);
                verticalForce += hit.force;
            }
            float deflection = VehiclePhysics.GetTireDeflection(verticalForce, wheelConfiguration.RadialTireStiffness);
            float bulge = VehiclePhysics.GetTireDeflection(verticalForce, wheelConfiguration.LateralTireStiffness);
            float targetRadius = wheelConfiguration.Radius - deflection;
            currentWheelRadius = Mathf.MoveTowards(currentWheelRadius, targetRadius, RADIUS_SMOOTH_STEP * Time.fixedDeltaTime);
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                wheelCollider.radius = currentWheelRadius;
            }
            float currentWidth = wheelConfiguration.Width + bulge;
            UpdateWheelWidth(currentWidth);
            UpdateTireVisuals(currentWheelRadius, currentWidth);
            UpdateTireFriction(deflection);
        }

        private void UpdateTireVisuals(float currentRadius, float currentWidth)
        {
            tireModel.localScale = new Vector3(currentRadius * 2f, currentWidth / 2f, wheelConfiguration.Radius * 2f);
        }

        private void UpdateTireFriction(float deflection)
        {
            float frictionMultiplier = 1f + (deflection-nominalDeflection)/nominalDeflection * wheelConfiguration.DeflectionGrip;
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                WheelFrictionCurve forwardFriction = wheelCollider.forwardFriction;
                forwardFriction.stiffness = wheelConfiguration.ForwardStiffness * frictionMultiplier;
                wheelCollider.forwardFriction = forwardFriction;

                WheelFrictionCurve sidewaysFriction = wheelCollider.sidewaysFriction;
                sidewaysFriction.stiffness = wheelConfiguration.SideWaysStiffness * frictionMultiplier;
                wheelCollider.sidewaysFriction = sidewaysFriction;
            }
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

        private Vector3 GetWheelFrictionVector()
        {
            Vector3 frictionVector = Vector3.zero;
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                wheelCollider.GetGroundHit(out WheelHit hit);
                float forwardFriction = VehiclePhysics.GetForwardFriction(wheelCollider.forwardFriction, hit.forwardSlip, ref hit);
                float sidewaysFriction = VehiclePhysics.GetSidewaysFriction(wheelCollider.sidewaysFriction, hit.sidewaysSlip, ref hit);
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
    }
}