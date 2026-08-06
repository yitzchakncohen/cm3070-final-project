using UnityEngine;

namespace ModularVehicleSimulator.Vehicle.Audio
{
    public class AudioController : MonoBehaviour
    {
        private const float ENGINE_MEDIUM_PERCENT = 0.25f;
        private const float ENGINE_HIGH_PERCENT = 0.75f;
        private const float TRANSITION_DELAY = 0.35f;
        [SerializeField] private AudioSource engineAudioSource;
        [SerializeField] private AudioSource wheelsAudioSource;
        [SerializeField] private AudioClip tireScreech;
        [SerializeField] private AudioClip engineIdle;
        [SerializeField] private AudioClip engineLow;
        [SerializeField] private AudioClip engineMedium;
        [SerializeField] private AudioClip engineHigh;
        private Engine engine;
        private Wheel[] wheels;
        private float transitionTimer = 0f;

        private void Start()
        {
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
                if (wheel.IsGrounded() && wheel.GetAverageForwardSlip() > Wheel.SPEEDOMETER_SLIP_THRESHHOLD)
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
            if(transitionTimer < TRANSITION_DELAY) return;
            if(engine.RPM <= engine.RPMIdle)
            {
                engineAudioSource.clip = engineIdle; 
                transitionTimer = 0f;
                if(!engineAudioSource.isPlaying)
                {
                    engineAudioSource.Play();
                }
                return;               
            }
            float currentPercent = Mathf.InverseLerp(engine.RPMIdle, engine.RPMMax, engine.RPM);
            if(currentPercent > ENGINE_HIGH_PERCENT)
            {
                engineAudioSource.clip = engineHigh;
                transitionTimer = 0f;
            }
            else if(currentPercent > ENGINE_MEDIUM_PERCENT)
            {
                engineAudioSource.clip = engineMedium;
                transitionTimer = 0f;
            }
            else
            {
                engineAudioSource.clip = engineLow;
                transitionTimer = 0f;
            }
            if(!engineAudioSource.isPlaying)
            {
                engineAudioSource.Play();
            }
        }
    }    
}
