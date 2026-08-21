using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
    [CreateAssetMenu(fileName = "WheelConfiguration", menuName = "Vehicle Simulator/WheelConfiguration")]
    public class WheelConfiguration : ScriptableObject
    {
        private const float COLD_WEATHER_TEMPERATURE = 7f;
        public float Radius => radiusInMeters;
        public float Width => widthInMeters;
        public float Weight => weightInKG;
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
        [SerializeField] private float defaultForwardStiffness = 1.0f;
        [Header("Friction Sideways")]
        [SerializeField] private float sideWaysExtremeSlip = 0.175f;
        [SerializeField] private float sideWaysExtremeValue = 0.875f;
        [SerializeField] private float sideWaysAsymptoteSlip = 0.7f;
        [SerializeField] private float sideWaysAsymptoteValue = 0.725f;
        [SerializeField] private float defaultSidewaysStiffness = 1.0f;
        [Header("Tire Deformation")]
        [SerializeField] private float pressureMultiplier = 2500f;
        [SerializeField] private float carcassBaseStiffness = 145000;
        [SerializeField] private float tirePressureInPSI = 35f;
        [SerializeField] private float lateralStiffnessRatio = 0.6f;
        [SerializeField] private float gripGainedPerMeterOfDeflection = 0.15f;
        [Header("Weather Conditions")]
        [SerializeField] private float wetForwardStiffness = 0.9f;
        [SerializeField] private float wetSidewaysStiffness = 0.9f;
        [SerializeField] private float coldForwardStiffness = 0.6f;
        [SerializeField] private float coldSidewaysStiffness = 0.6f;
        [SerializeField] private float snowyForwardStiffness = 0.3f;
        [SerializeField] private float snowySidewaysStiffness = 0.3f;
        [SerializeField] private float icyForwardStiffness = 0.1f;
        [SerializeField] private float icySidewaysStiffness = 0.1f;

        public float GetForwardStiffness(float temperature, RoadSurfaceCondition roadSurfaceCondition)
        {
            if(roadSurfaceCondition == RoadSurfaceCondition.Icy)
            {
                return icyForwardStiffness;
            }
            else if(roadSurfaceCondition == RoadSurfaceCondition.Snowy)
            {
                return snowyForwardStiffness;
            }
            else if(temperature < COLD_WEATHER_TEMPERATURE)
            {
                return coldForwardStiffness;
            }
            else if(roadSurfaceCondition == RoadSurfaceCondition.Wet)
            {
                return wetForwardStiffness;
            }
            return defaultForwardStiffness;
        }

        public float GetSidewaysStiffness(float temperature, RoadSurfaceCondition roadSurfaceCondition)
        {
            if(roadSurfaceCondition == RoadSurfaceCondition.Icy)
            {
                return icySidewaysStiffness;
            }
            else if(roadSurfaceCondition == RoadSurfaceCondition.Snowy)
            {
                return snowySidewaysStiffness;
            }
            else if(temperature < COLD_WEATHER_TEMPERATURE)
            {
                return coldSidewaysStiffness;
            }
            else if(roadSurfaceCondition == RoadSurfaceCondition.Wet)
            {
                return wetSidewaysStiffness;
            }
            return defaultSidewaysStiffness;
        }

        public WheelFrictionCurve GetDefaultForwardFrictionCurve(float numberOfColliders, float temperature, RoadSurfaceCondition roadSurfaceCondition)
        {
            return new WheelFrictionCurve
            {
                extremumSlip = forwardExtremeSlip,
                extremumValue = forwardExtremeValue,
                asymptoteSlip = forwardAsymptoteSlip,
                asymptoteValue = forwardAsymptoteValue,
                stiffness = GetForwardStiffness(temperature, roadSurfaceCondition) / numberOfColliders,
            };
        }

        public WheelFrictionCurve GetDefaultSidewaysFrictionCurve(float numberOfColliders, float temperature, RoadSurfaceCondition roadSurfaceCondition)
        {
            return new WheelFrictionCurve
            {
                extremumSlip = sideWaysExtremeSlip,
                extremumValue = sideWaysExtremeValue,
                asymptoteSlip = sideWaysAsymptoteSlip,
                asymptoteValue = sideWaysAsymptoteValue,
                stiffness = GetSidewaysStiffness(temperature, roadSurfaceCondition)  / numberOfColliders
            };
        }
    }
}
