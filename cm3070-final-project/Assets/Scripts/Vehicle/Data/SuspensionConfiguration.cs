using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
    [CreateAssetMenu(fileName = "SuspensionConfiguration", menuName = "Vehicle Simulator/SuspensionConfiguration")]
    public class SuspensionConfiguration : ScriptableObject
    {
        public float Distance => suspensionDistanceInMeters;
        [SerializeField] private float suspensionDistanceInMeters = 0.3f;
        [SerializeField] private float frontSpringConstant = 30000f;
        [SerializeField] private float backSpringConstant = 20000f;
        [SerializeField] private float damper = 3000f;
        
        public JointSpring GetFrontSuspectionSpring(float targetPosition)
        {
            return new JointSpring
            {
                spring = frontSpringConstant,
                damper = damper,
                targetPosition = targetPosition
            };
        }

        public JointSpring GetBackSuspectionSpring(float targetPosition)
        {
            return new JointSpring
            {
                spring = backSpringConstant,
                damper = damper,
                targetPosition = targetPosition
            };
        }
    }
}

