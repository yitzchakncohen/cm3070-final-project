using UnityEngine;

public class VehicleController : MonoBehaviour
{
    private Wheel[] wheels;

    private void Start()
    {
        wheels = GetComponentsInChildren<Wheel>();
    }

    public void Steer(float steeringInput)
    {
        foreach (Wheel wheel in wheels)
        {
            if(wheel.IsSteerable)
            {
                wheel.Steer(steeringInput);
            }
        }
    }

    public void Accelerate(float accelerationInput)
    {
        foreach (Wheel wheel in wheels)
        {
            if(wheel.IsMotorized)
            {
                wheel.Accelerate(accelerationInput);
            }
        }
    }

    public void Brake(float brakeInput)
    {
        foreach (Wheel wheel in wheels)
        {
            wheel.Brake(brakeInput);
        }
    }
}
