using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
    [CreateAssetMenu(fileName = "SuspensionConfiguration", menuName = "Vehicle Simulator/SuspensionConfiguration")]
    public class SuspensionConfiguration : ScriptableObject
    {
        public float Distance => suspensionDistanceInMeters = 0.3f;
        public float FrontSpring => frontSpringConstant = 30000f;
        public float BackSpring => backSpringConstant = 20000f;
        public float Damper => damper = 3000f;
        [SerializeField] private float suspensionDistanceInMeters = 0.3f;
        [SerializeField] private float frontSpringConstant = 30000f;
        [SerializeField] private float backSpringConstant = 20000f;
        [SerializeField] private float damper = 3000f;
    }
}

