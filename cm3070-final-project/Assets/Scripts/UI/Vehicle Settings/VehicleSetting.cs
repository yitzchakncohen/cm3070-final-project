using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using System;

namespace ModularVehicleSimulator.UI.VehicleSettings
{
    public class VehicleSetting : MonoBehaviour
    {
        [SerializeField] private TMP_Text settingName;
        [SerializeField] private TMP_InputField settingValueInput;
        [SerializeField] private Toggle settingValueToggle;
        private Action<float> onFloatValueChanged = null;
        private Action<bool> onBoolValueChanged = null;
        private const string NUMERICAL_REGEX_STRING = @"[^0-9.]";

        private void Start()
        {
            settingValueInput.contentType = TMP_InputField.ContentType.DecimalNumber; 
            settingValueInput.onValueChanged.AddListener(SettingsValueInput_onValueChanged); 
            settingValueToggle.onValueChanged.AddListener(SettingsValueToggle_onValueChanged);
        }

        private void OnDestroy()
        {
            settingValueInput.onValueChanged.RemoveAllListeners();
        }

        public void Init(string name, float value, Action<float> onValueChanged)
        {
            settingName.text = name;
            settingValueInput.text = value.ToString();
            settingValueInput.gameObject.SetActive(true);
            settingValueToggle.gameObject.SetActive(false);
            onFloatValueChanged = onValueChanged;
        }

        public void Init(string name, bool value, Action<bool> onValueChanged)
        {
            settingName.text = name;
            settingValueInput.gameObject.SetActive(false);
            settingValueToggle.gameObject.SetActive(true);
            settingValueToggle.isOn = value;
            onBoolValueChanged = onValueChanged;
        }

        private void SettingsValueInput_onValueChanged(string input)
        {
            input = Regex.Replace(input, NUMERICAL_REGEX_STRING, "");
            
            // Remove extra decimal points
            int firstDecimal = input.IndexOf('.');
            if(firstDecimal != -1)
            {
                input = input.Substring(firstDecimal + 1).Replace(".", "");
            }

            settingValueInput.text = input;
            if(onFloatValueChanged != null)
            {
                onFloatValueChanged?.Invoke(float.Parse(input));                
            }
        }

        private void SettingsValueToggle_onValueChanged(bool value)
        {
            if(onBoolValueChanged != null)
            {
                onBoolValueChanged.Invoke(value);
            }
        }
    }
}
