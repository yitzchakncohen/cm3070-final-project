using System.Collections.Generic;
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
        public const float SPEEDOMETER_SLIP_THRESHHOLD_MULTIPLIER = .70f;
        public const float FX_SLIP_THRESHHOLD_MULTIPLIER = 7.0f;
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
        private Rigidbody chassisRigidBody;
        private Dictionary<WheelCollider, PhysicsMaterial> currentSurfaceMaterials = new Dictionary<WheelCollider, PhysicsMaterial>();
        private float rightSteeringAngle = 0f;
        private float leftSteeringAngle = 0f;
        private float currentDeflection = 0f;
        private float nominalDeflection = 0.02f;

        private void Awake()
        {
            wheelColliders = GetComponentsInChildren<WheelCollider>();
        }

        public void Init(WheelConfiguration wheels, 
                        SteeringConfiguration steering, 
                        SuspensionConfiguration suspension,
                        ChassisConfiguration chassis,
                        DriveTrain driveTrain,
                        Rigidbody chassisRigidBody)
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
            this.chassisRigidBody = chassisRigidBody;
            ApplyWheelPhysicsParamters();
            UpdateWheelPositions();
            UpdateTireVisuals(wheelConfiguration.Radius, wheelConfiguration.Width);
        }

        private void FixedUpdate()
        {
            ApplyDeflection();
            UpdateSurfaceMaterial();
            UpdateTireFriction(currentDeflection);
            UpdateWheelAngles();
        }

        public void Steer(float leftSteeringAngle, float rightSteeringAngle)
        {
            this.rightSteeringAngle = rightSteeringAngle;
            this.leftSteeringAngle = leftSteeringAngle;
        }

        public void Accelerate(float torque, float brakeTorque = 0f)
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

        public float GetSlipThreshold(float bufferMultiplier)
        {
            WheelFrictionCurve forwardFriction = wheelColliders[0].forwardFriction;
            return forwardFriction.extremumSlip * bufferMultiplier;
        }

        public float GetEffectiveRPM()
        {
            return GetRPM(EFFECTIVE_SLIP_THRESHHOLD);
        }

        public float GetSpeedometerRPM()
        {
            float slipThreshold = GetSlipThreshold(SPEEDOMETER_SLIP_THRESHHOLD_MULTIPLIER);
            return GetRPM(slipThreshold);
        }

        public float GetAverageForwardSlip()
        {
            float slip = 0f;
            int colliders = 0;
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                float colliderSlip = GetForwardSlipForCollider(wheelCollider);
                if(colliderSlip < Mathf.Infinity)
                {
                    colliders++;
                    slip += colliderSlip;
                }
            }
            return slip / colliders;
        }

        public float GetAverageSidewaysSlip()
        {
            float slip = 0f;
            int colliders = 0;
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                float colliderSlip = GetSidewaysSlipForCollider(wheelCollider);
                if(colliderSlip < Mathf.Infinity)
                {
                    colliders++;
                    slip += colliderSlip;
                }
            }
            return slip / colliders;
        }

        public float GetTravel()
        {
            float travel = 0;
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                if(wheelCollider.GetGroundHit(out WheelHit hit))
                {
                    float localY = wheelCollider.transform.InverseTransformPoint(hit.point).y;
                    float compression = (-localY - wheelCollider.radius) / wheelCollider.suspensionDistance;
                    travel += Mathf.Clamp01(compression);
                }
            }
            return travel / wheelColliders.Count();
        }

        public bool IsGrounded()
        {
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                if(wheelCollider.GetGroundHit(out WheelHit hit))
                {
                    return true;
                }
            }
            return false;
        }

        private float GetRPM(float slipThreshhold)
        {
            float rpm = 0f;
            int effectiveColliders = 0;
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                float vehicleForwardSlip = GetForwardSlipForCollider(wheelCollider);
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

        private static float GetForwardSlipForCollider(WheelCollider wheelCollider)
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

        private static float GetSidewaysSlipForCollider(WheelCollider wheelCollider)
        {
            float vehicleSidewaysSlip = Mathf.Infinity;
            if (wheelCollider.GetGroundHit(out WheelHit hit))
            {
                // Calculate total slip magnitude accounting for steering angle
                float steerAngleRad = wheelCollider.steerAngle * Mathf.Deg2Rad;
                vehicleSidewaysSlip = hit.sidewaysSlip * Mathf.Cos(steerAngleRad) + hit.forwardSlip * Mathf.Sin(steerAngleRad);
            }

            return vehicleSidewaysSlip;
        }

        private void UpdateWheelAngles()
        {
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                if (IsLeft)
                {
                    wheelCollider.steerAngle = Mathf.MoveTowards(wheelCollider.steerAngle, leftSteeringAngle, steeringConfiguration.SteeringSpeed * Time.fixedDeltaTime);
                }
                else
                {
                    wheelCollider.steerAngle = Mathf.MoveTowards(wheelCollider.steerAngle, rightSteeringAngle, steeringConfiguration.SteeringSpeed * Time.fixedDeltaTime);
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
                WheelFrictionCurve forwardFriction = wheelConfiguration.GetDefaultForwardFrictionCurve();
                wheelCollider.forwardFriction = forwardFriction;
                WheelFrictionCurve sidewaysFriction = wheelConfiguration.GetDefaultSidewaysFrictionCurve(); 
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
                wheelCollider.forceAppPointDistance = GetForceAppPointDistance();
            }
            UpdateWheelWidth(wheelConfiguration.Width);
        }

        private float GetForceAppPointDistance()
        {
            Vector3 wheelLocalPosition = chassisRigidBody.transform.InverseTransformPoint(transform.position);
            float wheelOffsetFromGround = wheelConfiguration.Radius;
            float offsetFromGroundToCenterOfMass = chassisConfiguration.CenterOfMass.y - wheelLocalPosition.y + wheelOffsetFromGround;
            float offsetDistance = offsetFromGroundToCenterOfMass - suspensionConfiguration.ForceAppPointOffset;
            return Mathf.Max(0f, offsetDistance);
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
                wheelCollider.forceAppPointDistance = GetForceAppPointDistance();
            }
            float currentWidth = wheelConfiguration.Width + bulge;
            UpdateWheelWidth(currentWidth);
            UpdateTireVisuals(currentWheelRadius, currentWidth);
        }

        private void UpdateSurfaceMaterial()
        {
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                // Setup dictionary. 
                if (!currentSurfaceMaterials.ContainsKey(wheelCollider))
                {
                    currentSurfaceMaterials.Add(wheelCollider, null);
                }

                WheelHit hit;
                if (wheelCollider.GetGroundHit(out hit))
                {
                    if (hit.collider.material.GetType() == typeof(PhysicsMaterial))
                    {
                        if (hit.collider.material != currentSurfaceMaterials[wheelCollider])
                        {
                            currentSurfaceMaterials[wheelCollider] = hit.collider.material;
                        }
                    }
                    else
                    {
                        if(currentSurfaceMaterials[wheelCollider] != null)
                        {
                            currentSurfaceMaterials[wheelCollider] = null;
                        }
                    }
                }
            }
        }

        private void UpdateTireVisuals(float currentRadius, float currentWidth)
        {
            tireModel.localScale = new Vector3(currentRadius * 2f, currentWidth / 2f, wheelConfiguration.Radius * 2f);
        }

        private void UpdateTireFriction(float deflection)
        {
            float frictionMultiplier = 1f + (deflection-nominalDeflection)/nominalDeflection * wheelConfiguration.DeflectionGrip;
            float temperature = Weather.Instance? Weather.Instance.Temperature : 20f;
            RoadSurfaceCondition roadSurfaceCondition = Weather.Instance? Weather.Instance.RoadSurfaceCondition : RoadSurfaceCondition.None;
            float forwardWeatherMultiplier = wheelConfiguration.GetForwardWeatherFrictionMultiplier(temperature, roadSurfaceCondition);
            float sidewaysWeatherMultiplier = wheelConfiguration.GetSidewaysWeatherFrictionMultiplier(temperature, roadSurfaceCondition);
            WheelFrictionCurve defaultForwardFriction = wheelConfiguration.GetDefaultForwardFrictionCurve();
            WheelFrictionCurve defaultSidewaysFriction = wheelConfiguration.GetDefaultSidewaysFrictionCurve();
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                float surfaceFriction = currentSurfaceMaterials[wheelCollider] ? currentSurfaceMaterials[wheelCollider].dynamicFriction : 1f;

                WheelFrictionCurve forwardFriction = wheelCollider.forwardFriction;
                forwardFriction.stiffness = defaultForwardFriction.stiffness;
                forwardFriction.extremumValue = defaultForwardFriction.extremumValue * surfaceFriction * frictionMultiplier * forwardWeatherMultiplier / numberOfColliders;
                forwardFriction.asymptoteValue = defaultForwardFriction.asymptoteValue * surfaceFriction * frictionMultiplier * forwardWeatherMultiplier / numberOfColliders;
                wheelCollider.forwardFriction = forwardFriction;

                WheelFrictionCurve sidewaysFriction = wheelCollider.sidewaysFriction;
                sidewaysFriction.stiffness = defaultSidewaysFriction.stiffness;
                sidewaysFriction.extremumValue = defaultSidewaysFriction.extremumValue * surfaceFriction * frictionMultiplier * sidewaysWeatherMultiplier / numberOfColliders;
                sidewaysFriction.asymptoteValue = defaultSidewaysFriction.asymptoteValue * surfaceFriction * frictionMultiplier * sidewaysWeatherMultiplier / numberOfColliders;
                wheelCollider.sidewaysFriction = sidewaysFriction;
            }
        }

        private void UpdateWheelPositions()
        {
            float wheelXPosition = chassisConfiguration.Track/2f;
            float wheelZPosition = chassisConfiguration.WheelBase/2f;
            float wheelYPosition = chassisConfiguration.GroundClearance;
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