#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using ModularVehicleSimulator.Vehicle;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.UI.VehicleSettings.Editor
{
    public static class SettingsUIGenerator
    {
        public static VehicleSettingsData GenerateUI(VehicleConfiguration vehicleConfiguration)
        {
            VehicleSettingsData vehicleSettings = GenerateVehicleSettings(vehicleConfiguration);
            return vehicleSettings;
        }

        private static VehicleSettingsData GenerateVehicleSettings(VehicleConfiguration vehicleConfiguration)
        {
            Type type = vehicleConfiguration.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Dictionary<string, VehicleSettingsGroupData> settingsGroups = new Dictionary<string, VehicleSettingsGroupData>();

            foreach (FieldInfo field in fields)
            {
                // Skip hidden fields.
                if (IsSkippable(field)) continue;

                Type fieldType = field.FieldType;

                if (typeof(ScriptableObject).IsAssignableFrom(fieldType))
                {
                    ScriptableObject childScriptableObject = field.GetValue(vehicleConfiguration) as ScriptableObject;

                    if(childScriptableObject != null)
                    {
                        VehicleSettingsGroupData group = CreateVehicleSettingsGroup(childScriptableObject);
                        settingsGroups.Add(field.Name, group);                        
                    }
                }
            }

            return new VehicleSettingsData{Name = vehicleConfiguration.Name, VehicleSettingsGroups = settingsGroups};
        }

        private static VehicleSettingsGroupData CreateVehicleSettingsGroup(ScriptableObject parentField)
        {
            Type type = parentField.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Dictionary<FieldInfo, float> floatSettings = new Dictionary<FieldInfo, float>();
            Dictionary<FieldInfo, bool> boolSettings = new Dictionary<FieldInfo, bool>();
            Dictionary<FieldInfo, EngineType> engineTypeSettings = new Dictionary<FieldInfo, EngineType>();
            Dictionary<FieldInfo, Vector3> vector3Settings = new Dictionary<FieldInfo, Vector3>();
            Dictionary<FieldInfo, List<GearRatio>> gearRatioSettings = new Dictionary<FieldInfo, List<GearRatio>>();
            
            foreach (FieldInfo field in fields)
            {
                if (IsSkippable(field)) continue;

                Type fieldType = field.FieldType;

                if(typeof(float).IsAssignableFrom(fieldType))
                {
                    floatSettings.Add(field, (float)field.GetValue(parentField));
                }
                else if(typeof(bool).IsAssignableFrom(fieldType))
                {
                    boolSettings.Add(field, (bool)field.GetValue(parentField));
                }
                else if(typeof(EngineType).IsAssignableFrom(fieldType))
                {
                    engineTypeSettings.Add(field, (EngineType)field.GetValue(parentField));
                }
                else if(typeof(Vector3).IsAssignableFrom(fieldType))
                {
                    vector3Settings.Add(field, (Vector3)field.GetValue(parentField));
                }
                else if(typeof(List<GearRatio>).IsAssignableFrom(fieldType))
                {
                    gearRatioSettings.Add(field, (List<GearRatio>)field.GetValue(parentField));
                }
            }

            return new VehicleSettingsGroupData{ScriptableObject = parentField, 
                                                BoolSettings = boolSettings, 
                                                FloatSettings = floatSettings,
                                                EngineTypeSettings = engineTypeSettings,
                                                Vector3Settings = vector3Settings,
                                                GearRatioSettings = gearRatioSettings};
        }

        private static bool IsSkippable(FieldInfo fieldInfo)
        {
            // Skip hidden fields.
            if (fieldInfo.IsPrivate && fieldInfo.GetCustomAttribute<SerializeField>() == null) return true;
            if (fieldInfo.GetCustomAttribute<HideInInspector>() != null) return true;
            return false;
        }
    }   

    public struct VehicleSettingsData
    {
        public string Name;
        public Dictionary<string, VehicleSettingsGroupData> VehicleSettingsGroups;
    }

    public struct VehicleSettingsGroupData
    {
        public ScriptableObject ScriptableObject;
        public Dictionary<FieldInfo, float> FloatSettings;
        public Dictionary<FieldInfo, bool> BoolSettings;
        public Dictionary<FieldInfo, EngineType> EngineTypeSettings;
        public Dictionary<FieldInfo, Vector3> Vector3Settings;
        public Dictionary<FieldInfo, List<GearRatio>> GearRatioSettings;
    } 
}

#endif