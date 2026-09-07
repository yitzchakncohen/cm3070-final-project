using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
    [CreateAssetMenu(fileName = "BrakesConfiguration", menuName = "Vehicle Simulator/BrakesConfiguration")]
    public class BrakesConfiguration : ScriptableObject
    {
        public float Torque => brakeTorqueInNewtonMeters;
        public float RegenerativeBrakeTorque => regenerativeBrakeTorqueInNewtonMeters;
        public float FrontBias => fontBrakeBias;
        public bool ABSEnabled => enableABS;
        public bool RegenerativeBrakingEnabled => enableRegenerativeBraking;
        public float ABSSlipThreshholdMultiplier => aBSSlipThresholdMultiplier;
        public float ABSOscillationSpeed => aBSOscillationSpeed;
        public bool IsBrakeTorqueVectoringEnabled => enableBrakeTorqueVectoring;
        public float VectoringBrakeTorque => vectoringBrakeTorqueInNewtonMeters;
        public float UndersteerThreshold => understeerThresholdInDegreesPerSecond;
        public float UndersteerSpeedMin => understeerSpeedMinInMetersPerSecond;
        public float UndersteerSpeedMax => understeerSpeedMaxInMetersPerSecond;

        [Tooltip("Maximum braking force.")]
        [SerializeField] private float brakeTorqueInNewtonMeters = 3000f;
        [Tooltip("Braking force applied when there is no \ngas or brake input on electric vehicle.")]
        [SerializeField] private float regenerativeBrakeTorqueInNewtonMeters = 600f;
        [Tooltip("The ratio of the braking force \napplied to the front wheels.")]
        [SerializeField] private float fontBrakeBias = 0.7f;
        [Tooltip("Does the EV use regenerative \nbraking to charge the battery?")]
        [SerializeField] private bool enableRegenerativeBraking = true;
        [Header("ABS")]
        [Tooltip("Is the Anti-lock Braking System (ABS) enabled.")]
        [SerializeField] private bool enableABS = true;
        [Tooltip("Is slip threshold at which \nthe ABS system is engaged.")]
        [SerializeField] private float aBSSlipThresholdMultiplier = 1.1f;
        [Tooltip("The angular frequency of the ABS \nsystem in pulses per second")]
        [SerializeField] private float aBSOscillationSpeed = 20f;
        [Header("Brake Torque Vectoring (BTV)")]
        [Tooltip("Is the brake torque vectoring (BTV) system \nenabled for enhanced turn braking.")]
        [SerializeField] private bool enableBrakeTorqueVectoring = true;
        [Tooltip("Force applied to the inner \nwheel by the BTV.")]
        [SerializeField] private float vectoringBrakeTorqueInNewtonMeters = 250f;
        [Tooltip("Minmum understeer threshold \nto engage the BTV.")]
        [SerializeField] private float understeerThresholdInDegreesPerSecond = 4.5f;
        [Tooltip("Minimum speed threshold \nto engage the BTV.")]
        [SerializeField] private float understeerSpeedMinInMetersPerSecond = 5f;
        [Tooltip("Maximum speed threshold \nto engage the BTV.")]
        [SerializeField] private float understeerSpeedMaxInMetersPerSecond = 40f;
        
    }
}
