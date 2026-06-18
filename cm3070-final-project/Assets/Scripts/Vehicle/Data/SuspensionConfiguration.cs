using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
    [CreateAssetMenu(fileName = "SuspensionConfiguration", menuName = "Vehicle Simulator/SuspensionConfiguration")]
    public class SuspensionConfiguration : ScriptableObject
    {
        private float Distance => suspensionDistanceInMeters = 0.3f;
        private float FrontSpring => frontSpringConstant = 30000f;
        private float BackSpring => backSpringConstant = 20000f;
        private float Damper => damper = 3000f;
        [SerializeField] private float suspensionDistanceInMeters = 0.3f;
        [SerializeField] private float frontSpringConstant = 30000f;
        [SerializeField] private float backSpringConstant = 20000f;
        [SerializeField] private float damper = 3000f;
    }
}

