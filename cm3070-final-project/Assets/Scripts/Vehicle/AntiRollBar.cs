using System;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class AntiRollBar : MonoBehaviour
    {
        [SerializeField] private Wheel[] wheels;
        [SerializeField] private bool isFront;
        private Rigidbody chassisRigidBody;
        private SteeringConfiguration steeringConfiguration;

        public void Init(Rigidbody chassisRigidBody, SteeringConfiguration steeringConfiguration)
        {
            this.chassisRigidBody = chassisRigidBody;
            this.steeringConfiguration = steeringConfiguration;
            if (wheels.Length <= 1)
            {
                Debug.LogException(new Exception("[Anti-Roll Bar] Not enough wheels on anti-roll bar: " + gameObject.name));
            }
        }

        private void FixedUpdate()
        {
            if (wheels.Length <= 1)
            {
                return;
            }
            float travelLeft = 0f;
            float travelRight = 0f;
            int leftWheels = 0;
            int rightWheels = 0;
            foreach (Wheel wheel in wheels)
            {
                if(wheel.IsLeft)
                {
                    leftWheels++;
                    travelLeft += wheel.GetTravel();
                }
                else
                {
                    rightWheels++;
                    travelRight += wheel.GetTravel();
                }
            }
            float stiffness = isFront ? steeringConfiguration.FrontStiffness : steeringConfiguration.RearStiffness;
            float antiRollForce = (travelLeft/leftWheels - travelRight/rightWheels) * stiffness;

            foreach (Wheel wheel in wheels)
            {
                if(wheel.IsLeft && wheel.IsGrounded)
                {
                    // Debug.Log($"wheel.transform.up * -antiRollForce {wheel.transform.up * -antiRollForce}");
                    chassisRigidBody.AddForceAtPosition(wheel.transform.up * -antiRollForce, wheel.transform.position);
                }
                else if(wheel.IsRight && wheel.IsGrounded)
                {
                    // Debug.Log($"wheel.transform.up * antiRollForce {wheel.transform.up * antiRollForce}");
                    chassisRigidBody.AddForceAtPosition(wheel.transform.up * antiRollForce, wheel.transform.position);
                }
            }
        }
    }    
}
