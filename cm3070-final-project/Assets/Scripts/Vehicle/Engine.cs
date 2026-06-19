using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class Engine : MonoBehaviour
    {
        EngineConfiguration engineConfiguration;
        private DriveTrain driveTrain;
        private Wheel[] wheels;

        public void Init(EngineConfiguration engineConfiguration, DriveTrain driveTrain, Wheel[] wheels)
        {
            this.engineConfiguration = engineConfiguration;
            this.driveTrain = driveTrain;
            this.wheels = wheels;
        }

        public void Accelerate(Gear gear, float accelerationInput)
        {
            foreach (Wheel wheel in wheels)
            {
                if(wheel.IsMotorized)
                {
                    float input = GetWheelTorque(gear, accelerationInput);
                    wheel.Accelerate(input);
                }
            }
        }

        private float GetWheelTorque(Gear gear, float input)
        {
            return GetAccelerationInputWithGear(engineConfiguration.Power * input, gear, engineConfiguration.Type);
        }
        
        private float GetAccelerationInputWithGear(float input, Gear gear, EngineType engineType)
        {
            if(engineType == EngineType.Gas)
            {
            }
            else if (engineType == EngineType.Electric)
            {
            }
            return input * driveTrain.GetRatioForGear(gear); 
        }
    }    

    public enum EngineType
    {
        Gas,
        Electric
    }
}
