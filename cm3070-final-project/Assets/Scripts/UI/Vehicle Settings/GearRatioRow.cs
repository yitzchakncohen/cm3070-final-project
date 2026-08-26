using System;
using ModularVehicleSimulator.Vehicle;
using ModularVehicleSimulator.Vehicle.Data;
using TMPro;
using UnityEngine;

namespace ModularVehicleSimulator.UI.VehicleSettings
{
    public class GearRatioRow : MonoBehaviour
    {
        public event Action<GearRatioRow> OnValueChanged;
        public GearRatio GearRatio => gearRatio;
        [SerializeField] private TMP_Dropdown gearDropDown;
        [SerializeField] private TMP_InputField ratioInputField;
        private bool isInitialized = false;
        private GearRatio gearRatio = null;

        private void OnEnable()
        {
            gearDropDown.onValueChanged.AddListener(GearDropDown_onValueChanged);
            ratioInputField.onValueChanged.AddListener(RatioInputField_onValueChanged);
        }

        private void OnDisable()
        {
            gearDropDown.onValueChanged.RemoveAllListeners();
            ratioInputField.onValueChanged.RemoveAllListeners();
        }

        public void Init(GearRatio gearRatio)
        {
            this.gearRatio = new GearRatio{Gear = gearRatio.Gear, Ratio = gearRatio.Ratio};
            gearDropDown.value = (int)gearRatio.Gear;
            ratioInputField.text = VehicleSetting.ValidateFloat(gearRatio.Ratio.ToString());
            isInitialized = true;
        }

        private void GearDropDown_onValueChanged(int input)
        {
            if(!isInitialized) return;

            float.TryParse(ratioInputField.text, out float ratio);
            gearRatio = new GearRatio{Gear = (Gear)input, Ratio = ratio};
            OnValueChanged?.Invoke(this);
        }

        private void RatioInputField_onValueChanged(string input)
        {
            if(!isInitialized) return;

            input = VehicleSetting.ValidateFloat(input);
            ratioInputField.text = input;

            float.TryParse(ratioInputField.text, out float ratio);
            gearRatio = new GearRatio{Gear = (Gear)gearDropDown.value, Ratio = ratio};
            OnValueChanged?.Invoke(this);
        }
    }
    
}
