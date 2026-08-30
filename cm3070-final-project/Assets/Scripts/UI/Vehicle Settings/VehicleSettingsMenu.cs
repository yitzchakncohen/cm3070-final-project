using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using ModularVehicleSimulator.UI.VehicleSettings.Editor;
using ModularVehicleSimulator.Vehicle;
using ModularVehicleSimulator.Vehicle.Data;
using TMPro;
using UnityEngine;

namespace ModularVehicleSimulator.UI.VehicleSettings
{
    public class VehicleSettingsMenu : MonoBehaviour
    {
        [SerializeField] private VehicleConfiguration vehicleConfiguration;
        [SerializeField] private VehicleSettingsGroup groupPrefab;
        [SerializeField] private VehicleSetting settingPrefab;
        [SerializeField] private GameObject columnPrefab;
        [SerializeField] private Transform columnsContainer;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private int columnRowMax = 8;
        
        private void Awake()
        {
            GenerateUI();
        }

        public void UpdateVehicle(VehicleConfiguration newVehicleConfiguration)
        {
            vehicleConfiguration = newVehicleConfiguration;
            GenerateUI();
        }

        [ContextMenu("Generate UI")]
        public void GenerateUI()
        {
            subtitle.text = vehicleConfiguration.Name;
            for (int i = 0; i < columnsContainer.childCount; i++)
            {
                Destroy(columnsContainer.GetChild(i).gameObject);
            }
            VehicleSettingsData vehicleSettings = SettingsUIGenerator.GenerateUI(vehicleConfiguration);
            GameObject currentColumn = Instantiate(columnPrefab, columnsContainer);
            for (int i = 0; i < currentColumn.transform.childCount; i++)
            {
                Destroy(currentColumn.transform.GetChild(i).gameObject);
            }
            int columnRowCount = 0;
            
            foreach (KeyValuePair<string, VehicleSettingsGroupData> group in vehicleSettings.VehicleSettingsGroups)
            {
                // Check column row count
                int columnCount = columnRowCount + group.Value.FloatSettings.Count + group.Value.BoolSettings.Count + 1;
                if(columnRowCount != 0 && columnCount > columnRowMax)
                {
                    currentColumn = Instantiate(columnPrefab, columnsContainer);
                    for (int i = 0; i < currentColumn.transform.childCount; i++)
                    {
                        Destroy(currentColumn.transform.GetChild(i).gameObject);
                    }
                    columnRowCount = 0;
                }

                VehicleSettingsGroup vehicleSettingsGroup = Instantiate(groupPrefab, currentColumn.transform);
                List<VehicleSetting> settingsList = new List<VehicleSetting>();
                columnRowCount++; // Title row

                foreach (KeyValuePair<FieldInfo, float> setting in group.Value.FloatSettings)
                {
                    VehicleSetting vehicleSetting = Instantiate(settingPrefab);
                    vehicleSetting.Init(CamelCaseToName(setting.Key.Name), setting.Value, UpdateField(group.Value.ScriptableObject, setting));
                    settingsList.Add(vehicleSetting);
                    columnRowCount++;
                }
                foreach (KeyValuePair<FieldInfo, bool> setting in group.Value.BoolSettings)
                {
                    VehicleSetting vehicleSetting = Instantiate(settingPrefab);
                    vehicleSetting.Init(CamelCaseToName(setting.Key.Name), setting.Value, UpdateField(group.Value.ScriptableObject, setting));
                    settingsList.Add(vehicleSetting);
                    columnRowCount++;
                }
                foreach (KeyValuePair<FieldInfo, EngineType> setting in group.Value.EngineTypeSettings)
                {
                    VehicleSetting vehicleSetting = Instantiate(settingPrefab);
                    vehicleSetting.Init(CamelCaseToName(setting.Key.Name), setting.Value, UpdateEnumField<EngineType>(group.Value.ScriptableObject, setting.Key));
                    settingsList.Add(vehicleSetting);
                    columnRowCount++;
                }
                foreach (KeyValuePair<FieldInfo, List<GearRatio>> setting in group.Value.GearRatioSettings)
                {
                    VehicleSetting vehicleSetting = Instantiate(settingPrefab);
                    vehicleSetting.Init(CamelCaseToName(setting.Key.Name), setting.Value, UpdateField(group.Value.ScriptableObject, setting));
                    settingsList.Add(vehicleSetting);
                    columnRowCount++;
                }
                foreach (KeyValuePair<FieldInfo, Vector3> setting in group.Value.Vector3Settings)
                {
                    VehicleSetting vehicleSetting = Instantiate(settingPrefab);
                    vehicleSetting.Init(CamelCaseToName(setting.Key.Name), setting.Value, UpdateField(group.Value.ScriptableObject, setting));
                    settingsList.Add(vehicleSetting);
                    columnRowCount++;
                }
                vehicleSettingsGroup.Init(CamelCaseToName(group.Key) ,settingsList);
            }
        }

        private static Action<T> UpdateField<T>(ScriptableObject scriptableObject, KeyValuePair<FieldInfo, T> setting)
        {
            return (newValue) =>
            {
                setting.Key.SetValue(scriptableObject, newValue);
            };
        }

        private static Action<object> UpdateEnumField<T>(ScriptableObject scriptableObject, FieldInfo field) where T : Enum
        {
            return (newValue) =>
            {
                field.SetValue(scriptableObject, newValue);
            };
        }

        // Replaces UnityEditor.ObjectNames.NicifyVariableName at runtime
        private static string CamelCaseToName(string text)
        {
            // Replace units
            string result = ReplaceUnits(text);

            // Insert space between an uppercase letter and a lowercase letter
            result = Regex.Replace(result, @"(?<=[A-Z])(?=[A-Z][a-z])", " ");

            // Insert space between lowercase/digit and uppercase
            result = Regex.Replace(result, @"(?<=[a-z0-9])(?=[A-Z])", " ");


            // Capitalize the first character
            return char.ToUpper(result[0]) + result.Substring(1);
        }

        private static string ReplaceUnits(string text)
        {
            text = text.Replace("InNewtonMeters", " [Nm]");
            text = text.Replace("InKgSquareMeters", " [Kgm^2]");
            text = text.Replace("InMetersPerSecond", " [m/s]");
            text = text.Replace("InMeters", " [m]");
            text = text.Replace("RPM", " [RPM]");
            text = text.Replace("PerMeter", " per m");
            text = text.Replace("InDegreesPerSecond", " [deg/s]");
            return text;
        }
    }
}
