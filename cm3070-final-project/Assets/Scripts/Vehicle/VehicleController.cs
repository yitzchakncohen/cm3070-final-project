using System;
using System.Linq;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class VehicleController : MonoBehaviour
    {
        private const float RPM_TO_METERS_PER_SECOND = (2f * Mathf.PI) / 60f;
        public float Speed => speed;
        public Gear Gear => (Gear)currentGear;
        public Rigidbody ChassisRigidBody => chassisRigidBody;
        public event Action OnGearChanged;
        [SerializeField] private VehicleConfiguration vehicleConfiguration;
        [SerializeField] private Rigidbody chassisRigidBody;
        private Wheel[] wheels;
        private Engine engine;
        private int currentGear = 0;
        private float speed = 0f;

        private void Start()
        {
            wheels = GetComponentsInChildren<Wheel>();
            foreach (Wheel wheel in wheels)
            {
                wheel.Init(vehicleConfiguration.Wheels, 
                        vehicleConfiguration.Brakes, 
                        vehicleConfiguration.Steering, 
                        vehicleConfiguration.Suspension,
                        vehicleConfiguration.Chassis,
                        vehicleConfiguration.DriveTrain
                    );
            }
            engine = GetComponent<Engine>();
            engine.Init(vehicleConfiguration.Engine, vehicleConfiguration.DriveTrain, wheels);
            chassisRigidBody.centerOfMass = vehicleConfiguration.Chassis.CenterOfMass;
        }

        private void Update()
        {
            float rpm = Mathf.Abs(wheels.Where(wheel => wheel.IsMotorized).Average(wheel => wheel.GetEffectiveRPM()));
            speed = rpm * vehicleConfiguration.Wheels.Radius * RPM_TO_METERS_PER_SECOND; 
            Debug.Log($"Speed[m/s]: {speed} [km/h] {speed * 3.6f}");
        }

        public void Steer(float steeringInput)
        {
            foreach (Wheel wheel in wheels)
            {
                if(wheel.IsSteerable)
                {
                    wheel.Steer(steeringInput, speed);
                }
            }
        }

        public void Accelerate(float accelerationInput)
        {
            engine.Accelerate(Gear, accelerationInput);
        }

        public void Brake(float brakeInput)
        {
            foreach (Wheel wheel in wheels)
            {
                wheel.Brake(brakeInput);
            }
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

        private float GetAccelerationInputWithGear(float input)
        {
            switch (Gear)
            {
                case Gear.Park:
                    return 0;
                case Gear.Reverse:
                    return - input;
                case Gear.Drive:
                default:
                    return input;
            }
        }

        private static int GetMaxGear()
        {
            return Enum.GetValues(typeof(Gear)).Length -1;
        }
    }
}
