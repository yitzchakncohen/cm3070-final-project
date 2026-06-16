using UnityEngine;
using UnityEngine.UI;

public class DrivingUI : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private Image accelerator;
    [SerializeField] private Image brake;
    [SerializeField] private RectTransform steeringWheel;
    private float wheelRotationRate = 180f;

    private void Update()
    {
        accelerator.fillAmount = inputManager.CurrentAcceleration;
        brake.fillAmount = inputManager.CurrentBraking;
        steeringWheel.rotation = Quaternion.Euler(0, 0, -inputManager.CurrentSteering * wheelRotationRate);
    }
}
