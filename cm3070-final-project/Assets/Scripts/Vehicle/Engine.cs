using System;
using System.Collections.Generic;
using System.Linq;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class Engine : MonoBehaviour
    {
        public float RPM => engineConfiguration.Type == EngineType.Gas ? currentEngineRPM : lastEngineRPM;
        public float RPMIdle => engineConfiguration.IdleRPM;
        public float RPMMax => engineConfiguration.MaxRPM;
        private const float RAD_SEC_TO_RPM = 60f / (2f * Mathf.PI);
        private const float IDLE_FLOOR_FACTOR = 0.8f;
        private const float MAX_TORQUE_FROM_WHEELS_DELTA = 1000f; // [N*m]
        private const float MAX_ENGINE_RPM_DELTA_PER_SECOND = 5000f; // [RPM]
        private const int ENGINE_SUBSTEPS = 10;
        private EngineConfiguration engineConfiguration;
        private DriveTrain driveTrain;
        private Wheel[] wheels;
        private List<Wheel> motorizedWheels;
        private float currentEngineRPM = 0f;
        private float lastEngineRPM = 0f;
        private float smoothedEngineRPM = 0f;
        private float torqueFromWheels = 0f;

        public void Init(EngineConfiguration engineConfiguration, DriveTrain driveTrain, Wheel[] wheels)
        {
            this.engineConfiguration = engineConfiguration;
            this.driveTrain = driveTrain;
            this.wheels = wheels;
            if(wheels.Length == 0)
            {
                Debug.LogException(new Exception("[Brake] No wheels found"));
            }
            motorizedWheels = wheels.Where(wheel => wheel.IsMotorized).ToList();
            currentEngineRPM = engineConfiguration.IdleRPM;
        }

        public void Accelerate(Gear gear, float accelerationInput, float brakeInput)
        {
            float substepDT = Time.fixedDeltaTime / ENGINE_SUBSTEPS;
            float totalTorque = 0f;
            float targetEngineRPM = motorizedWheels.Average(wheel => wheel.GetEffectiveRPM()) * driveTrain.GetRatioForGear(gear);
            smoothedEngineRPM = Mathf.MoveTowards(smoothedEngineRPM, targetEngineRPM, MAX_ENGINE_RPM_DELTA_PER_SECOND * Time.fixedDeltaTime);

            for (int i = 0; i < ENGINE_SUBSTEPS; i++)
            {
                float t = (float)(i + 1) / ENGINE_SUBSTEPS;
                float estimatedEngineRPM = Mathf.Lerp(lastEngineRPM, smoothedEngineRPM, t);
                totalTorque = GetWheelTorque(gear, accelerationInput, brakeInput, estimatedEngineRPM, substepDT);           
            }

            // Apply the engine torque or braking to the wheels
            foreach (Wheel wheel in motorizedWheels)
            {
                float wheelTorque = ApplyOpenDifferential(totalTorque, motorizedWheels.Count);
                wheel.Accelerate(wheelTorque);
            }               

            lastEngineRPM = smoothedEngineRPM;
        }

        private float ApplyOpenDifferential(float inputTorque, int numberOfWheels)
        {
            return inputTorque / numberOfWheels;
        }

        private float GetWheelTorque(Gear gear, float throttleInput, float brakeInput, float engineInputRPM, float substepDT)
        {
            if(gear == Gear.Park || gear == Gear.Neutral) return 0f;

            // Calculate Engine Torque
            float netEngineTorque = 0f;
            float idleDelta = Mathf.Max(engineConfiguration.IdleRPM - currentEngineRPM, 0f);
            float idleCompensation = Mathf.Min(idleDelta / engineConfiguration.IdleRPM, engineConfiguration.IdleCompensation);
            float effectiveInput = Mathf.Max(idleCompensation, throttleInput);
            float engineTorque = engineConfiguration.GetTorque(currentEngineRPM) * effectiveInput;
            float gearDirection = Mathf.Sign(driveTrain.GetRatioForGear(gear));
            
            if(engineConfiguration.Type == EngineType.Gas)
            {
                float engineFriction = engineConfiguration.GetFriction(currentEngineRPM);
                netEngineTorque = engineTorque - engineFriction;                
            }
            else if(engineConfiguration.Type == EngineType.Electric)
            {
                netEngineTorque = engineTorque;                    
            }

            // Calculate Wheel Torque 
            float rpmDelta = currentEngineRPM - engineInputRPM;
            float effectiveRigidity = driveTrain.Rigidity * Mathf.Abs(driveTrain.GetRatioForGear(gear));
            torqueFromWheels = Mathf.MoveTowards(torqueFromWheels, rpmDelta * effectiveRigidity / RAD_SEC_TO_RPM, MAX_TORQUE_FROM_WHEELS_DELTA * substepDT);
            float driveTrainDampingForce = Mathf.Abs(rpmDelta * driveTrain.Damping);

            // Calculate Engine Momentum
            float netTorque = netEngineTorque - torqueFromWheels - driveTrainDampingForce;
            float angularAcceleration = netTorque / engineConfiguration.Inertia;

            // Update the engine RPM
            currentEngineRPM += angularAcceleration * substepDT * RAD_SEC_TO_RPM;
            currentEngineRPM = Mathf.Clamp(currentEngineRPM, engineConfiguration.IdleRPM * IDLE_FLOOR_FACTOR, engineConfiguration.MaxRPM);

            // Output engine torque through the drive train to the wheels
            // Still simulated for an EV
            bool idleEngineCreep = currentEngineRPM > Mathf.Abs(engineInputRPM) && brakeInput < 0.01f;
            bool isIdleEV = engineConfiguration.Type == EngineType.Electric && brakeInput < 0.01f && throttleInput < 0.01f;
            if(isIdleEV)
            {
                // Manually simulate engine creep on an EV.
                return engineConfiguration.GetTorque(engineConfiguration.IdleRPM) * engineConfiguration.IdleCompensation;
            }
            else if (throttleInput > 0.01f || idleEngineCreep)
            {
                // Combustion or idle momentum applies force to the wheels
                return Mathf.Abs(netEngineTorque) * driveTrain.GetRatioForGear(gear) * driveTrain.Loss;
            }
            else
            {
                if(gear == Gear.Neutral || gear == Gear.Park) return 0f;

                // Engine Braking
                float engineBrakingMagnitude = Mathf.Abs(torqueFromWheels * driveTrain.Loss);
                if(engineInputRPM * gearDirection > 0.01f) return -engineBrakingMagnitude;
                else if(engineInputRPM * gearDirection < 0.01f) return engineBrakingMagnitude;
                
                return 0f;
            }
        }
    }    

    public enum EngineType
    {
        Gas,
        Electric
    }
}
