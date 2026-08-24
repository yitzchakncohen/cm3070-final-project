using System;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Data
{
    [CreateAssetMenu(fileName = "EngineConfiguration", menuName = "Vehicle Simulator/EngineConfiguration")]
    public class EngineConfiguration : ScriptableObject
    {
        public EngineType Type => engineType;
        public float Inertia => inertiaInKgSquareMeters;
        public float IdleRPM => idleRPM;
        public float MaxRPM => maxRPM;
        public float MinAutoShiftTime => minAutomaticTransmissionShiftTime;
        public bool IsAutomaticTransmision => isAutomaticTransmision;
        [SerializeField] private EngineType engineType;
        [SerializeField] private bool isAutomaticTransmision = true;
        [SerializeField] private float minAutomaticTransmissionShiftTime = 1.0f;
        [SerializeField] private float inertiaInKgSquareMeters = 0.2f;
        [Header("Torque Curve")]
        [SerializeField] private float idleRPM = 800f;
        [SerializeField] private float peakTorqueRPM = 5000f;
        [SerializeField] private float maxRPM = 6800f;
        [SerializeField] private float peakTorqueInNewtonMeters = 344f;
        [SerializeField] private float idleTorqueMultiplier = 0.70f;
        [SerializeField] private float maxTorqueMultiplier = 0.82f;
        [SerializeField] private AnimationCurve torqueCurve;
        [Header("Friction Curve")]
        [SerializeField] private float minFrictionInNewtonMeters = 15f;
        [SerializeField] private float peakTorqueFrictionInNewtonMeters = 30f;
        [SerializeField] private float maxRPMFrictionInNewtonMeters = 42f;
        [SerializeField] private AnimationCurve frictionCurve;

        public float GetTorque(float currentRPM)
        {
            if(engineType == EngineType.Electric) return peakTorqueInNewtonMeters;
            if(currentRPM < idleRPM) return torqueCurve.Evaluate(idleRPM);
            if(currentRPM > maxRPM) return 0f;

            return torqueCurve.Evaluate(currentRPM);
        }

        public float GetFriction(float currentRPM)
        {
            if(engineType == EngineType.Electric) return maxRPMFrictionInNewtonMeters;
            if(currentRPM < idleRPM) return minFrictionInNewtonMeters;
            if(currentRPM > maxRPM) return maxRPMFrictionInNewtonMeters;

            return frictionCurve.Evaluate(currentRPM);
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
            GenerateFrictionCurve();
        }

        private void GenerateTorqueCurve()
        {
            torqueCurve = new AnimationCurve();
            Keyframe idle = new Keyframe(idleRPM, idleTorqueMultiplier * peakTorqueInNewtonMeters);
            Keyframe peak = new Keyframe(peakTorqueRPM, peakTorqueInNewtonMeters);
            Keyframe max = new Keyframe(maxRPM, maxTorqueMultiplier * peakTorqueInNewtonMeters);
            torqueCurve.AddKey(idle);
            torqueCurve.AddKey(peak);
            torqueCurve.AddKey(max);
        }

        private void GenerateFrictionCurve()
        {
            frictionCurve = new AnimationCurve();
            Keyframe idle = new Keyframe(idleRPM, minFrictionInNewtonMeters);
            Keyframe peak = new Keyframe(peakTorqueRPM, peakTorqueFrictionInNewtonMeters);
            Keyframe max = new Keyframe(maxRPM, maxTorqueMultiplier * maxRPMFrictionInNewtonMeters);
            frictionCurve.AddKey(idle);
            frictionCurve.AddKey(peak);
            frictionCurve.AddKey(max);
        }        
    }
}
