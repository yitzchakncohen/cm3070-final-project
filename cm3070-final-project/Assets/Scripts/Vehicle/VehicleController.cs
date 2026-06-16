using System;
using UnityEngine;

public class VehicleController : MonoBehaviour
{
    public Gear Gear => (Gear)currentGear;
    public event Action OnGearChanged;
    private Wheel[] wheels;
    private int currentGear = 0;

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
                float input = GetAccelerationInputWithGear(accelerationInput);
                wheel.Accelerate(input);
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

    public void ShiftGearNext()
    {
        currentGear = Mathf.Clamp(currentGear + 1, 0, GetMaxGear());
        Debug.Log("Gear: " + Gear.ToString());
        OnGearChanged?.Invoke();
    }

    public void ShiftGearPrevious()
    {
        currentGear = Mathf.Clamp(currentGear - 1, 0, GetMaxGear());
        OnGearChanged?.Invoke();
    }

    private float GetAccelerationInputWithGear(float input)
    {
        switch (Gear)
        {
            case Gear.Park:
                return 0;
            case Gear.Reverse:
                return - input;
            case Gear.Drive:
            default:
                return input;
        }
    }

    private static int GetMaxGear()
    {
        return Enum.GetValues(typeof(Gear)).Length -1;
    }
}
