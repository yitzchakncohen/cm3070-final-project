using System.Collections.Generic;
using ModularVehicleSimulator.Vehicle;
using UnityEngine;

namespace ModularVehicleSimulator.UI
{
    public class CameraUI : MonoBehaviour
    {
        [SerializeField] private List<CameraLabel> cameraLabels;
        private CameraController currentCameraController = null;

        public void Init(CameraController vehicleCameraController)
        {
            if(currentCameraController != null) currentCameraController.OnCameraTypeChanged -= CurrentVehicle_OnCameraTypeChanged;
            currentCameraController = vehicleCameraController;
            currentCameraController.OnCameraTypeChanged += CurrentVehicle_OnCameraTypeChanged;
            CurrentVehicle_OnCameraTypeChanged(vehicleCameraController.Type);
        }

        private void OnDestory()
        {
            if(currentCameraController != null) currentCameraController.OnCameraTypeChanged -= CurrentVehicle_OnCameraTypeChanged;
        }

        private void CurrentVehicle_OnCameraTypeChanged(Vehicle.CameraType type)
        {
            foreach (CameraLabel camera in cameraLabels)
            {
                if(camera.Type == type)
                {
                    camera.gameObject.SetActive(true);
                }
                else
                {
                    camera.gameObject.SetActive(false);
                }
            }
        }
    }
}
