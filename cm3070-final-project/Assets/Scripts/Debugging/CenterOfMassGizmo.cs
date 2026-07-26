using ModularVehicleSimulator.Vehicle;
using UnityEngine;

namespace ModularVehicleSimulator.Debugging
{
    public class CenterOfMassGizmo : DebuggingTool
    {
        private const float CENTER_OF_MASS_RADIUS = 0.5f;
        private const float VELOCITY_SCALING = 3.6f;
        [SerializeField] private Color debugColor = Color.green;
        private VehicleController vehicleController;

        private void Start()
        {
            vehicleController = GetComponent<VehicleController>();
        }

        private void OnDrawGizmos()
        {
            if(!isDebuggingEnabled) return;
            if (vehicleController == null) return;

            Vector3 centerOfMass = vehicleController.ChassisRigidBody.worldCenterOfMass;
            Gizmos.color = debugColor;      
            Gizmos.DrawWireSphere(centerOfMass, CENTER_OF_MASS_RADIUS);
            Gizmos.DrawLine(centerOfMass, centerOfMass + vehicleController.ChassisRigidBody.linearVelocity / VELOCITY_SCALING);
        }        
    }
}
