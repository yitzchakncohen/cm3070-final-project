using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
    [CreateAssetMenu(fileName = "WheelConfiguration", menuName = "Vehicle Simulator/WheelConfiguration")]
    public class WheelConfiguration : ScriptableObject
    {
        public float Radius => radiusInMeters;
        public float Width => widthInMeters;
        public float Weight => weightInKG;
        public float ForwardExtremeSlip => forwardExtremeSlip = 0.125f;
        public float ForwardExtremeValue => forwardExtremeValue = 0.875f;
        public float ForwardAsymptoteSlip => forwardAsymptoteSlip = 0.7f;
        public float ForwardAsymptoteValue => forwardAsymptoteValue = 0.725f;
        public float ForwardStiffness => forwardStiffness = 1.0f;
        public float SideWaysExtremeSlip => sideWaysExtremeSlip = 0.175f;
        public float SideWaysExtremeValue => sideWaysExtremeValue = 0.875f;
        public float SideWaysAsymptoteSlip => sideWaysAsymptoteSlip = 0.7f;
        public float SideWaysAsymptoteValue => sideWaysAsymptoteValue = 0.725f;
        public float SideWaysStiffness => sideWaysStiffness = 1.0f;
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

    }
}
