using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using ModularVehicleSimulator.Vehicle;
using ModularVehicleSimulator.Vehicle.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModularVehicleSimulator.UI.VehicleSettings
{
    public class VehicleSettingsMenu : MonoBehaviour
    {
        private const float TOOL_TIP_SCREEN_BUFFER = 20f;
        public event Action OnUpdateField;
        [SerializeField] private VehicleConfiguration vehicleConfiguration;
        [SerializeField] private VehicleSettingsGroup groupPrefab;
        [SerializeField] private VehicleSetting settingPrefab;
        [SerializeField] private GameObject columnPrefab;
        [SerializeField] private Transform columnsContainer;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private int columnRowMax = 8;
        [SerializeField] private RectTransform toolTip;
        private TMP_Text toolTipText;
        private Dictionary<VehicleSetting, FieldInfo> globalSettingsList = new Dictionary<VehicleSetting, FieldInfo>();
        private void Awake()
        {
            GenerateUI();
            toolTipText = toolTip.GetComponentInChildren<TMP_Text>();
            HideTooltip();
        }

        public void UpdateVehicle(VehicleConfiguration newVehicleConfiguration)
        {
            vehicleConfiguration = newVehicleConfiguration;
            GenerateUI();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void UnsubscribeEvents()
        {
            foreach (VehicleSetting setting in globalSettingsList.Keys)
            {
                setting.OnEnter -= VehicleSetting_OnEnter;
                setting.OnExit -= VehicleSetting_OnExit;
            }
        }

        [ContextMenu("Generate UI")]
        public void GenerateUI()
        {
            UnsubscribeEvents();
            globalSettingsList.Clear();

            subtitle.text = vehicleConfiguration.Name;
            // Initialize Column Container
            for (int i = 0; i < columnsContainer.childCount; i++)
            {
                Destroy(columnsContainer.GetChild(i).gameObject);
            }

            VehicleSettingsData vehicleSettings = SettingsUIGenerator.GenerateUI(vehicleConfiguration);
            // Initialize Column 
            GameObject currentColumn = Instantiate(columnPrefab, columnsContainer);
            for (int i = 0; i < currentColumn.transform.childCount; i++)
            {
                Destroy(currentColumn.transform.GetChild(i).gameObject);
            }
            int columnRowCount = 0;

            foreach (KeyValuePair<string, VehicleSettingsGroupData> group in vehicleSettings.VehicleSettingsGroups)
            {
                int groupsPerGroup = 1;
                if (columnRowCount + 1 > columnRowMax)
                {
                    CreateNewColumn(out currentColumn, out columnRowCount);
                }
                
                VehicleSettingsGroup vehicleSettingsGroup = Instantiate(groupPrefab, currentColumn.transform);
                List<VehicleSetting> groupSettingsList = new List<VehicleSetting>();

                columnRowCount++; // Title row

                foreach (KeyValuePair<FieldInfo, float> setting in group.Value.FloatSettings)
                {
                    VehicleSetting vehicleSetting = CreateVehicleSetting(
                        ref currentColumn, 
                        ref columnRowCount, 
                        ref vehicleSettingsGroup, 
                        ref groupSettingsList, 
                        ref groupsPerGroup, 
                        setting, 
                        group.Key);
                    vehicleSetting.Init(CamelCaseToName(setting.Key.Name), setting.Value, UpdateField(group.Value.ScriptableObject, setting, OnUpdateField));
                }
                foreach (KeyValuePair<FieldInfo, bool> setting in group.Value.BoolSettings)
                {
                    VehicleSetting vehicleSetting = CreateVehicleSetting(
                        ref currentColumn, 
                        ref columnRowCount, 
                        ref vehicleSettingsGroup, 
                        ref groupSettingsList, 
                        ref groupsPerGroup, 
                        setting, 
                        group.Key);
                    vehicleSetting.Init(CamelCaseToName(setting.Key.Name), setting.Value, UpdateField(group.Value.ScriptableObject, setting, OnUpdateField));
                }
                foreach (KeyValuePair<FieldInfo, EngineType> setting in group.Value.EngineTypeSettings)
                {
                    VehicleSetting vehicleSetting = CreateVehicleSetting(
                        ref currentColumn, 
                        ref columnRowCount, 
                        ref vehicleSettingsGroup, 
                        ref groupSettingsList, 
                        ref groupsPerGroup, 
                        setting, 
                        group.Key);
                    vehicleSetting.Init(CamelCaseToName(setting.Key.Name), setting.Value, UpdateEnumField<EngineType>(group.Value.ScriptableObject, setting.Key, OnUpdateField));
                }
                foreach (KeyValuePair<FieldInfo, List<GearRatio>> setting in group.Value.GearRatioSettings)
                {
                    VehicleSetting vehicleSetting = CreateVehicleSetting(
                        ref currentColumn, 
                        ref columnRowCount, 
                        ref vehicleSettingsGroup, 
                        ref groupSettingsList, 
                        ref groupsPerGroup, 
                        setting, 
                        group.Key);
                    vehicleSetting.Init(CamelCaseToName(setting.Key.Name), setting.Value, UpdateField(group.Value.ScriptableObject, setting, OnUpdateField));
                }
                foreach (KeyValuePair<FieldInfo, Vector3> setting in group.Value.Vector3Settings)
                {
                    VehicleSetting vehicleSetting = CreateVehicleSetting(
                        ref currentColumn, 
                        ref columnRowCount, 
                        ref vehicleSettingsGroup, 
                        ref groupSettingsList, 
                        ref groupsPerGroup, 
                        setting, 
                        group.Key);
                    vehicleSetting.Init(CamelCaseToName(setting.Key.Name), setting.Value, UpdateField(group.Value.ScriptableObject, setting, OnUpdateField));
                }
                
                // Finalize active group if it has more than zero settings.
                if (groupSettingsList.Count > 0)
                {
                    string title = groupsPerGroup > 1 ? $"{group.Key} {groupsPerGroup}" : group.Key;
                    vehicleSettingsGroup.Init(CamelCaseToName(title), groupSettingsList);
                }
                else
                {
                    // Clean up empty group
                    Destroy(vehicleSettingsGroup.gameObject);
                    columnRowCount--; 
                }
            }
        }

        private VehicleSetting CreateVehicleSetting<T>(
            ref GameObject currentColumn, 
            ref int columnRowCount, 
            ref VehicleSettingsGroup vehicleSettingsGroup, 
            ref List<VehicleSetting> groupSettingsList, 
            ref int groupsPerGroup, 
            KeyValuePair<FieldInfo, T> setting, 
            string groupName)
        {
            CheckColumnRowCount(
                ref currentColumn, 
                ref columnRowCount, 
                ref vehicleSettingsGroup, 
                ref groupSettingsList, 
                ref groupsPerGroup, 
                groupName
                );
            
            VehicleSetting vehicleSetting = Instantiate(settingPrefab);
            groupSettingsList.Add(vehicleSetting);
            AddToGlobalList(setting.Key, vehicleSetting);
            columnRowCount++;

            return vehicleSetting;
        }

        private void CheckColumnRowCount(
            ref GameObject currentColumn, 
            ref int columnRowCount, 
            ref VehicleSettingsGroup vehicleSettingsGroup, 
            ref List<VehicleSetting> groupSettingsList, 
            ref int groupsPerGroup, 
            string baseGroupName)
        {
            Debug.Log(baseGroupName + " | columnRowCount: " + columnRowCount);
            if (columnRowCount >= columnRowMax)
            {
                Debug.Log(groupSettingsList.Count);
                if (groupSettingsList.Count > 0)
                {
                    string title = groupsPerGroup > 1 ? $"{baseGroupName} {groupsPerGroup}" : baseGroupName;
                    vehicleSettingsGroup.Init(CamelCaseToName(title), groupSettingsList);
                }
                else
                {
                    Destroy(vehicleSettingsGroup.gameObject);
                }

                CreateNewColumn(out currentColumn, out columnRowCount);
                groupsPerGroup++;

                vehicleSettingsGroup = Instantiate(groupPrefab, currentColumn.transform);
                groupSettingsList = new List<VehicleSetting>();
                columnRowCount++;
            }
        }

        private void CreateNewColumn(out GameObject currentColumn, out int columnRowCount)
        {
            currentColumn = Instantiate(columnPrefab, columnsContainer);
            columnRowCount = 0;
            for (int i = 0; i < currentColumn.transform.childCount; i++)
            {
                Destroy(currentColumn.transform.GetChild(i).gameObject);
            }
        }

        private void AddToGlobalList(FieldInfo fieldInfo, VehicleSetting vehicleSetting)
        {
            vehicleSetting.OnEnter += VehicleSetting_OnEnter;
            vehicleSetting.OnExit += VehicleSetting_OnExit;
            globalSettingsList.Add(vehicleSetting, fieldInfo);
        }

        private static Action<T> UpdateField<T>(ScriptableObject scriptableObject, KeyValuePair<FieldInfo, T> setting, Action updateFieldevent)
        {
            return (newValue) =>
            {
                setting.Key.SetValue(scriptableObject, newValue);
                updateFieldevent?.Invoke();
            };
        }

        private static Action<object> UpdateEnumField<T>(ScriptableObject scriptableObject, FieldInfo field, Action updateFieldevent) where T : Enum
        {
            return (newValue) =>
            {
                field.SetValue(scriptableObject, newValue);
                updateFieldevent?.Invoke();
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

        private void HideTooltip()
        {
            if (toolTip != null) toolTip.gameObject.SetActive(false);
        }

        private void ShowTooltip(string text, RectTransform rectTransform)
        {
            toolTip.pivot = new Vector2(0f, 0f);
            toolTip.anchorMin = new Vector2(0f, 0f);
            toolTip.anchorMax = new Vector2(0f, 0f);
            toolTipText.text = text;
            toolTip.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(toolTip);
            ClampToScreen(toolTip, rectTransform);
        }

        private void VehicleSetting_OnEnter(VehicleSetting setting)
        {
            if (globalSettingsList.ContainsKey(setting))
            {
                TooltipAttribute tooltipAttribute = globalSettingsList[setting].GetCustomAttribute<TooltipAttribute>();
                if (tooltipAttribute != null)
                {
                    ShowTooltip(tooltipAttribute.tooltip, setting.GetComponent<RectTransform>());
                }
            }
        }

        private void VehicleSetting_OnExit(VehicleSetting setting)
        {
            HideTooltip();
        }

        private void ClampToScreen(RectTransform rectTransform, RectTransform targetRectTransform)
        {
            // Vertically based on setting position. 
            Vector3 currentPos = targetRectTransform.position;
            float yOffset = targetRectTransform.rect.height * targetRectTransform.lossyScale.y;
            currentPos.y += yOffset;

            // Horizontally within the screen space.
            float tooltipWidth = rectTransform.rect.width * rectTransform.lossyScale.x;
            float xOffset = tooltipWidth / 2f;
            currentPos.x -= xOffset;
            // With pivot at (0,0), currentPos.x is the left edge of the tooltip
            float minX = TOOL_TIP_SCREEN_BUFFER;
            float maxX = Screen.width - tooltipWidth - TOOL_TIP_SCREEN_BUFFER;

            currentPos.x = Mathf.Clamp(currentPos.x, minX, maxX);

            rectTransform.position = currentPos;
        }
    }
}
