using ModularVehicleSimulator.Vehicle;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.SceneManagement;

namespace ModularVehicleSimulator.Input
{
    public class InputManager : MonoBehaviour
    {
        public float CurrentAcceleration => currentAcceleration;
        public float CurrentBraking => currentBraking;
        public float CurrentSteering => currentSteering;
        private const string KEYBOARD_SCHEME = "Keyboard";
        private const string CONTROLLER_SCHEME = "Controller";
        private PlayerInput playerInput;
        [SerializeField] private VehicleController vehicleController;
        [SerializeField] private float accelerationRampRate = 3.0f;
        [SerializeField] private float brakingRampRate = 3.0f;
        [SerializeField] private float steeringRampRate = 4.0f;
        [SerializeField] private float accelerationReleaseRate = 8.0f;
        [SerializeField] private float brakingReleaseRate = 10.0f;
        [SerializeField] private float steeringReleaseRate = 6.0f;
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

        private void OnEnable()
        {
            playerInput.ActivateInput();
            if (playerInput.user.valid)
            {
                playerInput.user.UnpairDevices();
                foreach (var device in InputSystem.devices)
                {
                    InputUser.PerformPairingWithDevice(device, user: playerInput.user);
                }
            }
            InputSystem.Update();
        }

        private void OnDisable()
        {
            playerInput.DeactivateInput();            
            if (playerInput.user.valid)
            {
                playerInput.user.UnpairDevices();
            }
        }

        void Update()
        {
            UpdateAcceleration(accelerationInput);       
            UpdateBraking(brakingInput);
            UpdateSteering(steeringInput);
        }

        void FixedUpdate()
        {
            vehicleController.Steer(currentSteering);
            vehicleController.Brake(currentBraking, currentAcceleration);
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

        public void OnToggleCamera(InputAction.CallbackContext context)
        {
            if(!context.performed) return;

            vehicleController.ToggleCamera();
        }

        public void OnRestart(InputAction.CallbackContext context)
        {
            if(!context.performed) return;

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void UpdateAcceleration(float input)
        {
            if (input > currentAcceleration)
            {
                currentAcceleration = Mathf.MoveTowards(currentAcceleration, input, accelerationRampRate * Time.deltaTime);
            }
            else
            {
                currentAcceleration = Mathf.MoveTowards(currentAcceleration, input, accelerationReleaseRate * Time.deltaTime);
            }
        }

        private void UpdateBraking(float input)
        {
            if (input > currentBraking)
            {
                currentBraking = Mathf.MoveTowards(currentBraking, input, brakingRampRate * Time.deltaTime);
            }
            else
            {
                currentBraking = Mathf.MoveTowards(currentBraking, input, brakingReleaseRate * Time.deltaTime);
            }
        }

        private void UpdateSteering(float input)
        {
            bool isSteeringDeeper = Mathf.Abs(input) > Mathf.Abs(currentSteering) 
                && (Mathf.Sign(input) == Mathf.Sign(currentSteering) 
                || Mathf.Approximately(currentSteering, 0f));
            if (isSteeringDeeper)
            {
                currentSteering = Mathf.MoveTowards(currentSteering, input, steeringRampRate * Time.deltaTime);
            }
            else
            {
                currentSteering = Mathf.MoveTowards(currentSteering, input, steeringReleaseRate * Time.deltaTime);
            }
        }
    }
}
