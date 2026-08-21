using System.Collections.Generic;
using System.Linq;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class Engine : MonoBehaviour
    {
        public float RPM => currentEngineRPM;
        public float RPMIdle => engineConfiguration.IdleRPM;
        public float RPMMax => engineConfiguration.MaxRPM;
        private const float RAD_SEC_TO_RPM = 60f / (2f * Mathf.PI);
        private const float IDLE_COMPENSATION_MAX = 0.3f;
        private const float IDLE_FLOOR_FACTOR = 0.8f;
        private EngineConfiguration engineConfiguration;
        private DriveTrain driveTrain;
        private Wheel[] wheels;
        private List<Wheel> motorizedWheels;
        private float currentEngineRPM = 0f;

        public void Init(EngineConfiguration engineConfiguration, DriveTrain driveTrain, Wheel[] wheels)
        {
            this.engineConfiguration = engineConfiguration;
            this.driveTrain = driveTrain;
            this.wheels = wheels;
            motorizedWheels = wheels.Where(wheel => wheel.IsMotorized).ToList();
            currentEngineRPM = engineConfiguration.IdleRPM;
        }

        public void Accelerate(Gear gear, float accelerationInput)
        {
            float engineInputRPM = motorizedWheels.Average(wheel => wheel.GetEffectiveRPM()) * driveTrain.GetRatioForGear(gear);
            float totalTorque = GetWheelTorque(gear, accelerationInput, engineInputRPM);
            float totalRMP = motorizedWheels.Sum(wheel => wheel.GetEffectiveRPM());

            // Apply the engine torque or braking to the wheels
            if(accelerationInput > 0.01f || currentEngineRPM > Mathf.Abs(engineInputRPM))
            {
                foreach (Wheel wheel in motorizedWheels)
                {
                    float wheelTorque = ApplyOpenDifferential(totalTorque, wheel.GetEffectiveRPM(), totalRMP);
                    Debug.Log($"wheelTorque {wheelTorque}");
                    wheel.Accelerate(wheelTorque, 0f);
                }                
            }
            else
            {
                foreach (Wheel wheel in motorizedWheels)
                {
                    float wheelTorque = ApplyOpenDifferential(totalTorque, wheel.GetEffectiveRPM(), totalRMP);
                    wheel.Accelerate(0f, Mathf.Abs(wheelTorque));
                } 
            }
        }

        private float ApplyOpenDifferential(float inputTorque, float wheelRPM, float totalRMP)
        {
            totalRMP = Mathf.Max(0.001f, Mathf.Abs(totalRMP));
            wheelRPM = Mathf.Abs(wheelRPM);
            return inputTorque * (wheelRPM / totalRMP);
        }

        private float GetWheelTorque(Gear gear, float input, float engineInputRPM)
        {
            if(gear == Gear.Park || gear == Gear.Neutral) return 0f;

            // Correct input for idle engine rpm
            float idleDelta = Mathf.Max(engineConfiguration.IdleRPM - currentEngineRPM, 0f);
            float idleCompensation = Mathf.Min(idleDelta / engineConfiguration.IdleRPM, IDLE_COMPENSATION_MAX);
            float effectiveInput = Mathf.Max(idleCompensation, input);

            // Calculate Engine Torque
            float engineTorque = engineConfiguration.GetTorque(currentEngineRPM) * effectiveInput;
            float engineFriction = engineConfiguration.GetFriction(currentEngineRPM);
            float netEngineTorque = engineTorque - engineFriction;

            // Calculate Wheel Torque 
            float rpmDelta = currentEngineRPM - engineInputRPM;
            float effectiveRigidity = driveTrain.Rigidity * Mathf.Abs(driveTrain.GetRatioForGear(gear));
            float torqueFromWheels = rpmDelta * effectiveRigidity * Time.fixedDeltaTime;

            // Calcultae Engine Momentum
            float netTorque = netEngineTorque - torqueFromWheels;
            float angularAcceleration = netTorque / engineConfiguration.Inertia;

            // Update the engine RPM
            currentEngineRPM += angularAcceleration * Time.fixedDeltaTime * RAD_SEC_TO_RPM;
            currentEngineRPM = Mathf.Clamp(currentEngineRPM, engineConfiguration.IdleRPM * IDLE_FLOOR_FACTOR, engineConfiguration.MaxRPM);

            // Output engine torque through the drive train to the wheels
            if (input > 0.01f || currentEngineRPM > Mathf.Abs(engineInputRPM))
            {
                // Combustion or idle momentum applies force to the wheels
                return netEngineTorque * driveTrain.GetRatioForGear(gear) * driveTrain.Loss;
            }
            else
            {
                // Engine braking applies force to the wheels
                return torqueFromWheels * driveTrain.GetRatioForGear(gear) * driveTrain.Loss;
            }
        }
    }    

    public enum EngineType
    {
        Gas,
        Electric
    }
}
