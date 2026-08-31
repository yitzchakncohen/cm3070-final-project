using System;
using ModularVehicleSimulator.UI.VehicleSettings;
using UnityEngine;
using UnityEngine.UI;

namespace ModularVehicleSimulator.UI
{
    public class WeatherUI : MonoBehaviour
    {
        [SerializeField] private VehicleSetting precipitation;
        [SerializeField] private VehicleSetting roadSurfaceCondition;
        [SerializeField] private VehicleSetting windVelocity;
        [SerializeField] private VehicleSetting temperature;
        [SerializeField] private VehicleSetting gravity;
        [SerializeField] private Button randomButton;
        private Weather weather;

        private void Start()
        {
            weather = Weather.Instance;
            randomButton.onClick.AddListener(RandomizeWeather);
            Init();
        }

        private void OnDestroy()
        {
            randomButton.onClick.RemoveAllListeners();
        }

        private void Init()
        {
            precipitation.Init("Precipitiation", weather.Precipitation, UpdatePrecipitation);
            roadSurfaceCondition.Init("Road Surface Condition", weather.RoadSurfaceCondition, UpdateRoadSurfaceCondition);
            windVelocity.Init("Wind Velocity", weather.WindVelocity, UpdateWindVelocity);
            temperature.Init("Temperature", weather.Temperature, UpdateTemperature);
            gravity.Init("Gravity", UnityEngine.Physics.gravity, UpdateGravity);
        }

        private void UpdatePrecipitation(object precipitatioObject)
        {
            Precipitation precipitation = (Precipitation)precipitatioObject;
            weather.SetPrecipitation(precipitation);
        }

        private void UpdateRoadSurfaceCondition(object roadSurfaceConditionObject)
        {
            RoadSurfaceCondition roadSurfaceCondition = (RoadSurfaceCondition)roadSurfaceConditionObject;
            weather.SetRoadSurfaceCondition(roadSurfaceCondition);
        }

        private void UpdateWindVelocity(Vector3 windVelocity)
        {
            weather.SetWindVelocity(windVelocity);
        }

        private void UpdateTemperature(float temperature)
        {
            weather.SetTemperature(temperature);
        }

        private void UpdateGravity(Vector3 gravity)
        {
            UnityEngine.Physics.gravity = gravity;
        }

        private void RandomizeWeather()
        {
            weather.RandomizeWeather();
            Init();
        }
    }
}
