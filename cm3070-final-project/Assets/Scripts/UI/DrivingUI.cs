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
        [SerializeField] private Odemeter speedomdeter;
        [SerializeField] private Odemeter odemeter;
        private float wheelRotationRate = 180f;

        private void Start()
        {
            VehicleController_OnGearChanged();
            speedomdeter.Init("km/h", 20f) ;
            odemeter.Init("x1000r/min", 0.5f) ;
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
            speedomdeter.UpdateNeedle(vehicleController.Speed * 3.6f);
            odemeter.UpdateNeedle(vehicleController.RPM / 1000f);
        }

        private void VehicleController_OnGearChanged()
        {
            gearText.text = vehicleController.Gear.ToLetter();
        }
    }
}
