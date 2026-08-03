using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ModularVehicleSimulator
{
    public class Weather : MonoBehaviour
    {
        private const float WIND_SPEED_MAX = 35f;
        private const float RAIN_WIND_MULTIPLIER = 0.3f;
        private const float SNOW_WIND_MULTIPLIER = 0.6f;
        private const float FX_CAMERA_OFFSET = 4f;
        // Singleton pattern for weather
        public static Weather Instance;
        public float Temperature => temperature;
        public RoadSurfaceCondition RoadSurfaceCondition => roadSurfaceCondition;
        public Vector3 WindVelocity => windVelocity;

        [SerializeField] private RoadSurfaceCondition roadSurfaceCondition = RoadSurfaceCondition.None;
        [SerializeField] private Precipitation precipitation = Precipitation.None;
        [SerializeField] private Vector3 windVelocity = Vector3.zero;
        [SerializeField] private float temperature = 20f;
        [SerializeField] private ParticleSystem rainFX;
        [SerializeField] private ParticleSystem snowFX;
        [SerializeField] private ParticleSystem windFX;

        private void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
                UpdateWeather();
            }
            else
            {
                DestroyImmediate(this);
            }
        }

        private void OnValidate()
        {
            if(Application.isPlaying)
            {
                UpdateWeather();
            }
        }

        private void FixedUpdate()
        {
            Transform cameraTransform = Camera.main.transform;
            rainFX.transform.position = cameraTransform.position + cameraTransform.forward * FX_CAMERA_OFFSET;
            windFX.transform.position = cameraTransform.position + cameraTransform.forward * FX_CAMERA_OFFSET;
            snowFX.transform.position = cameraTransform.position + cameraTransform.forward * FX_CAMERA_OFFSET;
        }

        private void OnDestroy()
        {
            if(Instance == this)
            {
                Instance = null;
            }
        }

        [ContextMenu("Randomize Weather")]
        public void RandomizeWeather()
        {
            windVelocity = new Vector3(Random.Range(0, WIND_SPEED_MAX) / Mathf.Sqrt(2), Random.Range(0, WIND_SPEED_MAX) / Mathf.Sqrt(2), 0f);
            roadSurfaceCondition = GetRandomValue<RoadSurfaceCondition>();
            if(roadSurfaceCondition == RoadSurfaceCondition.Wet)
            {
                precipitation = Precipitation.Rain;
            }
            else if(roadSurfaceCondition == RoadSurfaceCondition.Snowy || roadSurfaceCondition == RoadSurfaceCondition.Icy)
            {
                precipitation = Precipitation.Snow;
            }
            else
            {
                precipitation = Precipitation.None;
            }
            UpdateWeather();
        }

        [ContextMenu("Update Wather")]
        public void UpdateWeather()
        {
            if(precipitation == Precipitation.Rain)
            {
                rainFX.gameObject.SetActive(true);
                snowFX.gameObject.SetActive(false);
            }
            else if (precipitation == Precipitation.Snow)
            {
                rainFX.gameObject.SetActive(false);
                snowFX.gameObject.SetActive(true);
            }
            else
            {
                rainFX.gameObject.SetActive(false);
                snowFX.gameObject.SetActive(false);
            }

            if(WindVelocity.sqrMagnitude > 0)
            {
                windFX.gameObject.SetActive(true);
            }
            else
            {
                windFX.gameObject.SetActive(false);                
            }

            ParticleSystem.VelocityOverLifetimeModule rainVelocityModule = rainFX.velocityOverLifetime;
            rainVelocityModule.space = ParticleSystemSimulationSpace.World;
            rainVelocityModule.x = new ParticleSystem.MinMaxCurve(windVelocity.x * RAIN_WIND_MULTIPLIER);
            rainVelocityModule.y = new ParticleSystem.MinMaxCurve(windVelocity.y * RAIN_WIND_MULTIPLIER);
            rainVelocityModule.z = new ParticleSystem.MinMaxCurve(windVelocity.z * RAIN_WIND_MULTIPLIER);
            rainVelocityModule.enabled = true;

            ParticleSystem.VelocityOverLifetimeModule snowVelocityModule = snowFX.velocityOverLifetime;
            snowVelocityModule.space = ParticleSystemSimulationSpace.World;
            snowVelocityModule.x = new ParticleSystem.MinMaxCurve(windVelocity.x * SNOW_WIND_MULTIPLIER);
            snowVelocityModule.y = new ParticleSystem.MinMaxCurve(windVelocity.y * SNOW_WIND_MULTIPLIER);
            snowVelocityModule.z = new ParticleSystem.MinMaxCurve(windVelocity.z * SNOW_WIND_MULTIPLIER);
            snowVelocityModule.enabled = true;

            ParticleSystem.VelocityOverLifetimeModule windVelocityModule = windFX.velocityOverLifetime;
            windVelocityModule.space = ParticleSystemSimulationSpace.World;
            windVelocityModule.x = new ParticleSystem.MinMaxCurve(windVelocity.x);
            windVelocityModule.y = new ParticleSystem.MinMaxCurve(windVelocity.y);
            windVelocityModule.z = new ParticleSystem.MinMaxCurve(windVelocity.z);
            windVelocityModule.enabled = true;
        }
        
        // Generice helper for getting random enums;
        public static T GetRandomValue<T>() where T : struct, Enum
        {
            T[] values = (T[])Enum.GetValues(typeof(T));            
            return values[Random.Range(0, values.Length)];
        }
    }    

    public enum RoadSurfaceCondition
    {
        None = 0,
        Wet = 1,
        Snowy = 2,
        Icy = 3,
    }

    public enum Precipitation
    {
        None = 0,
        Rain = 1,
        Snow = 2,
    }
}
