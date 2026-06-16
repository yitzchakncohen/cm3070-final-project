using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public float CurrentAcceleration => currentAcceleration;
    public float CurrentBraking => currentBraking;
    public float CurrentSteering => currentSteering;
    private const string KEYBOARD_SCHEME = "Keyboard";
    private const string CONTROLLER_SCHEME = "Controller";
    private PlayerInput playerInput;
    [SerializeField] private VehicleController vehicleController;
    [SerializeField] private float accelerationRate = 0.01f;
    [SerializeField] private float brakingRate = 0.01f;
    [SerializeField] private float steeringRate = 0.01f;
    private float currentAcceleration = 0f;
    private float currentBraking = 0f;
    private float currentSteering = 0f;
    private float accelerationInput = 0f;
    private float brakingInput = 0f;
    private float steeringInput = 0f;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    void Update()
    {
        UpdateAcceleration(accelerationInput);       
        UpdateBraking(brakingInput);
        UpdateSteering(steeringInput);

        vehicleController.Steer(currentSteering);
        vehicleController.Brake(currentBraking);
        vehicleController.Accelerate(currentAcceleration);
    }

    public void OnAccelerate(InputAction.CallbackContext context)
    {
        accelerationInput = context.ReadValue<float>();
    }

    public void OnBrake(InputAction.CallbackContext context)
    {
        brakingInput = context.ReadValue<float>();
    }

    public void OnTurn(InputAction.CallbackContext context)
    {
        steeringInput = context.ReadValue<float>();
    }

    public void OnGear(InputAction.CallbackContext context)
    {
        if(!context.performed) return; 
        
        float input = context.ReadValue<float>();
        if(input < 0)
        {
            vehicleController.ShiftGearPrevious();
        }
        else if(input > 0)
        {
            vehicleController.ShiftGearNext();
        }
    }

    private void UpdateAcceleration(float input)
    {
        if (playerInput.currentControlScheme == KEYBOARD_SCHEME)
        {
            if (input == 1)
            {
                currentBraking = 0f;
                currentAcceleration += accelerationRate * Time.deltaTime;
            }
            else if (input == 0)
            {
                currentAcceleration -= accelerationRate * Time.deltaTime;
            }
        }
        else
        {
            currentAcceleration = input;
        }
        currentAcceleration = Mathf.Clamp(currentAcceleration, 0f, 1f);
    }

    private void UpdateBraking(float input)
    {
        if (playerInput.currentControlScheme == KEYBOARD_SCHEME)
        {
            if (input == 1)
            {
                currentAcceleration = 0f;
                currentBraking += brakingRate * Time.deltaTime;
            }
            else if (input == 0)
            {
                currentBraking -= brakingRate * Time.deltaTime;
            }
        }
        else
        {
            currentBraking = input;
        }
        currentBraking = Mathf.Clamp(currentBraking, 0f, 1f);
    }

    private void UpdateSteering(float input)
    {
        if (playerInput.currentControlScheme == KEYBOARD_SCHEME)
        {
            if (input == 1)
            {
                currentSteering += steeringRate * Time.deltaTime;
            }
            else if (input == -1)
            {
                currentSteering -= steeringRate * Time.deltaTime;
            }
        }
        else
        {
            currentSteering = input;
        }
        currentSteering = Mathf.Clamp(currentSteering, -1f, 1f);
    }
}
