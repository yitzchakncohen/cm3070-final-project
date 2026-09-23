using System;
using ModularVehicleSimulator.Physics;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    [RequireComponent(typeof(Engine), typeof(Brake))]
    public class VehicleController : MonoBehaviour
    {
        private const string CHASSIS_LAYER = "Chassis";
        public event Action OnGearChanged;
        public string Name => vehicleConfiguration.Name;
        public bool IsABSActive => brake != null? brake.IsABSActive : false;
        public bool IsTVBActive => brake != null ? brake.IsTVBActive : false;
        public float Speed => speed;
        public float RPM => engineRPM;
        public float CurrentSteeringAngle => currentTargetSteeringAngle;
        public Gear Gear => (Gear)currentGear;
        public Rigidbody ChassisRigidBody => chassisRigidBody;
        public VehicleConfiguration Config => vehicleConfiguration;
        public ChassisConfiguration Chassis => vehicleConfiguration.Chassis;
        public SteeringConfiguration Steering => vehicleConfiguration.Steering;
        public CameraController CameraController => cameraController;
        public Camera SelectioCamera => cameraController.SelectioCamera;
        [SerializeField] private VehicleConfiguration vehicleConfiguration;
        [SerializeField] private Rigidbody chassisRigidBody;
        [SerializeField] private Transform chassisModel;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private LayerMask groundLayerMask;
        private Wheel[] wheels;
        private Engine engine;
        private Brake brake;
        private int currentGear = 0;
        private float speed = 0f;
        private float engineRPM = 0f;
        private float autoShiftTimer = 0f;
        private float currentTargetSteeringAngle = 0f;

        private void Awake()
        {
            int layer = LayerMask.NameToLayer(CHASSIS_LAYER);
            VehiclePhysics.SetChildrenLayerRecursive(chassisModel, layer);
        }

        private void Start()
        {
            wheels = GetComponentsInChildren<Wheel>();
            if(wheels.Length == 0)
            {
                Debug.LogException(new Exception("[VehicleController] No wheels found in vehicle."));
            }
            engine = GetComponent<Engine>();
            brake = GetComponent<Brake>();
            Collider[] colliders = GetComponentsInChildren<Collider>();
            if(colliders.Length == 0)
            {
                Debug.LogException(new Exception("[VehicleController] No colliders found on vehicle."));
            }
            foreach (Collider collider in colliders)
            {
                if(collider as WheelCollider) continue;
                collider.material = Chassis.Material;
            }
            Init();
        }

        private void Init()
        {
            try
            {
                chassisRigidBody.centerOfMass = vehicleConfiguration.Chassis.CenterOfMass;
                foreach (Wheel wheel in wheels)
                {
                    wheel.Init(vehicleConfiguration.Wheels, 
                            vehicleConfiguration.Steering, 
                            vehicleConfiguration.Suspension,
                            vehicleConfiguration.Chassis,
                            chassisRigidBody,
                            groundLayerMask
                        );
                }
                engine.Init(vehicleConfiguration.Engine, vehicleConfiguration.DriveTrain, wheels);
                brake.Init(wheels, vehicleConfiguration.Brakes, vehicleConfiguration.Engine.Type, vehicleConfiguration.Chassis, chassisRigidBody, vehicleConfiguration.Wheels);
                foreach (AntiRollBar antiRollBar in GetComponentsInChildren<AntiRollBar>())
                {
                    antiRollBar.Init(chassisRigidBody, Steering);                
                }                
            }
            catch (System.Exception e)
            {
                Debug.LogException(new Exception("[VehicleController] Unable to initialize vehicle: " + e.Message));
                throw;
            }
        }

        private void Update()
        {
            CalculateCurrentSpeed();
            UpdateTransmission();
        }

        public void Reset()
        {
            Init();
        }

        public void Steer(float steeringInput)
        {
            UpdateCurrentSteeringAngle(steeringInput);
            VehiclePhysics.GetAckermannSteeringAngles(
                vehicleConfiguration.Chassis.WheelBase,
                vehicleConfiguration.Chassis.Track,
                currentTargetSteeringAngle,
                out float rightSteeringAngle,
                out float leftSteeringAngle
            );
            foreach (Wheel wheel in wheels)
            {
                if (wheel.IsSteerable)
                {
                    wheel.Steer(rightSteeringAngle, leftSteeringAngle);
                }
            }
        }

        public void Accelerate(float accelerationInput, float brakeInput)
        {
            engine.Accelerate(Gear, accelerationInput, brakeInput);
        }

        public void Brake(float brakeInput, float accelerationInput)
        {
            // Parking Break
            if(currentGear == (int)Gear.Park)
            {
                brakeInput = 1f;
            }
            brake.ApplyForce(brakeInput, accelerationInput, currentTargetSteeringAngle);
        }

        public void ToggleCamera()
        {
            cameraController.ToggleCamera();
        }

        public void ShiftGearNext()
        {
            if(!vehicleConfiguration.DriveTrain.ContainsGear(currentGear + 1)) return;
            currentGear = Mathf.Clamp(currentGear + 1, 0, GetMaxGear());
            OnGearChanged?.Invoke();
        }

        public void ShiftGearPrevious()
        {
            if(!vehicleConfiguration.DriveTrain.ContainsGear(currentGear - 1)) return;
            currentGear = Mathf.Clamp(currentGear - 1, 0, GetMaxGear());
            OnGearChanged?.Invoke();
        }

        private void CalculateCurrentSpeed()
        {
            // speed = VehiclePhysics.GetVehicleSpeed(wheels, vehicleConfiguration.Wheels.Radius);
            // if (speed == 0f)
            // {
                speed = chassisRigidBody.linearVelocity.magnitude;
            // }
        }

        private void UpdateCurrentSteeringAngle(float steeringInput)
        {
            float speed = VehiclePhysics.GetVehicleSpeed(wheels, vehicleConfiguration.Wheels.Radius);

            float rawTargetSteeringAngle = VehiclePhysics.GetTargetSteeringAngle(
                steeringInput,
                speed,
                vehicleConfiguration.Steering.HighSpeedThreshold,
                vehicleConfiguration.Steering.MaxSteeringAngleAtRest,
                vehicleConfiguration.Steering.MaxSteeringAngleAtHighSpeed
            );

            currentTargetSteeringAngle = ApplyPowerSteering(rawTargetSteeringAngle, speed);
        }

        private float ApplyPowerSteering(float rawTargetSteeringAngle, float speed)
        {
            float speedFactor = Mathf.InverseLerp(0f, vehicleConfiguration.Steering.HighSpeedThreshold, speed);
            float powerAssist = Mathf.Lerp(vehicleConfiguration.Steering.SteeringAssistMax, vehicleConfiguration.Steering.SteeringAssistMin, speedFactor);
            float turningSpeed = vehicleConfiguration.Steering.SteeringSpeed * powerAssist;
            float targetSteeringAngle = Mathf.MoveTowards(currentTargetSteeringAngle, rawTargetSteeringAngle, turningSpeed * Time.fixedDeltaTime);
            return targetSteeringAngle;
        }

        private void UpdateTransmission()
        {
            engineRPM = currentGear == 0 ? 0 : Mathf.Abs(engine.RPM);
            autoShiftTimer += Time.deltaTime;

            if(!vehicleConfiguration.Engine.IsAutomaticTransmision) return;
            
            if(autoShiftTimer < vehicleConfiguration.Engine.MinAutoShiftTime) return;
 
            if(engineRPM >= vehicleConfiguration.Engine.MaxRPM && currentGear > (int)Gear.Drive)
            {
                if(!vehicleConfiguration.DriveTrain.ContainsGear(currentGear + 1)) return;
                ShiftGearNext();
                autoShiftTimer = 0f;
            }
            else if(engineRPM < vehicleConfiguration.Engine.IdleRPM && currentGear > (int)Gear.Drive)
            {
                ShiftGearPrevious();
                autoShiftTimer = 0f;
            }
        }

        private static int GetMaxGear()
        {
            return Enum.GetValues(typeof(Gear)).Length -1;
        }
    }
}
