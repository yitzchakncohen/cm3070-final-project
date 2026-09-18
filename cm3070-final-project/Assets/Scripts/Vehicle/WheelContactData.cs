using UnityEngine;

namespace ModularVehicleSimulator
{
    public struct WheelContactData
    {
        public int hitCount;
        public float distance;
        public Vector3 normal;
        public Vector3 point;
        public Collider collider;
        public Transform transform;
    }    
}