using System.Linq;
using ModularVehicleSimulator.Physics;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class Brake : MonoBehaviour
    {
        private const float REGENERATIVE_BRAKING_CUTOFF_KMH = 5f;
        private BrakesConfiguration brakesConfiguration;
        private EngineType engineType;
        private Wheel[] wheels;
        private int frontWheelCount = 2;
        private int backWheelCount = 2;
        private int motorizedWheelCount = 2;

        public void Init(Wheel[] wheels, BrakesConfiguration brakesConfiguration, EngineType engineType)
        {
            this.wheels = wheels; 
            this.brakesConfiguration = brakesConfiguration;
            this.engineType = engineType;
            frontWheelCount = wheels.Count(wheel => wheel.IsFront);
            backWheelCount = wheels.Count(wheel => !wheel.IsFront);
            motorizedWheelCount = wheels.Count(wheel => wheel.IsMotorized);
        }

        public void ApplyForce(float brakeInput, float throttleInput)
        {
            foreach (Wheel wheel in wheels)
            {
                if(engineType == EngineType.Electric && brakeInput < 0.01f && throttleInput < 0.01f)
                {
                    float regenerativeBrakeTorque = Mathf.Clamp01(wheel.GetSpeedometerRPM() * VehiclePhysics.RPM_TO_METERS_PER_SECOND / REGENERATIVE_BRAKING_CUTOFF_KMH) * brakesConfiguration.RegenerativeBrakeTorque;
                    float brakeTorque = ApplyABS(wheel, regenerativeBrakeTorque / motorizedWheelCount);
                    if(wheel.IsMotorized)
                    {
                        wheel.Brake(1.0f, brakeTorque);
                    }
                }
                else
                {
                    float brakeTorque = brakesConfiguration.Torque * brakesConfiguration.FrontBias / frontWheelCount;
                    if (!wheel.IsFront)
                    {
                        brakeTorque = brakesConfiguration.Torque * (1 - brakesConfiguration.FrontBias) / backWheelCount;
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
