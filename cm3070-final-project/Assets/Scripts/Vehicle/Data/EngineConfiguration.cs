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
        [Tooltip("Gas or electric vehicle (EV).")]
        [SerializeField] private EngineType engineType;
        [Tooltip("Drive gears change automatically \nas RPM reaches max or idle.")]
        [SerializeField] private bool isAutomaticTransmision = true;
        [Tooltip("Minimum time between \nautomatic gear shifting.")]
        [SerializeField] private float minAutomaticTransmissionShiftTime = 1.0f;
        [Tooltip("Momemntum of the rotating engine \ncomponents (or simulated intertia in an EV)")]
        [SerializeField] private float inertiaInKgSquareMeters = 0.2f;
        [Header("Torque Curve")]
        [Tooltip("The engines rotations per minute (RPM) at idle.")]
        [SerializeField] private float idleRPM = 800f;
        [Tooltip("The RPM of the engine at maximum torque.")]
        [SerializeField] private float peakTorqueRPM = 5000f;
        [Tooltip("The maximum RPM the engine \ncan achieve (i.e engine creep).")]
        [SerializeField] private float maxRPM = 6800f;
        [Tooltip("The maximum amount of torque \nthe engine can produce.")]
        [SerializeField] private float peakTorqueInNewtonMeters = 344f;
        [Tooltip("The ratio of toqure between \npeak output and idle.")]
        [SerializeField] private float idleTorqueMultiplier = 0.70f;
        [Tooltip("The ratio of toqure between \npeak output and maximum RPM.")]
        [SerializeField] private float maxTorqueMultiplier = 0.82f;
        [Tooltip("The curve produced by the above \nfields where x is RPM and y is torque.")]
        [SerializeField] private AnimationCurve torqueCurve;
        [Header("Friction Curve")]
        [Tooltip("The minimum friction produced by \nthe engine (i.e. engine braking)")]
        [SerializeField] private float minFrictionInNewtonMeters = 15f;
        [Tooltip("The friction produced by the \nengine at maximum torque.")]
        [SerializeField] private float peakTorqueFrictionInNewtonMeters = 30f;
        [Tooltip("The friction produced by the \nengine at maximum RPM.")]
        [SerializeField] private float maxRPMFrictionInNewtonMeters = 42f;
        [Tooltip("The curve produced by the above fields \nwhere x is RPM and y is friction.")]
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
