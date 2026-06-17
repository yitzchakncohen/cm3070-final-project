using System.Linq;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    public class Wheel : MonoBehaviour
    {
        public bool IsMotorized => isMotorized;
        public bool IsSteerable => isSteerable;
        [SerializeField] private bool isMotorized = true;
        [SerializeField] private bool isSteerable = true;
        [SerializeField] private Transform wheelModel;
        private WheelCollider[] wheelColliders;
        private float steeringRange = 45f;
        private float motorTorque = 220f;
        private float driveTrainLoss = 0.85f;
        //Final ratio times 1st gear ratio.
        private float driveTrainRatio = 4.31f * 2.697f;
        private float brakeTorque = 1000f;

        private void Awake()
        {
            wheelColliders = GetComponentsInChildren<WheelCollider>();
        }

        public void Steer(float steeringInput)
        {
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                wheelCollider.steerAngle = steeringInput * steeringRange;
                wheelCollider.GetWorldPose(out Vector3 wheelPosition, out rotation);  
                position = position + wheelPosition;
            }
            wheelModel.position = position / wheelColliders.Count();
            wheelModel.rotation = rotation;
        }

        public void Accelerate(float accelerationInput)
        {
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                wheelCollider.brakeTorque = 0f;
                wheelCollider.motorTorque = accelerationInput * motorTorque * driveTrainLoss * driveTrainRatio /2f;            
            }
        }

        public void Brake(float brakingInput)
        {
            foreach (WheelCollider wheelCollider in wheelColliders)
            {
                wheelCollider.motorTorque = 0f;
                wheelCollider.brakeTorque = brakingInput * brakeTorque / wheelColliders.Count();            
            }
        }
    }
}