using System;
using System.Collections.Generic;
using ModularVehicleSimulator.Vehicle.Data;
using UnityEngine;

namespace ModularVehicleSimulator.UI.VehicleSettings
{
    public class GearRatioList : MonoBehaviour
    {
        public event Action<List<GearRatio>> OnValueChanged;
        [SerializeField] private GearRatioRow gearRatioRowPrefab;
        [SerializeField] private Transform gearListContent;
        private List<GearRatioRow> gearRatioRows;
        private bool isInitialized = false;

        public void Init(List<GearRatio> gearRatios)
        {
            foreach (Transform child in gearListContent)
            {
                Destroy(child.gameObject);
            }

            gearRatioRows = new List<GearRatioRow>();
            foreach (GearRatio gearRatio in gearRatios)
            {
                GearRatioRow gearRatioRow = Instantiate(gearRatioRowPrefab, gearListContent);
                gearRatioRow.Init(gearRatio);
                gearRatioRows.Add(gearRatioRow);
                gearRatioRow.OnValueChanged += GearRatioRow_OnValueChanged;
            }

            isInitialized = true;
        }

        private void OnDestroy()
        {
            if(!isInitialized) return;

            foreach (GearRatioRow gearRatioRow in gearRatioRows)
            {
                gearRatioRow.OnValueChanged -= GearRatioRow_OnValueChanged;
            }
        }

        private void GearRatioRow_OnValueChanged(GearRatioRow gearRatioRow)
        {
            if(!isInitialized) return;

            List<GearRatio> gearRatios = gearRatioRows.ConvertAll(row => new GearRatio{Gear = row.GearRatio.Gear, Ratio = row.GearRatio.Ratio});
            OnValueChanged?.Invoke(gearRatios);
        }
    }
}
