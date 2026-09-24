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
        [Tooltip("Radius of the tires.")]
        [SerializeField] private float radiusInMeters = 0.3284f;
        [Tooltip("Width of the tires.")]
        [SerializeField] private float widthInMeters = 0.225f;
        [Tooltip("Weight of the wheel including the tires.")]
        [SerializeField] private float weightInKG = 20f;
        [Header("Friction")]
        [Header("Friction Forward")]
        [Tooltip("The slip (ratio of tire speed to ground speed) \nat maximum gripping force for forward motion of the wheel.")]
        [SerializeField] private float forwardExtremeSlip = 0.125f;
        [Tooltip("The maximum ratio of force applied by the tire \nat extreme slip for forward motion of the wheel.")]
        [SerializeField] private float forwardExtremeValue = 0.875f;
        [Tooltip("The slip ratio at which the force from the tires \nstops decreasing for forward motion of the wheel..")]
        [SerializeField] private float forwardAsymptoteSlip = 0.7f;
        [Tooltip("The force ratio applied by the tires once the asymptotic \nslip value has been reached for forward motion of the wheel.")]
        [SerializeField] private float forwardAsymptoteValue = 0.725f;
        [Tooltip("A multiplier for scaling the force values from \nthe friction curve on forward motion of the wheel.")]
        [SerializeField] private float forwardStiffness = 1.0f;
        [Header("Friction Sideways")]
        [Tooltip("The slip (ratio of tire speed to ground speed) \nat maximum gripping force for sideways motion of the wheel.")]
        [SerializeField] private float sidewaysExtremeSlip = 0.175f;
        [Tooltip("The maximum ratio of force applied by the tire \nat extreme slip for sideways motion of the wheel.")]
        [SerializeField] private float sidewaysExtremeValue = 0.875f;
        [Tooltip("The slip ratio at which the force from the tires \nstops decreasing for sideways motion of the wheel..")]
        [SerializeField] private float sidewaysAsymptoteSlip = 0.7f;
        [Tooltip("The force ratio applied by the tires once the asymptotic \nslip value has been reached for sideways motion of the wheel.")]
        [SerializeField] private float sidewaysAsymptoteValue = 0.725f;
        [Tooltip("A multiplier for scaling the force values from \n the friction curve on sideways motion of the wheel.")]
        [SerializeField] private float sidewaysStiffness = 1.0f;
        [Header("Tire Deformation")]
        [Tooltip("Multiplier for scaling from tire \npressure to deformation force.")]
        [SerializeField] private float pressureMultiplier = 2500f;
        [Tooltip("The rigidity of the tires uninflated carcass.")]
        [SerializeField] private float carcassBaseStiffness = 145000;
        [Tooltip("Tire pressure in pounds per square inch (PSI).")]
        [SerializeField] private float tirePressureInPSI = 35f;
        [Tooltip("The ratio of the vertical stiffness to lateral stiffness.")]
        [SerializeField] private float lateralStiffnessRatio = 0.6f;
        [Tooltip("The increase in grip provided by the increased \ncontact surface area of a deformed tire.")]
        [SerializeField] private float gripGainedPerMeterOfDeflection = 0.15f;
        [Header("Weather Conditions")]
        [Tooltip("Friction multplier for wet weather conditions \nfor the forward direction.")]
        [SerializeField] private float wetForwardFriction = 0.9f;
        [Tooltip("Friction multplier for wet weather conditions \nfor the sideways direction.")]
        [SerializeField] private float wetSidewaysFriction = 0.9f;
        [Tooltip("Friction multplier for cold weather conditions \nfor the forward direction.")]
        [SerializeField] private float coldForwardFriction = 0.6f;
        [Tooltip("Friction multplier for cold weather conditions \nfor the sideways direction.")]
        [SerializeField] private float coldSidewaysFriction = 0.6f;
        [Tooltip("Friction multplier for snowy weather conditions \nfor the forward direction.")]
        [SerializeField] private float snowyForwardFriction = 0.3f;
        [Tooltip("Friction multplier for snowy weather conditions \nfor the sideways direction.")]
        [SerializeField] private float snowySidewaysFriction = 0.3f;
        [Tooltip("Friction multplier for icy weather conditions \nfor the forward direction.")]
        [SerializeField] private float icyForwardFriction = 0.1f;
        [Tooltip("Friction multplier for icy weather conditions \nfor the sideways direction.")]
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
