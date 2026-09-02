using System;
using System.Collections;
using System.Collections.Generic;
using ModularVehicleSimulator.Input;
using ModularVehicleSimulator.Vehicle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModularVehicleSimulator.UI
{
    public class VehicleSelectionMenu : MonoBehaviour
    {
        public event Action<VehicleController> OnChangeVehicle;
        private List<VehicleController> vehicles;
        [SerializeField] private TMP_Text vehicleName;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button previousButton;
        private VehicleController currentVehicle;

        public void Init(List<VehicleController> vehicles)
        {
            this.vehicles = vehicles;
            currentVehicle = vehicles.Find(vehicle => vehicle.gameObject.activeSelf);
            vehicleName.text = currentVehicle.Name;
            OnChangeVehicle?.Invoke(currentVehicle);
            foreach (VehicleController vehicle in vehicles)
            {
                if(vehicle != currentVehicle)
                {
                    vehicle.GetComponent<InputManager>().OnDisable();                    
                }
            }
        }

        private void OnEnable()
        {
            nextButton.onClick.AddListener(OnNextButtonClick);
            previousButton.onClick.AddListener(OnPreviousButtonClick);
        }

        private void OnDisable()
        {
            nextButton.onClick.RemoveAllListeners();
            previousButton.onClick.RemoveAllListeners();
        }

        private void OnNextButtonClick()
        {
            int index = vehicles.IndexOf(currentVehicle);
            int newIndex = (index + 1) % vehicles.Count;
            UpdateCurrentVehicle(vehicles[newIndex]);
        }

        private void OnPreviousButtonClick()
        {
            int index = vehicles.IndexOf(currentVehicle);
            int newIndex = (index - 1 % vehicles.Count + vehicles.Count) % vehicles.Count;
            UpdateCurrentVehicle(vehicles[newIndex]);
        }

        private void UpdateCurrentVehicle(VehicleController newVehicle)
        {
            vehicleName.text = currentVehicle.Name;
            SetVehicle(currentVehicle, false);
            SetVehicle(newVehicle, true);
            currentVehicle = newVehicle;
            OnChangeVehicle?.Invoke(currentVehicle);
        }

        private void SetVehicle(VehicleController vehicle, bool enabled)
        {
            vehicle.gameObject.SetActive(enabled);
            StartCoroutine(ResetPhysicsRoutine(vehicle));
        }

        private IEnumerator ResetPhysicsRoutine(VehicleController vehicle)
        {
            vehicle.ChassisRigidBody.isKinematic = true;
            yield return new WaitForEndOfFrame();
            vehicle.ChassisRigidBody.isKinematic = false;
        }
    }    
}
