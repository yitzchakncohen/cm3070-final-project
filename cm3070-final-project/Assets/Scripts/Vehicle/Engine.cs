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
        private const float IDLE_COMPENSATION_MAX = 0.3f;
        private const float IDLE_FLOOR_FACTOR = 0.8f;
        private const float EV_CREEP_TORQUE_THROTTLE = 0.04f;
        private const float MAX_TORQUE_FROM_WHEELS_DELTA = 1000f; // [N*m]
        private const int ENGINE_SUBSTEPS = 10;
        private EngineConfiguration engineConfiguration;
        private DriveTrain driveTrain;
        private Wheel[] wheels;
        private List<Wheel> motorizedWheels;
        private float currentEngineRPM = 0f;
        private float lastEngineRPM = 0f;
        private float torqueFromWheels = 0f;

        public void Init(EngineConfiguration engineConfiguration, DriveTrain driveTrain, Wheel[] wheels)
        {
            this.engineConfiguration = engineConfiguration;
            this.driveTrain = driveTrain;
            this.wheels = wheels;
            motorizedWheels = wheels.Where(wheel => wheel.IsMotorized).ToList();
            currentEngineRPM = engineConfiguration.IdleRPM;
        }

        public void Accelerate(Gear gear, float accelerationInput, float brakeInput)
        {
            float substepDT = Time.fixedDeltaTime / ENGINE_SUBSTEPS;
            float totalTorque = 0f;
            lastEngineRPM = motorizedWheels.Average(wheel => wheel.GetEffectiveRPM()) * driveTrain.GetRatioForGear(gear);
            float averageWheelAcceleration = motorizedWheels.Average(wheel => wheel.RPMAcceleration); 

            for (int i = 0; i < ENGINE_SUBSTEPS; i++)
            {
                float substepTime = (Time.fixedDeltaTime / ENGINE_SUBSTEPS) * i;
                float estimatedEngineRPM = lastEngineRPM + (averageWheelAcceleration * driveTrain.GetRatioForGear(gear) * substepTime);
                totalTorque = GetWheelTorque(gear, accelerationInput, brakeInput, estimatedEngineRPM, substepDT);           
            }

            // Apply the engine torque or braking to the wheels
            foreach (Wheel wheel in motorizedWheels)
            {
                float wheelTorque = ApplyOpenDifferential(totalTorque, motorizedWheels.Count);
                wheel.Accelerate(wheelTorque);
            }                

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
            float idleCompensation = Mathf.Min(idleDelta / engineConfiguration.IdleRPM, IDLE_COMPENSATION_MAX);
            float effectiveInput = Mathf.Max(idleCompensation, throttleInput);
            float engineTorque = engineConfiguration.GetTorque(currentEngineRPM) * effectiveInput;
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
            float rpmDelta = currentEngineRPM - (engineInputRPM * Mathf.Sign(driveTrain.GetRatioForGear(gear)));
            float effectiveRigidity = driveTrain.Rigidity * Mathf.Abs(driveTrain.GetRatioForGear(gear));
            torqueFromWheels = Mathf.MoveTowards(torqueFromWheels, rpmDelta * effectiveRigidity / RAD_SEC_TO_RPM, MAX_TORQUE_FROM_WHEELS_DELTA * substepDT);
            float driveTrainDampingForce = Mathf.Abs(rpmDelta * driveTrain.Damping) * Mathf.Sign(driveTrain.GetRatioForGear(gear));
            Debug.Log("currentEngineRPM: " + currentEngineRPM + " | engineInputRPM " + engineInputRPM * Mathf.Sign(driveTrain.GetRatioForGear(gear)));

            // Calculate Engine Momentum
            float netTorque = netEngineTorque - torqueFromWheels - driveTrainDampingForce;
            float angularAcceleration = netTorque / engineConfiguration.Inertia;

            // Update the engine RPM
            currentEngineRPM += angularAcceleration * substepDT * RAD_SEC_TO_RPM;
            currentEngineRPM = Mathf.Clamp(currentEngineRPM, engineConfiguration.IdleRPM * IDLE_FLOOR_FACTOR, engineConfiguration.MaxRPM);

            // Output engine torque through the drive train to the wheels
            // Still simulated for an EV
            bool idleEngineCreep = currentEngineRPM > Mathf.Abs(engineInputRPM);
            bool isIdleEV = engineConfiguration.Type == EngineType.Electric && brakeInput < 0.01f && throttleInput < 0.01f;
            if(isIdleEV)
            {
                // Manually simulate engine creep on an EV.
                return engineConfiguration.GetTorque(engineConfiguration.IdleRPM) * EV_CREEP_TORQUE_THROTTLE;
            }
            else if (throttleInput > 0.01f || idleEngineCreep)
            {
                // Combustion or idle momentum applies force to the wheels
                return netEngineTorque * driveTrain.GetRatioForGear(gear) * driveTrain.Loss;
            }
            else
            {
                // Engine braking applies force to the wheels
                return torqueFromWheels * driveTrain.Loss;
            }
        }
    }    

    public enum EngineType
    {
        Gas,
        Electric
    }
}
