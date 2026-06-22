using UnityEngine;

[CreateAssetMenu(fileName = "ChassisConfiguration", menuName = "Vehicle Simulator/ChassisConfiguration")]
public class ChassisConfiguration : ScriptableObject
{
    public float Weight => weightInKG;
    public float WheelBase => wheelBaseInMeters;
    public float Track => trackInMeters;
    public Vector3 CenterOfMass => centerOfMassOffsetInMeters;
    [SerializeField] private float weightInKG = 1510f;
    // TODO give option to be determine by model.
    [SerializeField] private float wheelBaseInMeters = 2.8f;
    [SerializeField] private float trackInMeters = 1.58f;
    [SerializeField] private Vector3 centerOfMassOffsetInMeters;
}
