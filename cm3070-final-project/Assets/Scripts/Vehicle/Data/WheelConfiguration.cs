using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
    [CreateAssetMenu(fileName = "WheelConfiguration", menuName = "Vehicle Simulator/WheelConfiguration")]
    public class WheelConfiguration : ScriptableObject
    {
        public float Radius => radiusInMeters;
        public float Width => widthInMeters;
        public float Weight => weightInKG;
        public float ForwardStiffness => forwardStiffness;
        public float SideWaysStiffness => sideWaysStiffness;
        public float RadialTireStiffness => (pressureMultiplier * tirePressureInPSI) + carcassBaseStiffness;
        public float LateralTireStiffness => RadialTireStiffness * lateralStiffnessRatio;
        public float DeflectionGrip => gripGainedPerMeterOfDeflection;
        [Header("Dimensions")]
        private float radiusInMeters = 0.3284f;
        private float widthInMeters = 0.225f;
        private float weightInKG = 20f;
        [Header("Friction")]
        [Header("Friction Forward")]
        [SerializeField] private float forwardExtremeSlip = 0.125f;
        [SerializeField] private float forwardExtremeValue = 0.875f;
        [SerializeField] private float forwardAsymptoteSlip = 0.7f;
        [SerializeField] private float forwardAsymptoteValue = 0.725f;
        [SerializeField] private float forwardStiffness = 1.0f;
        [Header("Friction Sideways")]
        [SerializeField] private float sideWaysExtremeSlip = 0.175f;
        [SerializeField] private float sideWaysExtremeValue = 0.875f;
        [SerializeField] private float sideWaysAsymptoteSlip = 0.7f;
        [SerializeField] private float sideWaysAsymptoteValue = 0.725f;
        [SerializeField] private float sideWaysStiffness = 1.0f;
        [Header("Tire Deformation")]
        [SerializeField] private float pressureMultiplier = 2500f;
        [SerializeField] private float carcassBaseStiffness = 145000;
        [SerializeField] private float tirePressureInPSI = 35f;
        [SerializeField] private float lateralStiffnessRatio = 0.6f;
        [SerializeField] private float gripGainedPerMeterOfDeflection = 0.15f;

        public WheelFrictionCurve GetForwardFrictionCurve(float numberOfColliders)
        {
            return new WheelFrictionCurve
            {
                extremumSlip = forwardExtremeSlip,
                extremumValue = forwardExtremeValue,
                asymptoteSlip = forwardAsymptoteSlip,
                asymptoteValue = forwardAsymptoteValue,
                stiffness = forwardStiffness / numberOfColliders,
            };
        }

        public WheelFrictionCurve GetSidewaysFrictionCurve(float numberOfColliders)
        {
            return new WheelFrictionCurve
            {
                extremumSlip = sideWaysExtremeSlip,
                extremumValue = sideWaysExtremeValue,
                asymptoteSlip = sideWaysAsymptoteSlip,
                asymptoteValue = sideWaysAsymptoteValue,
                stiffness = sideWaysStiffness  / numberOfColliders
            };
        }
    }
}
