using System.Linq;
using ModularVehicleSimulator.Physics;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class Brake : MonoBehaviour
    {
        public bool IsABSActive => isABSActive;
        public bool IsTVBActive => isTVBActive;
        private bool isABSActive = false;
        private bool isTVBActive = false;
        private const float REGENERATIVE_BRAKING_CUTOFF_KMH = 5f;
        private const float ABS_CUTOFF_KMH = 5f;
        private const float BRAKE_TORQUE_VECTORING_BLEND_WINDOW = 0.2f;
        private BrakesConfiguration brakesConfiguration;
        private EngineType engineType;
        private Wheel[] wheels;
        private ChassisConfiguration chassis;
        private Rigidbody chassisRigidBody;
        private WheelConfiguration wheelConfiguration;
        private int frontWheelCount = 2;
        private int backWheelCount = 2;
        private int motorizedWheelCount = 2;

        public void Init(Wheel[] wheels, BrakesConfiguration brakesConfiguration, EngineType engineType, ChassisConfiguration chassisConfiguration, Rigidbody chassisRigidBody, WheelConfiguration wheelConfiguration)
        {
            this.wheels = wheels; 
            this.brakesConfiguration = brakesConfiguration;
            this.engineType = engineType;
            this.chassis = chassisConfiguration;
            this.chassisRigidBody = chassisRigidBody;
            this.wheelConfiguration = wheelConfiguration;
            frontWheelCount = wheels.Count(wheel => wheel.IsFront);
            backWheelCount = wheels.Count(wheel => !wheel.IsFront);
            motorizedWheelCount = wheels.Count(wheel => wheel.IsMotorized);
        }

        public void ApplyForce(float brakeInput, float throttleInput, float targetSteeringAngle)
        {
            float forwardSpeed = VehiclePhysics.GetVehicleSpeed(wheels, wheelConfiguration.Radius);
            foreach (Wheel wheel in wheels)
            {
                if(engineType == EngineType.Electric && brakesConfiguration.RegenerativeBrakingEnabled && brakeInput < 0.01f && throttleInput < 0.01f) // TODO, Hybrid?
                {
                    if(wheel.IsMotorized)
                    {
                        float speed = VehiclePhysics.GetVehicleSpeed(wheels, wheelConfiguration.Radius);
                        float regenerativeBrakeTorque = 
                            Mathf.Clamp01(speed / REGENERATIVE_BRAKING_CUTOFF_KMH ) 
                            * brakesConfiguration.RegenerativeBrakeTorque / motorizedWheelCount;
                        float brakeTorque = ApplyBrakeTorqueVectoring(wheel, targetSteeringAngle, regenerativeBrakeTorque, forwardSpeed);
                        brakeTorque = ApplyABS(wheel, brakeTorque);
                        wheel.Brake(brakeTorque);
                    }
                    else
                    {
                        float brakeTorque = ApplyBrakeTorqueVectoring(wheel, targetSteeringAngle, 0f, forwardSpeed);  
                        brakeTorque = ApplyABS(wheel, brakeTorque);
                        wheel.Brake(brakeTorque);
                    }
                }
                else
                {
                    float bias = wheel.IsFront ? brakesConfiguration.FrontBias : (1 - brakesConfiguration.FrontBias);
                    int wheelCount = wheel.IsFront ? frontWheelCount : backWheelCount;
                    float brakeTorque = brakesConfiguration.Torque * brakeInput * bias / wheelCount;
                    brakeTorque = ApplyBrakeTorqueVectoring(wheel, targetSteeringAngle, brakeTorque, forwardSpeed);
                    brakeTorque = ApplyABS(wheel, brakeTorque);
                    wheel.Brake(brakeTorque);     
                }
            }
        }

        // Anti-lock Brake Sustem (ABS)
        private float ApplyABS(Wheel wheel, float brakeTorque)
        {
            isABSActive = false;
            float speed = VehiclePhysics.GetVehicleSpeed(wheels, wheelConfiguration.Radius);
            if (speed < ABS_CUTOFF_KMH / VehiclePhysics.METERS_PER_SECOND_TO_KM_PER_HOUR) return brakeTorque;
            if (brakesConfiguration.ABSEnabled)
            {
                float vehicleForwardSlip = wheel.GetAverageForwardSlip();
                if (Mathf.Abs(vehicleForwardSlip) > wheel.GetSlipThreshold(brakesConfiguration.ABSSlipThreshholdMultiplier))
                {
                    isABSActive = true;
                    brakeTorque = VehiclePhysics.ABSStepFunction(brakeTorque, brakesConfiguration.ABSOscillationSpeed);
                }
            }
            return brakeTorque;
        }

        // TVB - Torque Vecotring by Braking
        private float ApplyBrakeTorqueVectoring(Wheel wheel, float targetSteeringAngle, float brakeTorque, float forwardSpeed)
        {
            isTVBActive = false;
            if(!brakesConfiguration.IsBrakeTorqueVectoringEnabled) return brakeTorque;

            // Speed Thresholds
            if( Mathf.Abs(forwardSpeed) < brakesConfiguration.UndersteerSpeedMin || Mathf.Abs(forwardSpeed) > brakesConfiguration.UndersteerSpeedMax) return brakeTorque;
            
            // Yaw Rate (rad/s) = (Velocity / Wheelbase) * tan(SteerAngle)
            float targetAngleRad = targetSteeringAngle * Mathf.Deg2Rad;
            float targetSteeringRate = forwardSpeed / chassis.WheelBase * Mathf.Tan(targetAngleRad);
            float actualSteeringRate = chassisRigidBody.angularVelocity.y;

            // Direction specific steering values factor in reversing
            float steeringDirection = Mathf.Sign(targetSteeringAngle) * Mathf.Sign(forwardSpeed);
            float understeeringError = (targetSteeringRate - actualSteeringRate) * steeringDirection; // In Rad/s
            
            if(understeeringError > brakesConfiguration.UndersteerThreshold)
            {
                isTVBActive= true;
                float severity = Mathf.Clamp01((understeeringError - brakesConfiguration.UndersteerThreshold) / BRAKE_TORQUE_VECTORING_BLEND_WINDOW );
                float brakeTorqueAdjustment = brakesConfiguration.VectoringBrakeTorque * severity;
                if(targetAngleRad > 0) // Turning right
                {
                    brakeTorque = wheel.IsRight ? brakeTorque + brakeTorqueAdjustment: brakeTorque;
                }
                else // Turning left
                {
                    brakeTorque = wheel.IsLeft ? brakeTorque + brakeTorqueAdjustment: brakeTorque;
                }
            }
    
            return brakeTorque;
        }
    }
}
