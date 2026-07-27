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
        [SerializeField] private InputManager inputManager;
        [SerializeField] private VehicleController vehicleController;
        [SerializeField] private Image accelerator;
        [SerializeField] private Image brake;
        [SerializeField] private RectTransform steeringWheel;
        [SerializeField] private TMP_Text gearText;
        [SerializeField] private Odemeter speedomdeter;
        [SerializeField] private Odemeter odemeter;
        private SteeringConfiguration steeringConfiguration;

        private void Start()
        {
            VehicleController_OnGearChanged();
            speedomdeter.Init("km/h", 20f) ;
            odemeter.Init("x1000r/min", 0.5f) ;
            steeringConfiguration = vehicleController.Steering;
        }

        private void OnEnable()
        {
            vehicleController.OnGearChanged += VehicleController_OnGearChanged;
            InvokeRepeating(nameof(UpdateDigitalDisplays), 0f, UPDATE_DIGITAL_INTERVAL);
        }

        private void OnDisable()
        {
            vehicleController.OnGearChanged -= VehicleController_OnGearChanged;
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
            gearText.text = vehicleController.Gear.ToLetter();
        }

        private void UpdateDigitalDisplays()
        {
            speedomdeter.UpdateDigital(vehicleController.Speed * 3.6f);
            odemeter.UpdateDigital(vehicleController.RPM);
        }
    }
}
