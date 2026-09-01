using System;
using ModularVehicleSimulator.Input;
using ModularVehicleSimulator.Vehicle;
using ModularVehicleSimulator.Vehicle.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModularVehicleSimulator.UI
{
    public class DrivingUI : MonoBehaviour
    {
        private const float WHEEL_ROTATION_MAX = 540f;
        private const float UPDATE_DIGITAL_INTERVAL = 0.5f;
        [SerializeField] private Image accelerator;
        [SerializeField] private Image brake;
        [SerializeField] private RectTransform steeringWheel;
        [SerializeField] private TMP_Text[] gearText;
        [SerializeField] private Odemeter speedomdeter;
        [SerializeField] private Odemeter odemeter;
        [SerializeField] private VehicleSelectionMenu vehicleSelectionMenu;
        [SerializeField] private CameraUI cameraUI;
        private VehicleController vehicleController;
        private SteeringConfiguration steeringConfiguration;
        private InputManager inputManager;

        private void Start()
        {
            speedomdeter.Init("km/h", 20f) ;
            odemeter.Init("x1000r/min", 0.5f) ;
            vehicleController = FindAnyObjectByType<VehicleController>(FindObjectsInactive.Exclude);
            cameraUI.Init(vehicleController.CameraController);
            inputManager = vehicleController.GetComponent<InputManager>();
            steeringConfiguration = vehicleController.Steering;
            VehicleController_OnGearChanged();
            OnEnable();
        }

        private void OnEnable()
        {
            if(vehicleController != null)
            {
                vehicleController.OnGearChanged += VehicleController_OnGearChanged;                
            }
            vehicleSelectionMenu.OnChangeVehicle += VehicleSelectionMenu_OnChangeVehicle;
            InvokeRepeating(nameof(UpdateDigitalDisplays), 0f, UPDATE_DIGITAL_INTERVAL);
        }

        private void OnDisable()
        {
            if(vehicleController != null)
            {
                vehicleController.OnGearChanged -= VehicleController_OnGearChanged;                
            }
            vehicleSelectionMenu.OnChangeVehicle -= VehicleSelectionMenu_OnChangeVehicle;
            CancelInvoke(nameof(UpdateDigitalDisplays));
        }

        private void Update()
        {
            accelerator.fillAmount = inputManager.CurrentAcceleration;
            brake.fillAmount = inputManager.CurrentBraking;
            UpdateSteeringWheel();
            speedomdeter.UpdateNeedle(vehicleController.Speed * 3.6f);
            odemeter.UpdateNeedle(vehicleController.RPM / 1000f);
        }

        private void UpdateSteeringWheel()
        {
            float wheelAngle = -vehicleController.CurrentSteeringAngle / (steeringConfiguration.MaxSteeringAngleAtRest - 0f);
            steeringWheel.rotation = Quaternion.Euler(0, 0, wheelAngle * WHEEL_ROTATION_MAX);
        }

        private void VehicleController_OnGearChanged()
        {
            int currentGear = (int)vehicleController.Gear;
            CheckGear(currentGear, -2);
            CheckGear(currentGear, -1);
            gearText[2].text = vehicleController.Gear.ToLetter();
            CheckGear(currentGear, 1);
            CheckGear(currentGear, 2);
        }

        private void CheckGear(int currentGear, int offset)
        {
            if (vehicleController.Config.DriveTrain.ContainsGear(currentGear + offset))
            {
                gearText[2 + offset].text = ((Gear)(currentGear + offset)).ToLetter();                    
            }
            else
            {
                gearText[2 + offset].text = string.Empty;
            }
        }

        private void UpdateDigitalDisplays()
        {
            speedomdeter.UpdateDigital(vehicleController.Speed * 3.6f);
            odemeter.UpdateDigital(vehicleController.RPM);
        }

        private void VehicleSelectionMenu_OnChangeVehicle(VehicleController controller)
        {
            OnDisable();
            vehicleController = controller;
            cameraUI.Init(vehicleController.CameraController);
            steeringConfiguration = vehicleController.Steering;
            inputManager = vehicleController.GetComponent<InputManager>();
            OnEnable();
            VehicleController_OnGearChanged();
        }
    }
}
