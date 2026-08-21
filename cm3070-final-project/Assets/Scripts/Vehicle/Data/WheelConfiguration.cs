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
        [SerializeField] private float forwardStiffness = 1.0f;
        [Header("Friction Sideways")]
        [SerializeField] private float sidewaysExtremeSlip = 0.175f;
        [SerializeField] private float sidewaysExtremeValue = 0.875f;
        [SerializeField] private float sidewaysAsymptoteSlip = 0.7f;
        [SerializeField] private float sidewaysAsymptoteValue = 0.725f;
        [SerializeField] private float sidewaysStiffness = 1.0f;
        [Header("Tire Deformation")]
        [SerializeField] private float pressureMultiplier = 2500f;
        [SerializeField] private float carcassBaseStiffness = 145000;
        [SerializeField] private float tirePressureInPSI = 35f;
        [SerializeField] private float lateralStiffnessRatio = 0.6f;
        [SerializeField] private float gripGainedPerMeterOfDeflection = 0.15f;
        [Header("Weather Conditions")]
        [SerializeField] private float wetForwardFriction = 0.9f;
        [SerializeField] private float wetSidewaysFriction = 0.9f;
        [SerializeField] private float coldForwardFriction = 0.6f;
        [SerializeField] private float coldSidewaysFriction = 0.6f;
        [SerializeField] private float snowyForwardFriction = 0.3f;
        [SerializeField] private float snowySidewaysFriction = 0.3f;
        [SerializeField] private float icyForwardFriction = 0.1f;
        [SerializeField] private float icySidewaysFriction = 0.1f;

        public float GetForwardWeatherFrictionMultiplier(float temperature, RoadSurfaceCondition roadSurfaceCondition)
        {
            if(roadSurfaceCondition == RoadSurfaceCondition.Icy)
            {
                return icyForwardFriction;
            }
            else if(roadSurfaceCondition == RoadSurfaceCondition.Snowy)
            {
                return snowyForwardFriction;
            }
            else if(temperature < COLD_WEATHER_TEMPERATURE)
            {
                return coldForwardFriction;
            }
            else if(roadSurfaceCondition == RoadSurfaceCondition.Wet)
            {
                return wetForwardFriction;
            }
            return 1f;
        }

        public float GetSidewaysWeatherFrictionMultiplier(float temperature, RoadSurfaceCondition roadSurfaceCondition)
        {
            if(roadSurfaceCondition == RoadSurfaceCondition.Icy)
            {
                return icySidewaysFriction;
            }
            else if(roadSurfaceCondition == RoadSurfaceCondition.Snowy)
            {
                return snowySidewaysFriction;
            }
            else if(temperature < COLD_WEATHER_TEMPERATURE)
            {
                return coldSidewaysFriction;
            }
            else if(roadSurfaceCondition == RoadSurfaceCondition.Wet)
            {
                return wetSidewaysFriction;
            }
            return 1f;
        }

        public WheelFrictionCurve GetDefaultForwardFrictionCurve()
        {
            return new WheelFrictionCurve
            {
                extremumSlip = forwardExtremeSlip,
                extremumValue = forwardExtremeValue,
                asymptoteSlip = forwardAsymptoteSlip,
                asymptoteValue = forwardAsymptoteValue,
                stiffness = forwardStiffness,
            };
        }

        public WheelFrictionCurve GetDefaultSidewaysFrictionCurve()
        {
            return new WheelFrictionCurve
            {
                extremumSlip = sidewaysExtremeSlip,
                extremumValue = sidewaysExtremeValue,
                asymptoteSlip = sidewaysAsymptoteSlip,
                asymptoteValue = sidewaysAsymptoteValue,
                stiffness = sidewaysStiffness
            };
        }
    }
}
