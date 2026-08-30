using ModularVehicleSimulator.Physics;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class Brake : MonoBehaviour
    {
        private BrakesConfiguration brakesConfiguration;
        private EngineType engineType;
        private Wheel[] wheels;

        public void Init(Wheel[] wheels, BrakesConfiguration brakesConfiguration, EngineType engineType)
        {
            this.wheels = wheels; 
            this.brakesConfiguration = brakesConfiguration;
            this.engineType = engineType;
        }

        public void ApplyForce(float brakeInput, float throttleInput)
        {
            foreach (Wheel wheel in wheels)
            {
                if(engineType == EngineType.Electric && throttleInput < 0.01f)
                {
                    if(wheel.IsMotorized)
                    {
                        wheel.Brake(1.0f, brakesConfiguration.RegenerativeBrakeTorque);
                    }
                }
                else
                {
                    float brakeTorque = brakesConfiguration.Torque * brakesConfiguration.FrontBias;
                    if (!wheel.IsFront)
                    {
                        brakeTorque = brakesConfiguration.Torque * (1 - brakesConfiguration.FrontBias);
                    }

                    brakeTorque = ApplyABS(wheel, brakeTorque);
                    wheel.Brake(brakeInput, brakeTorque);                    
                }
            }
        }

        private float ApplyABS(Wheel wheel, float brakeTorque)
        {
            if (brakesConfiguration.ABSEnabled)
            {
                float vehicleForwardSlip = wheel.GetAverageForwardSlip();
                if (Mathf.Abs(vehicleForwardSlip) > wheel.GetSlipThreshold(brakesConfiguration.ABSSlipThreshholdMultiplier))
                {
                    brakeTorque = VehiclePhysics.ABSStepFunction(brakeTorque, brakesConfiguration.ABSOscillationSpeed);
                }
            }
            return brakeTorque;
        }
    }
}
