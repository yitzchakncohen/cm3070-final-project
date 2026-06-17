using Unity.Cinemachine;
using UnityEngine;
namespace ModularVehicleSimulator.Vehicle
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera[] cameras;
        private int currentCamera = 0;

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

        public void ToggleCamera()
        {
            cameras[currentCamera].gameObject.SetActive(false);
            currentCamera = (currentCamera + 1) % cameras.Length;
            cameras[currentCamera].gameObject.SetActive(true);
        }        
    }
}
