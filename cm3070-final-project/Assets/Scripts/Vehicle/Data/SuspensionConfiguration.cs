using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
    [CreateAssetMenu(fileName = "SuspensionConfiguration", menuName = "Vehicle Simulator/SuspensionConfiguration")]
    public class SuspensionConfiguration : ScriptableObject
    {
        public float Distance => suspensionDistanceInMeters;
        public float ForceAppPointOffset => forceAppPointOffsetInMeters;
        [Tooltip("The vertical height of the suspension system \n(the unloaded height of the spring).")]
        [SerializeField] private float suspensionDistanceInMeters = 0.3f;
        [Tooltip("The vertical height at which the \nforce of the suspension is applied.")]
        [SerializeField] private float forceAppPointOffsetInMeters = 0.3f;
        [Tooltip("The spring constant of the supsension \nspring for the front wheels.")]
        [SerializeField] private float frontSpringConstant = 30000f;
        [Tooltip("The spring constant of the supsension \nspring for the rear wheels.")]
        [SerializeField] private float backSpringConstant = 20000f;
        [Tooltip("The damping value of the suspension hydraulic damping.")]
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

