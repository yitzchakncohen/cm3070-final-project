using UnityEngine;

public class Wheel : MonoBehaviour
{
    public bool IsMotorized => isMotorized;
    public bool IsSteerable => isSteerable;
    [SerializeField] private bool isMotorized = true;
    [SerializeField] private bool isSteerable = true;
    [SerializeField] private WheelCollider wheelCollider;
    [SerializeField] private Transform wheelModel;
    private float steeringRange = 45f;
    private float motorTorque = 1000f;
    private float brakeTorque = 1000f;

    private void Awake()
    {
        wheelCollider = GetComponentInChildren<WheelCollider>();
    }

    public void Steer(float steeringInput)
    {
        wheelCollider.steerAngle = steeringInput * steeringRange;
        wheelCollider.GetWorldPose(out Vector3 position, out Quaternion rotation);
        wheelModel.position = position;
        wheelModel.rotation = rotation;
    }

    public void Accelerate(float accelerationInput)
    {
        wheelCollider.brakeTorque = 0f;
        wheelCollider.motorTorque = accelerationInput * motorTorque;
    }

    public void Brake(float brakingInput)
    {
        wheelCollider.motorTorque = 0f;
        wheelCollider.brakeTorque = brakingInput * brakeTorque;
    }
}
