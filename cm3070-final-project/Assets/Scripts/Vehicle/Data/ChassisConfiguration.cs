using UnityEngine;

[CreateAssetMenu(fileName = "ChassisConfiguration", menuName = "Vehicle Simulator/ChassisConfiguration")]
public class ChassisConfiguration : ScriptableObject
{
    [SerializeField] private float weightInKG = 1510f;
}
