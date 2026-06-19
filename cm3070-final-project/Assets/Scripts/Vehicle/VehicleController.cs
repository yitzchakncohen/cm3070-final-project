using System;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class VehicleController : MonoBehaviour
    {
        public Gear Gear => (Gear)currentGear;
        public event Action OnGearChanged;
        [SerializeField] private VehicleConfiguration vehicleConfiguration;
        [SerializeField] private Rigidbody chassisRigidBody;
        private Wheel[] wheels;
        private Engine engine;
        private int currentGear = 0;

        private void Start()
        {
            wheels = GetComponentsInChildren<Wheel>();
            foreach (Wheel wheel in wheels)
            {
                wheel.Init(vehicleConfiguration.Wheels, 
                        vehicleConfiguration.Brakes, 
                        vehicleConfiguration.Steering, 
                        vehicleConfiguration.Suspension,
                        vehicleConfiguration.DriveTrain
                    );
            }
            engine = GetComponent<Engine>();
            engine.Init(vehicleConfiguration.Engine, vehicleConfiguration.DriveTrain, wheels);
        }

        public void Steer(float steeringInput)
        {
            foreach (Wheel wheel in wheels)
            {
                if(wheel.IsSteerable)
                {
                    float speed = chassisRigidBody.linearVelocity.magnitude;
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
