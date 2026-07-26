using System;
using System.Linq;
using ModularVehicleSimulator.Physics;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class VehicleController : MonoBehaviour
    {
        private const float RPM_TO_METERS_PER_SECOND = (2f * Mathf.PI) / 60f;
        public float Speed => speed;
        public float RPM => engineRPM;
        public float CurrentSteeringAngle => currentTargetSteeringAngle;
        public Gear Gear => (Gear)currentGear;
        public Rigidbody ChassisRigidBody => chassisRigidBody;
        public event Action OnGearChanged;
        public ChassisConfiguration Chassis => vehicleConfiguration.Chassis;
        public SteeringConfiguration Steering => vehicleConfiguration.Steering;
        [SerializeField] private VehicleConfiguration vehicleConfiguration;
        [SerializeField] private Rigidbody chassisRigidBody;
        private Wheel[] wheels;
        private Engine engine;
        private Brake brake;
        private int currentGear = -1;
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
                        vehicleConfiguration.DriveTrain
                    );
            }
            engine = GetComponent<Engine>();
            engine.Init(vehicleConfiguration.Engine, vehicleConfiguration.DriveTrain, wheels);
            chassisRigidBody.centerOfMass = vehicleConfiguration.Chassis.CenterOfMass;
            brake = GetComponent<Brake>();
            brake.Init(wheels, vehicleConfiguration.Brakes);
            foreach (AntiRollBar antiRollBar in GetComponentsInChildren<AntiRollBar>())
            {
                antiRollBar.Init(chassisRigidBody, Steering);                
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
                Brake(1f);
            }
        }

        public void Steer(float steeringInput)
        {
            float rpm = Mathf.Abs(wheels.Where(wheel => wheel.IsMotorized).Average(wheel => wheel.GetEffectiveRPM()));
            float speed = rpm * vehicleConfiguration.Wheels.Radius * RPM_TO_METERS_PER_SECOND;

            currentTargetSteeringAngle = VehiclePhysics.GetTargetSteeringAngle(
                steeringInput,
                speed,
                vehicleConfiguration.Steering.HighSpeedThreshold,
                vehicleConfiguration.Steering.MaxSteeringAngleAtRest,
                vehicleConfiguration.Steering.MaxSteeringAngleAtHighSpeed
            );
            VehiclePhysics.GetAckermannSteeringAngles(
                vehicleConfiguration.Chassis.WheelBase,
                vehicleConfiguration.Chassis.Track,
                currentTargetSteeringAngle,
                out float rightSteeringAngle,
                out float leftSteeringAngle
            );
            foreach (Wheel wheel in wheels)
            {
                if(wheel.IsSteerable)
                {
                    wheel.Steer(rightSteeringAngle, leftSteeringAngle);
                }
            }
        }

        public void Accelerate(float accelerationInput)
        {
            engine.Accelerate(Gear, accelerationInput);
        }

        public void Brake(float brakeInput)
        {
            brake.ApplyForce(brakeInput);
        }

        public void ShiftGearNext()
        {
            currentGear = Mathf.Clamp(currentGear + 1, -1, GetMaxGear());
            Debug.Log("Gear: " + Gear.ToString());
            OnGearChanged?.Invoke();
        }

        public void ShiftGearPrevious()
        {
            currentGear = Mathf.Clamp(currentGear - 1, -1, GetMaxGear());
            OnGearChanged?.Invoke();
        }

        private void CalculateCurrentSpeed()
        {
            float wheelRPM = Mathf.Abs(wheels.Where(wheel => wheel.IsMotorized).Average(wheel => wheel.GetSpeedometerRPM()));
            speed = wheelRPM * vehicleConfiguration.Wheels.Radius * RPM_TO_METERS_PER_SECOND;
            if (speed == 0f)
            {
                speed = chassisRigidBody.linearVelocity.magnitude;
            }
            Debug.Log($"Speed[m/s]: {speed} [km/h] {speed * 3.6f} velocity {chassisRigidBody.linearVelocity.magnitude * 3.6f}");
        }

        private void UpdateTransmission()
        {
            engineRPM = engine.RPM;
            autoShiftTimer += Time.deltaTime;

            if(!vehicleConfiguration.Engine.IsAutomaticTransmision) return;
            
            if(autoShiftTimer < vehicleConfiguration.Engine.MinAutoShiftTime) return;
 
            if(engineRPM >= vehicleConfiguration.Engine.MaxRPM && currentGear > 2)
            {
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
