using System;
using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Audio
{
    public class AudioController : MonoBehaviour
    {
        private enum EnginLevel
        {
            Idle,
            Low,
            Medium,
            High
        }
        private const float ENGINE_MEDIUM_PERCENT = 0.25f;
        private const float ENGINE_HIGH_PERCENT = 0.75f;
        private const float TRANSITION_DELAY = 1.0f;
        [SerializeField] private AudioSource engineAudioSource;
        [SerializeField] private AudioSource wheelsAudioSource;
        [SerializeField] private AudioClip tireScreech;
        [SerializeField] private AudioClip engineIdle;
        [SerializeField] private AudioClip engineLow;
        [SerializeField] private AudioClip engineMedium;
        [SerializeField] private AudioClip engineHigh;
        private VehicleController vehicleController;
        private Engine engine;
        private Wheel[] wheels;
        private float transitionTimer = 0f;
        private EnginLevel currentEngineLevel = EnginLevel.Idle;

        private void Awake()
        {
            vehicleController = transform.GetComponentInParent<VehicleController>();
            engine = transform.parent.GetComponentInChildren<Engine>();
            wheels = transform.parent.GetComponentsInChildren<Wheel>();
            engineAudioSource.clip = engineIdle;
            engineAudioSource.loop = true;
            engineAudioSource.Play();
            wheelsAudioSource.clip = tireScreech;
            wheelsAudioSource.loop = true;
        }

        private void FixedUpdate()
        {
            UpdateTireScreeches();
            UpdateEngine();
        }

        private void UpdateTireScreeches()
        {
            foreach (Wheel wheel in wheels)
            {
                if (wheel.IsGrounded() && wheel.GetAverageForwardSlip() > wheel.GetSlipThreshold(Wheel.FX_SLIP_THRESHHOLD_MULTIPLIER))
                {
                    if(!wheelsAudioSource.isPlaying)
                    {
                        wheelsAudioSource.Play();
                        return;
                    }
                }
            }
            if(wheelsAudioSource.isPlaying)
            {
                wheelsAudioSource.Pause();
            }
        }

        private void UpdateEngine()
        {
            transitionTimer += Time.fixedDeltaTime;
            float currentPercent = Mathf.InverseLerp(engine.RPMIdle, engine.RPMMax, engine.RPM);
            EnginLevel newEngineLevel;
            if (engine.RPM <= engine.RPMIdle)
            {
                newEngineLevel = EnginLevel.Idle;             
            }
            else if(currentPercent > ENGINE_HIGH_PERCENT)
            {
                newEngineLevel = EnginLevel.High;             
            }
            else if(currentPercent > ENGINE_MEDIUM_PERCENT)
            {
                newEngineLevel = EnginLevel.Medium;             
            }
            else
            {
                newEngineLevel = EnginLevel.Low;             
            }

            if(newEngineLevel == currentEngineLevel) return;
            if(transitionTimer < TRANSITION_DELAY) return;

            transitionTimer = 0f;
            
            currentEngineLevel = newEngineLevel;
            engineAudioSource.Stop();
            switch (currentEngineLevel)
            {
                case EnginLevel.Low:
                    engineAudioSource.clip = engineLow;
                    break;
                case EnginLevel.Medium:
                    engineAudioSource.clip = engineMedium;
                    break;
                case EnginLevel.High:
                    engineAudioSource.clip = engineHigh;
                    break;
                case EnginLevel.Idle:
                default:
                    engineAudioSource.clip = engineIdle; 
                    break;
            }
            engineAudioSource.Play();
        }
    }    
}
