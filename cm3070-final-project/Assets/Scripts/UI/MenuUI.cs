using System;
using System.Collections.Generic;
using System.Linq;
using ModularVehicleSimulator.UI.VehicleSettings;
using ModularVehicleSimulator.Vehicle;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ModularVehicleSimulator.UI
{
    public class MenuUI : MonoBehaviour
    {
        [SerializeField] private VehicleSettingsMenu vehicleSettings;
        [SerializeField] private Button vehicleSettingsOpenButton;
        [SerializeField] private Button vehicleSettingsCloseButton;
        [SerializeField] private VehicleSelectionMenu vehicleSelection;
        [SerializeField] private Button vehicleSelectionOpenButton;
        [SerializeField] private Button vehicleSelectionCloseButton;
        [SerializeField] private GameObject controls;
        [SerializeField] private Button controlsOpenButton;
        [SerializeField] private Button controlsCloseButton;
        [SerializeField] private WeatherUI weather;
        [SerializeField] private Button weatherOpenButton;
        [SerializeField] private Button weatherCloseButton;
        [SerializeField] private Button restartButton;
        private PlayerInput[] playerInputs;
        private List<VehicleController> vehicles;

        private void Start()
        {
            playerInputs = FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            vehicles = FindObjectsByType<VehicleController>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
            vehicleSelection.Init(vehicles);
        }

        private void OnEnable()
        {
            vehicleSettingsOpenButton.onClick.AddListener(VehicleSettingsOpenButton_onClick);
            vehicleSettingsCloseButton.onClick.AddListener(VehicleSettingsCloseButton_onClick);
            vehicleSelectionOpenButton.onClick.AddListener(VehicleSelectionOpenButton_onClick);
            vehicleSelectionCloseButton.onClick.AddListener(VehicleSelectionCloseButton_onClick);
            controlsOpenButton.onClick.AddListener(controlsOpenButton_onClick);
            controlsCloseButton.onClick.AddListener(controlsCloseButton_onClick);
            weatherOpenButton.onClick.AddListener(weatherOpenButton_onClick);
            weatherCloseButton.onClick.AddListener(weatherCloseButton_onClick);
            restartButton.onClick.AddListener(RestartButton_onClick);
            vehicleSelection.OnChangeVehicle += VehicleSelection_OnChangeVehicle;
        }

        private void OnDisable()
        {
            vehicleSettingsOpenButton.onClick.RemoveAllListeners();
            vehicleSettingsCloseButton.onClick.RemoveAllListeners();
            vehicleSelectionOpenButton.onClick.RemoveAllListeners();
            vehicleSelectionCloseButton.onClick.RemoveAllListeners();
            controlsOpenButton.onClick.RemoveAllListeners();
            controlsCloseButton.onClick.RemoveAllListeners();
            weatherOpenButton.onClick.RemoveAllListeners();
            weatherCloseButton.onClick.RemoveAllListeners();
            restartButton.onClick.RemoveAllListeners();
            vehicleSelection.OnChangeVehicle -= VehicleSelection_OnChangeVehicle;
        }

        private void VehicleSettingsOpenButton_onClick()
        {
            vehicleSettings.gameObject.SetActive(true);
            vehicleSelection.gameObject.SetActive(false);
            controls.SetActive(false);
            weather.gameObject.SetActive(false);
            DeactivatePlayerInput();
        }

        private void VehicleSettingsCloseButton_onClick()
        {
            vehicleSettings.gameObject.SetActive(false);
            ActivatePlayerInput();
        }

        private void VehicleSelectionOpenButton_onClick()
        {
            vehicleSettings.gameObject.SetActive(false);
            vehicleSelection.gameObject.SetActive(true);
            controls.SetActive(false);
            weather.gameObject.SetActive(false);
            DeactivatePlayerInput();
        }

        private void VehicleSelectionCloseButton_onClick()
        {
            vehicleSelection.gameObject.SetActive(false);
            ActivatePlayerInput();
        }

        private void controlsOpenButton_onClick()
        {
            vehicleSettings.gameObject.SetActive(false);
            vehicleSelection.gameObject.SetActive(false);
            controls.SetActive(true);
            weather.gameObject.SetActive(false);
            DeactivatePlayerInput();
        }

        private void controlsCloseButton_onClick()
        {
            controls.SetActive(false);
            ActivatePlayerInput();
        }

        private void weatherOpenButton_onClick()
        {
            vehicleSettings.gameObject.SetActive(false);
            vehicleSelection.gameObject.SetActive(false);
            controls.SetActive(false);
            weather.gameObject.SetActive(true);
            DeactivatePlayerInput();
        }

        private void weatherCloseButton_onClick()
        {
            weather.gameObject.SetActive(false);
            ActivatePlayerInput();
        }

        private void RestartButton_onClick()
        {
            ActivatePlayerInput();
            // TODO reset vehicle position.
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void VehicleSelection_OnChangeVehicle(VehicleController controller)
        {
            vehicleSettings.UpdateVehicle(controller.Config);
        }

        private void ActivatePlayerInput()
        {
            foreach (PlayerInput playerInput in playerInputs)
            {
                if(playerInput.isActiveAndEnabled)
                {
                    playerInput.ActivateInput();                    
                }
            }
        }

        private void DeactivatePlayerInput()
        {
            foreach (PlayerInput playerInput in playerInputs)
            {
                playerInput.DeactivateInput();
            }
        }
    }    
}
