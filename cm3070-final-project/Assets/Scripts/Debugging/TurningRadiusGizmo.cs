using ModularVehicleSimulator.Vehicle;
using UnityEditor;
using UnityEngine;

namespace ModularVehicleSimulator.Debugging
{
    public class TurningRadiusGizmo : MonoBehaviour
    {
        [SerializeField] private Color debugColor = Color.yellow;
        private const float WIRE_SPHERE_RADIUS = 0.3f;
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
            if(wheels == null) return;

            Gizmos.color = debugColor;
            Handles.color = debugColor;

            if (frontRight != null && frontLeft != null && backRight != null && backLeft != null)
            {
                Vector3 front = (frontRight.transform.position + frontLeft.transform.position) / 2f;
                Vector3 back = (backRight.transform.position + backLeft.transform.position) / 2f;
                // Track
                Gizmos.DrawLine(frontRight.transform.position, frontLeft.transform.position);
                // Wheel Base
                Gizmos.DrawLine(front, back);
                if(Mathf.Abs(frontRight.SteeringAngle) > 0f)
                {
                    float wheelBase = Vector3.Distance(frontRight.transform.position, frontLeft.transform.position);
                    float turningRadius = wheelBase / Mathf.Tan(frontRight.SteeringAngle * Mathf.Deg2Rad);
                    Vector3 turningRadiusCenter = back + transform.right * turningRadius;

                    // Draw Turning Center
                    Gizmos.DrawLine(backLeft.transform.position, turningRadiusCenter);                
                    Gizmos.DrawLine(frontRight.transform.position, turningRadiusCenter);                
                    Gizmos.DrawLine(frontLeft.transform.position, turningRadiusCenter);                
                    Gizmos.DrawWireSphere(turningRadiusCenter, WIRE_SPHERE_RADIUS);

                    // Draw turning arcs
                    foreach (Wheel wheel in wheels)
                    {
                        Vector3 direction = wheel.transform.forward;
                        float steeringAngle = frontRight.SteeringAngle;
                        if(wheel.IsRight)
                        {
                            steeringAngle = wheel.LeftSteeringAngle;
                        }
                        else if(wheel.IsLeft)
                        {
                            steeringAngle = wheel.RightSteeringAngle;
                        }
                        turningRadius = wheelBase / Mathf.Tan(steeringAngle * Mathf.Deg2Rad);

                        if(frontRight.SteeringAngle < 0f)
                        {
                            Handles.DrawWireArc(wheel.transform.position + transform.right * turningRadius, -wheel.transform.up, -direction, -ARC_ANGLE, turningRadius);                                                        
                        }
                        else
                        {
                            Handles.DrawWireArc(wheel.transform.position + transform.right * turningRadius, wheel.transform.up, direction, -ARC_ANGLE, turningRadius);                            
                        }
                    }
                }


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
