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
        private const float IDLE_ENGINE_VOLUME = 0.1f;
        private const float ENGINE_VOLUME_MIN = 0.1f;
        private const float ENGINE_VOLUME_MAX = 0.6f;

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
                float slip = wheel.GetAverageForwardSlip();
                float slipThreshhold = wheel.GetSlipThreshold(Wheel.FX_SLIP_THRESHHOLD_MULTIPLIER);
                if (wheel.IsGrounded && slip > slipThreshhold)
                {
                    float slipPercent = (slip + slipThreshhold) / (slipThreshhold * 20f);
                    wheelsAudioSource.volume = Mathf.Lerp(0f, 1f, slipPercent);
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
                engineAudioSource.volume = IDLE_ENGINE_VOLUME;         
            }
            else if(currentPercent > ENGINE_HIGH_PERCENT)
            {
                newEngineLevel = EnginLevel.High; 
                float highPercent = (currentPercent - ENGINE_HIGH_PERCENT) / (1.0f - ENGINE_HIGH_PERCENT);
                engineAudioSource.volume = Mathf.Lerp(ENGINE_VOLUME_MIN, ENGINE_VOLUME_MAX, highPercent);         
            }
            else if(currentPercent > ENGINE_MEDIUM_PERCENT)
            {
                newEngineLevel = EnginLevel.Medium;             
                float mediumPercent = (currentPercent - ENGINE_MEDIUM_PERCENT) / (ENGINE_HIGH_PERCENT - ENGINE_MEDIUM_PERCENT);
                engineAudioSource.volume = Mathf.Lerp(ENGINE_VOLUME_MIN, ENGINE_VOLUME_MAX, mediumPercent);         
            }
            else
            {
                newEngineLevel = EnginLevel.Low;             
                float lowPercent = currentPercent / ENGINE_MEDIUM_PERCENT;
                engineAudioSource.volume = Mathf.Lerp(ENGINE_VOLUME_MIN, ENGINE_VOLUME_MAX, lowPercent);         
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
