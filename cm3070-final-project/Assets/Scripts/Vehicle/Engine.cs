using System.Linq;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class Engine : MonoBehaviour
    {
        private EngineConfiguration engineConfiguration;
        private DriveTrain driveTrain;
        private Wheel[] wheels;
        private float numberOfMotorizedWheels = 0;

        public void Init(EngineConfiguration engineConfiguration, DriveTrain driveTrain, Wheel[] wheels)
        {
            this.engineConfiguration = engineConfiguration;
            this.driveTrain = driveTrain;
            this.wheels = wheels;
            numberOfMotorizedWheels = wheels.Select(wheel => wheel.IsMotorized).Count();
        }

        public void Accelerate(Gear gear, float accelerationInput)
        {
            foreach (Wheel wheel in wheels)
            {
                if(wheel.IsMotorized)
                {
                    float input = GetWheelTorque(gear, accelerationInput, wheel) / numberOfMotorizedWheels;
                    wheel.Accelerate(input);
                }
            }
        }

        private float GetWheelTorque(Gear gear, float input, Wheel wheel)
        {
            float torque = engineConfiguration.GetTorque(wheel.RPM);
            return GetWheelTorqueForGear(torque * input, gear, engineConfiguration.Type);
        }
        
        private float GetWheelTorqueForGear(float engineTorque, Gear gear, EngineType engineType)
        {
            if(engineType == EngineType.Gas)
            {
            }
            else if (engineType == EngineType.Electric)
            {
            }
            return engineTorque * driveTrain.GetRatioForGear(gear); 
        }
    }    

    public enum EngineType
    {
        Gas,
        Electric
    }
}
