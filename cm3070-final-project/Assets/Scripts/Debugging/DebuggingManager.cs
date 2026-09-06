using System;
using ModularVehicleSimulator.UI;
using ModularVehicleSimulator.Vehicle;
using UnityEngine;

namespace ModularVehicleSimulator.Debugging
{
    public class DebuggingManager : MonoBehaviour
    {
        public DebuggingTool[] Tools => tools;
        [SerializeField] private DebuggingMode debuggingModes;
        private DebuggingTool[] tools;
        private VehicleController vehicleController;
        private VehicleSelectionMenu vehicleSelectionMenu;

        private void Start()
        {
            vehicleController = FindAnyObjectByType<VehicleController>(FindObjectsInactive.Exclude);
            EnableTools();
            vehicleSelectionMenu = FindAnyObjectByType<VehicleSelectionMenu>(FindObjectsInactive.Include);
            vehicleSelectionMenu.OnChangeVehicle += VehicleSelectionMenu_OnChangeVehicle;
        }

        private void OnDestroy()
        {
            vehicleSelectionMenu.OnChangeVehicle -= VehicleSelectionMenu_OnChangeVehicle;            
        }

        [ContextMenu("Update Tools")]
        public void EnableTools()
        {
            tools = vehicleController.GetComponentsInChildren<DebuggingTool>();
            foreach (DebuggingTool tool in tools)
            {
                if (debuggingModes.HasFlag(tool.Mode))
                {
                    tool.Enable();
                }
                else
                {
                    tool.Disable();
                }
            }
        }

        private void VehicleSelectionMenu_OnChangeVehicle(VehicleController controller)
        {
            vehicleController = controller;
            EnableTools();
        }
    }
    
    [System.Flags]
    public enum DebuggingMode
    {
        None     = 0,     
        CenterOfMass     = 1 << 0,   
        TireFriction    = 1 << 1,   
        TurningRadius    = 1 << 2,
        AirResistance    = 1 << 3,
    }
}
