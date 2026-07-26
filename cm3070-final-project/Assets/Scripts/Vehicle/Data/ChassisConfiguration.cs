using UnityEngine;

[CreateAssetMenu(fileName = "ChassisConfiguration", menuName = "Vehicle Simulator/ChassisConfiguration")]
public class ChassisConfiguration : ScriptableObject
{
    public float Mass => massInKG;
    public float WheelBase => wheelBaseInMeters;
    public float Track => trackInMeters;
    public Vector3 CenterOfMass => centerOfMassOffsetInMeters;
    public int NumberOfWheels => numberOfWheels;
    public float DragCoefficient => dragCoefficient;
    [SerializeField] private float massInKG = 1510f;
    // TODO give option to be determine by model.
    // TODO apply ride height
    // TODO apply wheel front back offset
    [SerializeField] private float wheelBaseInMeters = 2.8f;
    [SerializeField] private float trackInMeters = 1.58f;
    [SerializeField] private Vector3 centerOfMassOffsetInMeters;
    [SerializeField] private int numberOfWheels = 4;
    [SerializeField] private float dragCoefficient = 0.34f;
}
