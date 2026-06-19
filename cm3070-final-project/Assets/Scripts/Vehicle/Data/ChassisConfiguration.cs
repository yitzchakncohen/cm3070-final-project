using UnityEngine;

[CreateAssetMenu(fileName = "ChassisConfiguration", menuName = "Vehicle Simulator/ChassisConfiguration")]
public class ChassisConfiguration : ScriptableObject
{
    public float Weight => weightInKG;
    public float WheelBase => wheelBaseInMeters;
    public float Track => trackInMeters;
    [SerializeField] private float weightInKG = 1510f;
    [SerializeField] private float wheelBaseInMeters = 2.8f;
    [SerializeField] private float trackInMeters = 1.58f;
}
