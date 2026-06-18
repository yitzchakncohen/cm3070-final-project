using System;
using ModularVehicleSimulator.Input;
using ModularVehicleSimulator.Vehicle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModularVehicleSimulator.UI
{
    public class DrivingUI : MonoBehaviour
    {
        [SerializeField] private InputManager inputManager;
        [SerializeField] private VehicleController vehicleController;
        [SerializeField] private Image accelerator;
        [SerializeField] private Image brake;
        [SerializeField] private RectTransform steeringWheel;
        [SerializeField] private TMP_Text gearText;
        private float wheelRotationRate = 180f;

        private void Start()
        {
            VehicleController_OnGearChanged();   
        }

        private void OnEnable()
        {
            vehicleController.OnGearChanged += VehicleController_OnGearChanged;
        }

        private void OnDisable()
        {
            vehicleController.OnGearChanged -= VehicleController_OnGearChanged;
        }

        private void Update()
        {
            accelerator.fillAmount = inputManager.CurrentAcceleration;
            brake.fillAmount = inputManager.CurrentBraking;
            steeringWheel.rotation = Quaternion.Euler(0, 0, -inputManager.CurrentSteering * wheelRotationRate);
        }

        private void VehicleController_OnGearChanged()
        {
            gearText.text = vehicleController.Gear.ToLetter();
        }
    }
}
