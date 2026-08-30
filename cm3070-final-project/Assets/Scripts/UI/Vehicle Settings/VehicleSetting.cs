using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using System;
using ModularVehicleSimulator.Vehicle;
using System.Collections.Generic;
using ModularVehicleSimulator.Vehicle.Data;

namespace ModularVehicleSimulator.UI.VehicleSettings
{
    public class VehicleSetting : MonoBehaviour
    {
        [SerializeField] private TMP_Text settingName;
        [SerializeField] private TMP_InputField floatValueInput;
        [SerializeField] private Toggle boolValueToggle;
        [SerializeField] private Vector3InputField vector3Input;
        [SerializeField] private TMP_Dropdown engineTypeDropDown;
        [SerializeField] private GearRatioList gearRatioList;

        private Action<float> onFloatValueChanged = null;
        private Action<bool> onBoolValueChanged = null;
        private Action<Vector3> onVector3ValueChanged = null;
        private Action<object> onEnumValueChanged = null;
        private Action<List<GearRatio>> onGearRatioListValueChanged = null;
        private const string NUMERICAL_REGEX_STRING = @"[^0-9.]";

        private void Start()
        {
            floatValueInput.contentType = TMP_InputField.ContentType.DecimalNumber; 
            floatValueInput.onValueChanged.AddListener(FloatValueInput_onValueChanged); 
            boolValueToggle.onValueChanged.AddListener(BoolValueToggle_onValueChanged);
            vector3Input.OnValueChanged += Vector3Input_OnValueChanged;
            engineTypeDropDown.onValueChanged.AddListener(EnumDropDown_onValueChanged);
            gearRatioList.OnValueChanged += GearRatioList_OnValueChanged;
        }

        private void OnDestroy()
        {
            floatValueInput.onValueChanged.RemoveAllListeners();
            boolValueToggle.onValueChanged.RemoveAllListeners();
            vector3Input.OnValueChanged -= Vector3Input_OnValueChanged;
            engineTypeDropDown.onValueChanged.RemoveAllListeners();
            gearRatioList.OnValueChanged -= GearRatioList_OnValueChanged;
        }

        public void Init(string name, float value, Action<float> onValueChanged)
        {
            settingName.text = name;
            floatValueInput.text = value.ToString();
            floatValueInput.gameObject.SetActive(true);
            boolValueToggle.gameObject.SetActive(false);
            vector3Input.gameObject.SetActive(false);
            engineTypeDropDown.gameObject.SetActive(false);
            gearRatioList.gameObject.SetActive(false);
            onFloatValueChanged = onValueChanged;
        }

        public void Init(string name, bool value, Action<bool> onValueChanged)
        {
            settingName.text = name;
            floatValueInput.gameObject.SetActive(false);
            boolValueToggle.gameObject.SetActive(true);
            vector3Input.gameObject.SetActive(false);
            engineTypeDropDown.gameObject.SetActive(false);
            gearRatioList.gameObject.SetActive(false);
            boolValueToggle.isOn = value;
            onBoolValueChanged = onValueChanged;
        }

        public void Init(string name, Vector3 value, Action<Vector3> onValueChanged)
        {
            settingName.text = name;
            floatValueInput.gameObject.SetActive(false);
            boolValueToggle.gameObject.SetActive(false);
            vector3Input.gameObject.SetActive(true);
            engineTypeDropDown.gameObject.SetActive(false);
            gearRatioList.gameObject.SetActive(false);
            vector3Input.Init(value);
            onVector3ValueChanged = onValueChanged;
        }

        public void Init(string name, List<GearRatio> value, Action<List<GearRatio>> onValueChanged)
        {
            settingName.text = name;
            floatValueInput.gameObject.SetActive(false);
            boolValueToggle.gameObject.SetActive(false);
            vector3Input.gameObject.SetActive(false);
            engineTypeDropDown.gameObject.SetActive(false);
            gearRatioList.gameObject.SetActive(true);
            gearRatioList.Init(value);
            onGearRatioListValueChanged = onValueChanged;
        }

        public void Init<T>(string name, T value, Action<object> onValueChanged) where T : Enum
        {
            settingName.text = name;
            floatValueInput.gameObject.SetActive(false);
            boolValueToggle.gameObject.SetActive(false);
            vector3Input.gameObject.SetActive(false);
            engineTypeDropDown.gameObject.SetActive(true);
            gearRatioList.gameObject.SetActive(false);

            // Setup list
            engineTypeDropDown.ClearOptions();
            List<string> options = new List<string>(Enum.GetNames(typeof(T)));
            engineTypeDropDown.AddOptions(options);
            engineTypeDropDown.MultiSelect = false;

            engineTypeDropDown.SetValueWithoutNotify(Convert.ToInt32(value));
            onEnumValueChanged = onValueChanged;
        }

        private void FloatValueInput_onValueChanged(string input)
        {
            input = ValidateFloat(input);

            floatValueInput.text = input;
            if (onFloatValueChanged != null)
            {
                onFloatValueChanged?.Invoke(float.Parse(input));
            }
        }

        public static string ValidateFloat(string input)
        {
            input = Regex.Replace(input, NUMERICAL_REGEX_STRING, "");

            // Remove extra decimal points
            int firstDecimal = input.IndexOf('.');
            if (firstDecimal != -1)
            {
                string wholeNumber = input.Substring(0, firstDecimal + 1);
                string fraction = input.Substring(firstDecimal + 1).Replace(".", "");
                input = wholeNumber + fraction;
            }

            return input;
        }

        private void BoolValueToggle_onValueChanged(bool value)
        {
            if(onBoolValueChanged != null)
            {
                onBoolValueChanged.Invoke(value);
            }
        }
        private void Vector3Input_OnValueChanged(Vector3 vector)
        {
            onVector3ValueChanged.Invoke(vector);
        }

        private void EnumDropDown_onValueChanged(int value)
        {
            onEnumValueChanged.Invoke(value);
        }

        private void GearRatioList_OnValueChanged(List<GearRatio> list)
        {
            onGearRatioListValueChanged.Invoke(list);
        }
    }
}
