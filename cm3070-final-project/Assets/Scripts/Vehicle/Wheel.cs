using System.Collections.Generic;
using System.Linq;
using ModularVehicleSimulator.Physics;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    [RequireComponent(typeof(Tire), typeof(Suspension))]
    public class Wheel : MonoBehaviour
    {
        public const float DEFLECTION_SMOOTH_STEP = 0.05f;
        public const float EFFECTIVE_SLIP_THRESHHOLD = 0.15f;
        public const float SPEEDOMETER_SLIP_THRESHHOLD_MULTIPLIER = .50f;
        public const float FX_SLIP_THRESHHOLD_MULTIPLIER = 7.5f;
        public const string TIRES_LAYER = "Tires";
        public Vector3 WheelFriction => GetWheelFrictionVector();
        public Vector3 WheelContactPoint => GetWheelContactPoint();
        public float SteerAngle => steerAngle;
        public bool IsGrounded => isGrounded;
        public bool IsMotorized => isMotorized;
        public bool IsSteerable => isSteerable;
        public bool IsFront => isFront;
        public bool IsLeft => transform.localPosition.x < 0f;
        public bool IsRight => transform.localPosition.x > 0f;
        public float RPM => tire.RPM;
        public float Radius => wheelConfiguration.Radius;
        [SerializeField] private bool isMotorized = true;
        [SerializeField] private bool isSteerable = true;
        [SerializeField] private bool isFront = true;
        [SerializeField] private Transform wheelModel;
        [SerializeField] private Transform tireModel;
        private Tire tire;
        private Suspension suspension;
        private WheelConfiguration wheelConfiguration;
        private SteeringConfiguration steeringConfiguration;
        private SuspensionConfiguration suspensionConfiguration;
        private ChassisConfiguration chassisConfiguration;
        private DriveTrain driveTrain;
        private Rigidbody chassisRigidBody;
        private LayerMask groundLayerMask;
        private PhysicsMaterial surfaceMaterial;
        private RaycastHit lastGroundHit;
        private float rightSteeringAngle = 0f;
        private float leftSteeringAngle = 0f;
        private float steerAngle = 0f;
        private float drivingAngle = 0f;
        private float currentDeflection = 0f;
        private float nominalDeflection = 0.02f;
        private float brakeTorque = 0f;
        private float motorTorque = 0f;
        private float radius;
        private bool isGrounded = false;

        private void Awake()
        {
            tire = GetComponent<Tire>();
            suspension = GetComponent<Suspension>();
            int layer = LayerMask.NameToLayer(TIRES_LAYER);
            VehiclePhysics.SetChildrenLayerRecursive(transform, layer);
        }

        public void Init(WheelConfiguration wheelConfiguration, 
                        SteeringConfiguration steeringConfiguration, 
                        SuspensionConfiguration suspensionConfiguration,
                        ChassisConfiguration chassisConfiguration,
                        DriveTrain driveTrain,
                        Rigidbody chassisRigidBody,
                        LayerMask groundLayerMask)
        {
            this.wheelConfiguration = wheelConfiguration;
            this.steeringConfiguration = steeringConfiguration;
            this.suspensionConfiguration = suspensionConfiguration;
            this.chassisConfiguration = chassisConfiguration;
            this.driveTrain = driveTrain;
            this.groundLayerMask = groundLayerMask;
            nominalDeflection = VehiclePhysics.GetNominalTireDeflection(
                chassisConfiguration.Mass, 
                chassisConfiguration.NumberOfWheels, 
                wheelConfiguration.RadialTireStiffness
            );
            UpdateWheelPositions();
            UpdateTireVisuals(wheelConfiguration.Radius, wheelConfiguration.Width);
            suspension.Init(chassisRigidBody, suspensionConfiguration, wheelConfiguration, chassisConfiguration, IsFront);
            tire.Init(chassisRigidBody, wheelConfiguration);
        }

        private void FixedUpdate()
        {
            lastGroundHit = CheckIsGrounded();
            UpdateSurfaceMaterial();
            ApplyDeflection();
            UpdateWheelAngles();
            Vector3 forceAppPoint = transform.position - (transform.up * GetForceAppPointDistance());
            tire.UpdateFriction(currentDeflection, nominalDeflection, surfaceMaterial ? surfaceMaterial.dynamicFriction : 1.0f);
            tire.ApplyFriction(lastGroundHit, forceAppPoint, SteerAngle, motorTorque, brakeTorque, suspension.NormalLoad, isGrounded);
        }

        public void Steer(float leftSteeringAngle, float rightSteeringAngle)
        {
            this.rightSteeringAngle = rightSteeringAngle;
            this.leftSteeringAngle = leftSteeringAngle;
        }

        public void Accelerate(float motorTorque)
        {
            this.motorTorque = motorTorque;    
        }

        public void Brake(float brakeTorque)
        {
            motorTorque = 0f;
            this.brakeTorque = brakeTorque;            
        }

        public float GetSlipThreshold(float bufferMultiplier)
        {
            WheelFrictionCurve forwardFriction = tire.ForwardFriction;
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
            return tire.ForwardSlip < Mathf.Infinity ? tire.ForwardSlip : 0f;
        }

        public float GetTravel()
        {
            float travel = 0;
            if(isGrounded)
            {
                float currentWheelRadius = wheelConfiguration.Radius - currentDeflection;
                float localY = tire.transform.InverseTransformPoint(lastGroundHit.point).y;
                float compression = (-localY - currentWheelRadius) / suspensionConfiguration.Distance;
                travel = Mathf.Clamp01(compression);                
            }
            return travel;
        }

        private float GetRPM(float slipThreshhold)
        {
            if (Mathf.Abs(tire.ForwardSlip) < slipThreshhold)
            {
                return tire.RPM;
            }
            return 0f;
        }

        private void UpdateSurfaceMaterial()
        {
            if (isGrounded)
            {
                float forceAppPointDistance = GetForceAppPointDistance();
                suspension.ApplySpringDamperForce(lastGroundHit, forceAppPointDistance);
                surfaceMaterial = lastGroundHit.collider.sharedMaterial;
            }
            else
            {
                surfaceMaterial = null;
            }
        }

        private void UpdateWheelAngles()
        {
            float targetAngle = IsLeft ? leftSteeringAngle : rightSteeringAngle;
            steerAngle = targetAngle;
            // Convert RPM to degrees per second.
            drivingAngle = (drivingAngle + RPM * (360f / 60f) * Time.fixedDeltaTime) % 360f;
            wheelModel.position = transform.position - suspension.Offset * transform.up;
            // Use quaternions to ensure rotations do not effect each other.
            wheelModel.localRotation = Quaternion.Euler(0f, steerAngle, 0f) * Quaternion.Euler(drivingAngle, 0f, 0f);
        }

        private void ApplyDeflection()
        {
            float targetDeflection = VehiclePhysics.GetTireDeflection(suspension.NormalLoad, wheelConfiguration.RadialTireStiffness);
            float bulge = VehiclePhysics.GetTireDeflection(suspension.NormalLoad, wheelConfiguration.LateralTireStiffness);
            currentDeflection = Mathf.MoveTowards(currentDeflection, targetDeflection, DEFLECTION_SMOOTH_STEP * Time.fixedDeltaTime);
            float currentWheelRadius = wheelConfiguration.Radius - currentDeflection;
            radius = currentWheelRadius;
            float currentWidth = wheelConfiguration.Width + bulge;
            UpdateTireVisuals(currentWheelRadius, currentWidth);
        }

        private void UpdateTireVisuals(float currentRadius, float currentWidth)
        {
            tireModel.localScale = new Vector3(currentRadius * 2f, currentWidth / 2f, wheelConfiguration.Radius * 2f);
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

        private Vector3 GetWheelFrictionVector()
        {
            Vector3 frictionVector = Vector3.zero;
            if(isGrounded)
            {
                float forwardFriction = VehiclePhysics.GetForwardFriction(tire.ForwardFriction, tire.ForwardSlip, suspension.NormalLoad);
                float sidewaysFriction = VehiclePhysics.GetSidewaysFriction(tire.SidewaysFriction, tire.SidewaysSlip, suspension.NormalLoad);
                frictionVector += (lastGroundHit.transform.forward * forwardFriction) + (lastGroundHit.transform.right * sidewaysFriction);                
            }
            return frictionVector;
        }

        private Vector3 GetWheelContactPoint()
        {
            Vector3 contactPoint = Vector3.zero;
            if(isGrounded) contactPoint = lastGroundHit.point;
            return contactPoint;
        }

        private float GetForceAppPointDistance()
        {
            if(chassisConfiguration == null || chassisRigidBody == null) return 0f;
            Vector3 wheelLocalPosition = chassisRigidBody.transform.InverseTransformPoint(transform.position);
            float wheelOffsetFromGround = wheelConfiguration.Radius;
            float offsetFromGroundToCenterOfMass = chassisConfiguration.CenterOfMass.y - wheelLocalPosition.y + wheelOffsetFromGround;
            float offsetDistance = offsetFromGroundToCenterOfMass - suspensionConfiguration.ForceAppPointOffset;
            return Mathf.Max(0f, offsetDistance);
        }

        private RaycastHit CheckIsGrounded()
        {
            // Offset the origin upwards to keep the cast start point above ground level
            float raycastOffset = wheelConfiguration.Radius * 2.0f;
            Vector3 origin = transform.position + (transform.up * raycastOffset);
            float maxDistance = suspensionConfiguration.Distance + raycastOffset;

            isGrounded = UnityEngine.Physics.SphereCast(
                origin,
                wheelConfiguration.Radius,
                -transform.up,
                out RaycastHit hit,
                maxDistance,
                groundLayerMask
            );

            if (isGrounded)
            {
                // Correct distance to account for the raised origin
                hit.distance = Mathf.Max(0f, hit.distance - raycastOffset);
            }

            return hit;
        }
    }
}