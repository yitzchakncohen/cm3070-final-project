using ModularVehicleSimulator.Vehicle;
using UnityEngine;


namespace ModularVehicleSimulator.Debugging
{
    public class TireFrictionGizmo : DebuggingTool
    {
        [SerializeField] private Color debugColor = Color.orange;
        private const float TIRE_FRICTION_LINE_MAX = 1f;
        private const float NEWTON_TO_METER_SCALING = 1000f;
        private const float ARC_ANGLE = 90f;
        private Wheel[] wheels = null;
        private Wheel frontLeft = null;
        private Wheel frontRight = null;
        private Wheel backLeft = null;
        private Wheel backRight = null;

        private void Start()
        {
            GetWheelTransforms();            
        }

        private void OnDrawGizmos()
        {
            if(!isDebuggingEnabled) return;
            if(wheels == null) return;

            Gizmos.color = debugColor;      

            foreach (Wheel wheel in wheels)
            {
                Gizmos.DrawLine(wheel.WheelContactPoint, wheel.WheelContactPoint + (wheel.WheelFriction / NEWTON_TO_METER_SCALING));
            }
        }

        private void GetWheelTransforms()
        {
            if (wheels == null)
            {
                wheels = GetComponentsInChildren<Wheel>();
            }
            foreach (Wheel wheel in wheels)
            {
                if (wheel.IsFront && wheel.IsLeft)
                {
                    frontLeft = wheel;
                }
                else if (wheel.IsFront && wheel.IsRight)
                {
                    frontRight = wheel;
                }
                else if (!wheel.IsFront && wheel.IsLeft)
                {
                    backLeft = wheel;
                }
                else if (!wheel.IsFront && wheel.IsRight)
                {
                    backRight = wheel;
                }
            }
        }
    }
}