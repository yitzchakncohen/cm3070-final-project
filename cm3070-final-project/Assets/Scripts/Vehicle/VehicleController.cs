using System;
using System.Linq;
using ModularVehicleSimulator.Physics;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    [RequireComponent(typeof(Engine), typeof(Brake))]
    public class VehicleController : MonoBehaviour
    {
        public event Action OnGearChanged;
        public string Name => vehicleConfiguration.Name;
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
        [SerializeField] private CameraController cameraController;
        private Wheel[] wheels;
        private Engine engine;
        private Brake brake;
        private int currentGear = 0;
        private float speed = 0f;
        private float engineRPM = 0f;
        private float autoShiftTimer = 0f;
        private float currentTargetSteeringAngle = 0f;

        private void Start()
        {
            wheels = GetComponentsInChildren<Wheel>();
            foreach (Wheel wheel in wheels)
            {
                wheel.Init(vehicleConfiguration.Wheels, 
                        vehicleConfiguration.Steering, 
                        vehicleConfiguration.Suspension,
                        vehicleConfiguration.Chassis,
                        vehicleConfiguration.DriveTrain,
                        chassisRigidBody
                    );
            }
            engine = GetComponent<Engine>();
            engine.Init(vehicleConfiguration.Engine, vehicleConfiguration.DriveTrain, wheels);
            chassisRigidBody.centerOfMass = vehicleConfiguration.Chassis.CenterOfMass;
            brake = GetComponent<Brake>();
            brake.Init(wheels, vehicleConfiguration.Brakes, vehicleConfiguration.Engine.Type, vehicleConfiguration.Chassis, chassisRigidBody, vehicleConfiguration.Wheels);
            foreach (AntiRollBar antiRollBar in GetComponentsInChildren<AntiRollBar>())
            {
                antiRollBar.Init(chassisRigidBody, Steering);                
            }
            foreach (Collider collider in GetComponentsInChildren<Collider>())
            {
                if(collider as WheelCollider) continue;
                collider.material = Chassis.Material;
            }
        }

        private void Update()
        {
            CalculateCurrentSpeed();
            UpdateTransmission();
        }

        private void FixedUpdate()
        {
            // Parking Break
            if(currentGear == (int)Gear.Park)
            {
                Brake(1f, 0f);
            }
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

        public void Accelerate(float accelerationInput)
        {
            engine.Accelerate(Gear, accelerationInput);
        }

        public void Brake(float brakeInput, float accelerationInput)
        {
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
            speed = VehiclePhysics.GetVehicleSpeed(wheels, vehicleConfiguration.Wheels.Radius);
            if (speed == 0f)
            {
                speed = chassisRigidBody.linearVelocity.magnitude;
            }
            // Debug.Log($"Speed[m/s]: {speed} [km/h] {speed * 3.6f} velocity {chassisRigidBody.linearVelocity.magnitude * 3.6f}");
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
            engineRPM = engine.RPM;
            autoShiftTimer += Time.deltaTime;

            if(!vehicleConfiguration.Engine.IsAutomaticTransmision) return;
            
            if(autoShiftTimer < vehicleConfiguration.Engine.MinAutoShiftTime) return;
 
            if(engineRPM >= vehicleConfiguration.Engine.MaxRPM && currentGear > 2)
            {
                if(!vehicleConfiguration.DriveTrain.ContainsGear(currentGear + 1)) return;
                ShiftGearNext();
                autoShiftTimer = 0f;
            }
            else if(engineRPM < vehicleConfiguration.Engine.IdleRPM && currentGear > 2)
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
