using System;
using TMPro;
using UnityEngine;

namespace ModularVehicleSimulator.UI.VehicleSettings
{
    public class Vector3InputField : MonoBehaviour
    {
        public event Action<Vector3> OnValueChanged;
        [SerializeField] private TMP_InputField[] inputFields;
        private bool isInitialized = false;

        private void OnEnable()
        {
            foreach (TMP_InputField inputField in inputFields)
            {
                inputField.onValueChanged.AddListener((input) => InputField_onValueChanged(input, inputField));
            }
        }

        private void OnDisable()
        {
            foreach (TMP_InputField inputField in inputFields)
            {
                inputField.onValueChanged.RemoveAllListeners();
            }
        }

        public void Init(Vector3 value)
        {
            inputFields[0].text = value.x.ToString();
            inputFields[1].text = value.y.ToString();
            inputFields[2].text = value.z.ToString();
            isInitialized = true;
        }

        private void InputField_onValueChanged(string input, TMP_InputField tMP_InputField)
        {
            if(!isInitialized) return;

            input = VehicleSetting.ValidateFloat(input);
            tMP_InputField.text = input;

            float.TryParse(inputFields[0].text, out float x);
            float.TryParse(inputFields[1].text, out float y);
            float.TryParse(inputFields[2].text, out float z);

            Vector3 newVector3 = new Vector3(x, y, z);

            OnValueChanged?.Invoke(newVector3);
        }
    }
}
