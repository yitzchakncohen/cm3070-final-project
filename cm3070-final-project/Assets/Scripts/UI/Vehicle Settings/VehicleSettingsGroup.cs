using System.Collections.Generic;
using ModularVehicleSimulator.UI.VehicleSettings;
using TMPro;
using UnityEngine;

public class VehicleSettingsGroup : MonoBehaviour
{
    [SerializeField] private TMP_Text subtitle;
    [SerializeField] private Transform settingsContainer;

    public void Init(string settingName, List<VehicleSetting> settings)
    {
        foreach (Transform child in settingsContainer)
        {
            Destroy(child.gameObject);                
        }
        subtitle.text = settingName;
        foreach (VehicleSetting vehicleSetting in settings)
        {
            vehicleSetting.transform.SetParent(settingsContainer);
        }
    }
}
