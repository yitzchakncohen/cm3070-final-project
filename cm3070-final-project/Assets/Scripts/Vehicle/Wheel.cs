using System;
using System.Linq;
using ModularVehicleSimulator.Physics;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class Wheel : MonoBehaviour
    {
        public const float DEFLECTION_SMOOTH_STEP = 1f;
        public const float EFFECTIVE_SLIP_THRESHHOLD = 0.15f;
        public const float SPEEDOMETER_SLIP_THRESHHOLD = 0.75f;
        public Vector3 WheelFriction => GetWheelFrictionVector();
        public Vector3 WheelContactPoint => GetWheelContactPoint();

        public bool IsMotorized => isMotorized;
        public bool IsSteerable => isSteerable;
        public bool IsFront => isFront;
        public bool IsLeft => transform.localPosition.x < 0f;
        public bool IsRight => transform.localPosition.x > 0f;
        public float RPM => wheelColliders.Average(wheelCollider => wheelCollider.rpm);
        private int numberOfColliders => wheelColliders.Length;
        [SerializeField] private bool isMotorized = true;
        [SerializeField] private bool isSteerable = true;
        [SerializeField] private bool isFront = true;
        [SerializeField] private Transform wheelModel;
        [SerializeField] private Transform tireModel;
        private WheelCollider[] wheelColliders;
        private WheelConfiguration wheelConfiguration;
        private SteeringConfiguration steeringConfiguration;
        private SuspensionConfiguration suspensionConfiguration;
        private ChassisConfiguration chassisConfiguration;
        private DriveTrain driveTrain;
        private PhysicsMaterial currentSurfaceMaterial = null;
        private float rightSteeringAngle = 0f;
        private float leftSteeringAngle = 0f;
        private float currentDeflection = 0f;
        private float nominalDeflection = 0.02f;

        private void Awake()
        {
            wheelColliders = GetComponentsInChildren<WheelCollider>();
        }

        private void Update()
        {
            UpdateSurfaceMaterial();
        }

        public void Init(WheelConfiguration wheels, 
                        SteeringConfiguration steering, 
                        SuspensionConfiguration suspension,
                        ChassisConfiguration chassis,
                        DriveTrain driveTrain)
        {
            wheelConfiguration = wheels;
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

        private void FixedUpdate()
        {
            ApplyDeflection();
        }

        public void Steer(float leftSteeringAngle, float rightSteeringAngle)
        {
            this.rightSteeringAngle = rightSteeringAngle;
            this.leftSteeringAngle = leftSteeringAngle;
            UpdateWheelAngles();
        }

        public void Accelerate(float torque, float brakeTorque)
        {
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                wheelCollider.brakeTorque = brakeTorque / numberOfColliders;
                wheelCollider.motorTorque = torque / numberOfColliders;             
            }
        }

        public void Brake(float brakingInput, float brakeTorque)
        {
            float brakeTorquePerCollider = brakeTorque / numberOfColliders;
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                wheelCollider.motorTorque = 0f;
                wheelCollider.brakeTorque = brakingInput * brakeTorquePerCollider;            
            }
        }

        public float GetEffectiveRPM()
        {
            return GetRPM(EFFECTIVE_SLIP_THRESHHOLD);
        }

        public float GetSpeedometerRPM()
        {
            return GetRPM(SPEEDOMETER_SLIP_THRESHHOLD);
        }

        public float GetAverageForwardSlip()
        {
            float slip = 0f;
            int colliders = 0;
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                float colliderSlip = GetSlipForCollider(wheelCollider);
                if(colliderSlip < Mathf.Infinity)
                {
                    colliders++;
                    slip += colliderSlip;
                }
            }
            return slip / colliders;
        }

        private void UpdateSurfaceMaterial()
        {
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                WheelHit hit;
                if (wheelCollider.GetGroundHit(out hit))
                {
                    if (hit.collider.material.GetType() == typeof(PhysicsMaterial))
                    {
                        if (hit.collider.material != currentSurfaceMaterial)
                        {
                            currentSurfaceMaterial = hit.collider.material;
                            ApplySurfaceMaterial(wheelCollider, hit.collider.material.dynamicFriction);
                        }
                    }
                    else
                    {
                        if(currentSurfaceMaterial != null)
                        {
                            currentSurfaceMaterial = null;
                            ApplySurfaceMaterial(wheelCollider);
                        }
                    }
                }
            }
        }

        private void ApplySurfaceMaterial(WheelCollider wheelCollider, float friction = 1f)
        {
            WheelFrictionCurve forwardFriction = wheelCollider.forwardFriction;
            forwardFriction.stiffness = forwardFriction.stiffness * friction / wheelColliders.Length;
            wheelCollider.forwardFriction = forwardFriction;
            WheelFrictionCurve sidewaysFriction = wheelCollider.sidewaysFriction;
            sidewaysFriction.stiffness = sidewaysFriction.stiffness * friction / wheelColliders.Length;
            wheelCollider.sidewaysFriction = forwardFriction;
        }

        private float GetRPM(float slipThreshhold)
        {
            float rpm = 0f;
            int effectiveColliders = 0;
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                float vehicleForwardSlip = GetSlipForCollider(wheelCollider);
                if (Mathf.Abs(vehicleForwardSlip) < slipThreshhold)
                {
                    rpm += wheelCollider.rpm;
                    effectiveColliders++;
                }
            }
            if (effectiveColliders > 0)
            {
                return rpm / effectiveColliders;
            }
            return 0f;
        }

        private static float GetSlipForCollider(WheelCollider wheelCollider)
        {
            float vehicleForwardSlip = Mathf.Infinity;
            if (wheelCollider.GetGroundHit(out WheelHit hit))
            {
                // Calculate total slip magnitude accounting for steering angle
                float steerAngleRad = wheelCollider.steerAngle * Mathf.Deg2Rad;
                vehicleForwardSlip = hit.forwardSlip * Mathf.Cos(steerAngleRad) - hit.sidewaysSlip * Mathf.Sin(steerAngleRad);

            }

            return vehicleForwardSlip;
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
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                wheelCollider.radius = wheelConfiguration.Radius;
                wheelCollider.mass = wheelConfiguration.Weight / numberOfColliders;
                WheelFrictionCurve forwardFriction = wheelConfiguration.GetForwardFrictionCurve(numberOfColliders);
                wheelCollider.forwardFriction = forwardFriction;
                WheelFrictionCurve sidewaysFriction = wheelConfiguration.GetSidewaysFrictionCurve(numberOfColliders); 
                wheelCollider.sidewaysFriction = sidewaysFriction;
                wheelCollider.suspensionDistance = suspensionConfiguration.Distance;
                wheelCollider.wheelDampingRate = driveTrain.Damping;
                if(IsFront)
                {
                    wheelCollider.suspensionSpring = suspensionConfiguration.GetFrontSuspectionSpring(wheelCollider.suspensionSpring.targetPosition);
                }
                else
                {
                    wheelCollider.suspensionSpring = suspensionConfiguration.GetBackSuspectionSpring(wheelCollider.suspensionSpring.targetPosition);
                }
            }
            UpdateWheelWidth(wheelConfiguration.Width);
        }

        private void ApplyDeflection()
        {
            float verticalForce = 0f;
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                wheelCollider.GetGroundHit(out WheelHit hit);
                verticalForce += hit.force;
            }
            float targetDeflection = VehiclePhysics.GetTireDeflection(verticalForce, wheelConfiguration.RadialTireStiffness);
            float bulge = VehiclePhysics.GetTireDeflection(verticalForce, wheelConfiguration.LateralTireStiffness);
            currentDeflection = Mathf.MoveTowards(currentDeflection, targetDeflection, DEFLECTION_SMOOTH_STEP * Time.fixedDeltaTime);
            float currentWheelRadius = wheelConfiguration.Radius - currentDeflection;
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                wheelCollider.radius = currentWheelRadius;
            }
            float currentWidth = wheelConfiguration.Width + bulge;
            UpdateWheelWidth(currentWidth);
            UpdateTireVisuals(currentWheelRadius, currentWidth);
            UpdateTireFriction(currentDeflection);
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
                forwardFriction.stiffness = (wheelConfiguration.ForwardStiffness / numberOfColliders) * frictionMultiplier;
                wheelCollider.forwardFriction = forwardFriction;

                WheelFrictionCurve sidewaysFriction = wheelCollider.sidewaysFriction;
                sidewaysFriction.stiffness = (wheelConfiguration.SideWaysStiffness / numberOfColliders) * frictionMultiplier;
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
            float increment = (numberOfColliders - 1) * currentWidth;
            for (int i = 0; i < numberOfColliders; i++)
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
            return contactPoint / numberOfColliders;
        }
    }
}