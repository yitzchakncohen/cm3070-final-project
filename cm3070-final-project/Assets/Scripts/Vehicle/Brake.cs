using ModularVehicleSimulator.Physics;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class Brake : MonoBehaviour
    {
        private BrakesConfiguration brakesConfiguration;
        private Wheel[] wheels;

        public void Init(Wheel[] wheels, BrakesConfiguration brakesConfiguration)
        {
            this.wheels = wheels; 
            this.brakesConfiguration = brakesConfiguration;
        }

        public void ApplyForce(float input)
        {
            foreach (Wheel wheel in wheels)
            {
                float brakeTorque = brakesConfiguration.Torque * brakesConfiguration.FrontBias;
                if (!wheel.IsFront)
                {
                    brakeTorque = brakesConfiguration.Torque * (1 - brakesConfiguration.FrontBias);
                }

                brakeTorque = ApplyABS(wheel, brakeTorque);
                wheel.Brake(input, brakeTorque);
            }
        }

        private float ApplyABS(Wheel wheel, float brakeTorque)
        {
            if (brakesConfiguration.ABSEnabled)
            {
                float vehicleForwardSlip = wheel.GetAverageForwardSlip();
                if (Mathf.Abs(vehicleForwardSlip) > brakesConfiguration.ABSSlipThreshhold)
                {
                    brakeTorque = VehiclePhysics.ABSStepFunction(brakeTorque, brakesConfiguration.ABSOscillationSpeed);
                }
            }
            return brakeTorque;
        }
    }
}
