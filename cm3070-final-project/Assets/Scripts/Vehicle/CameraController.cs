using Unity.Cinemachine;
using UnityEngine;
namespace ModularVehicleSimulator.Vehicle
{
    public class CameraController : MonoBehaviour
    {
        public Camera SelectioCamera => selectionCamera;
        [SerializeField] private CinemachineCamera[] cameras;
        [SerializeField] private Camera selectionCamera;
        [SerializeField] private Transform selectionCameraRotation;
        private int currentCamera = 0;
        private float rotationSpeed = 10;

        private void Awake()
        {
            for (int i = 0; i < cameras.Length; i++)
            {
                if(i == currentCamera)
                {
                    cameras[i].gameObject.SetActive(true);                    
                }
                else
                {
                    cameras[i].gameObject.SetActive(false);                    
                }
            }
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
            cameras[currentCamera].gameObject.SetActive(false);
            currentCamera = (currentCamera + 1) % cameras.Length;
            cameras[currentCamera].gameObject.SetActive(true);
        }        
    }
}
