using System;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
    [CreateAssetMenu(fileName = "EngineConfiguration", menuName = "Vehicle Simulator/EngineConfiguration")]
    public class EngineConfiguration : ScriptableObject
    {
        public EngineType Type => engineType;
        [SerializeField] private EngineType engineType;
        [SerializeField] private float idleRPM = 800f;
        [SerializeField] private float peakTorqueRPM = 5000f;
        [SerializeField] private float maxRPM = 6800f;
        [SerializeField] private float peakTorqueInNewtownsMeters = 344f;
        [SerializeField] private float idleTorqueMultiplier = 0.70f;
        [SerializeField] private float maxTorqueMultiplier = 0.82f;
        [SerializeField] private AnimationCurve torqueCurve;

        public float GetTorque(float currentRPM)
        {
            if(engineType == EngineType.Electric) return peakTorqueInNewtownsMeters;
            if(currentRPM < idleRPM) return torqueCurve.Evaluate(idleRPM);
            if(currentRPM > maxRPM) return 0f;

            return torqueCurve.Evaluate(currentRPM);
        }

        private void OnEnable()
        {
            if(torqueCurve == null || torqueCurve.length < 3)
            {
                GenerateTorqueCurve();
            }
        }

        private void OnValidate()
        {
            GenerateTorqueCurve();
        }

        private void GenerateTorqueCurve()
        {
            torqueCurve = new AnimationCurve();
            Keyframe idle = new Keyframe(idleRPM, idleTorqueMultiplier * peakTorqueInNewtownsMeters);
            Keyframe peak = new Keyframe(peakTorqueRPM, peakTorqueInNewtownsMeters);
            Keyframe max = new Keyframe(maxRPM, maxTorqueMultiplier * peakTorqueInNewtownsMeters);
            torqueCurve.AddKey(idle);
            torqueCurve.AddKey(peak);
            torqueCurve.AddKey(max);
        }
        
    }
}
