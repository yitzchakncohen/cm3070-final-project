using System;
using System.Collections.Generic;
using UnityEngine;
namespace ModularVehicleSimulator.Vehicle
{
    public class CameraController : MonoBehaviour
    {
        public event Action<CameraType> OnCameraTypeChanged;
        public Camera SelectioCamera => selectionCamera;
        public CameraType Type => currentCamera;
        [SerializeField] private List<CameraLabel> cameras;
        [SerializeField] private Camera selectionCamera;
        [SerializeField] private Transform selectionCameraRotation;
        private CameraType currentCamera = CameraType.LockToTarget;
        private float rotationSpeed = 10;

        private void Awake()
        {
            foreach (CameraLabel camera in cameras)
            {
                if(camera.Type == currentCamera)
                {
                    camera.gameObject.SetActive(true);
                }
                else
                {
                    camera.gameObject.SetActive(false);
                }
            }
        }

        private void OnEnable()
        {
            OnCameraTypeChanged?.Invoke(currentCamera);
        }

        private void FixedUpdate()
        {
            if(selectionCamera.gameObject.activeSelf)
            {
                selectionCameraRotation.Rotate(0f, rotationSpeed * Time.fixedDeltaTime, 0f, Space.Self);
            }
        }

        public void ToggleCamera()
        {
            cameras.Find(camera => camera.Type == currentCamera).gameObject.SetActive(false);
            currentCamera = (CameraType)(((int)currentCamera + 1) % Enum.GetValues(typeof(CameraType)).Length);
            cameras.Find(camera => camera.Type == currentCamera).gameObject.SetActive(true);
            OnCameraTypeChanged?.Invoke(currentCamera);
        }        
    }

    public enum CameraType
    {
        FixedAngle,
        LockToTarget,
        FirstPerson
    }
}
