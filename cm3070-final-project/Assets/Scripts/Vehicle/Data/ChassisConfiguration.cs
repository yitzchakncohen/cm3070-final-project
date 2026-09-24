using UnityEngine;

[CreateAssetMenu(fileName = "ChassisConfiguration", menuName = "Vehicle Simulator/ChassisConfiguration")]
public class ChassisConfiguration : ScriptableObject
{
    public float Mass => massInKG;
    public float WheelBase => wheelBaseInMeters;
    public float Track => trackInMeters;
    public Vector3 CenterOfMass => centerOfMassOffsetInMeters;
    public int NumberOfWheels => numberOfWheels;
    public float GroundClearance => groundClearance;
    public float DragCoefficient => dragCoefficient;
    public float LiftCoefficient => liftCoefficient;
    public float FrontLiftRatio => frontLiftRatio;
    public PhysicsMaterial Material => chassisMaterial;
    [Tooltip("The weight of chassis on earth.")]
    [SerializeField] private float massInKG = 1510f;
    // TODO give option to be determine by model.
    // TODO apply ride height
    // TODO apply wheel front back offset
    [Tooltip("Distance from the front wheel \naxle to the back wheel axle.")]
    [SerializeField] private float wheelBaseInMeters = 2.8f;
    [Tooltip("Distance from the left wheel \nto the right wheel.")]
    [SerializeField] private float trackInMeters = 1.58f;
    [Tooltip("Offset of the vehicle's center \nof mass from it's center.")]
    [SerializeField] private Vector3 centerOfMassOffsetInMeters;
    [Tooltip("How many wheels does the \nvehicle have?")]
    [SerializeField] private int numberOfWheels = 4;
    [Tooltip("Clearance between the bottom of \nthe chassis and the ground.")]
    [SerializeField] private float groundClearance = 0.146f;
    [Header("Aerodynamics")]
    [Tooltip("Forward drag coefficient of \nthe chassis body.")]
    [SerializeField] private float dragCoefficient = 0.31f;
    [Tooltip("Downward lift coefficient of \nthe chassis body.")]
    [SerializeField] private float liftCoefficient = -0.15f;
    [Tooltip("The ratio of the lift force applied to \nthe front of the vehicle verses the rear.")]
    [SerializeField] private float frontLiftRatio = 0.45f;
    [Header("Crash Physics")]
    [SerializeField] private PhysicsMaterial chassisMaterial;
}
