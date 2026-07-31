using UnityEngine;

namespace ModularVehicleSimulator.Vehicle
{
    [RequireComponent(typeof(Wheel))]
    public class SkidFX : MonoBehaviour
    {
        [SerializeField] private GameObject particleFX;
        private ParticleSystem[] particleSystems;
        private Wheel wheel;

        private void Start()
        {
            wheel = GetComponent<Wheel>();
            particleSystems = GetComponentsInChildren<ParticleSystem>();
        }

        private void Update()
        {
            UpdateSkidFX();
        }

        private void UpdateSkidFX()
        {
            if (wheel.IsGrounded() && wheel.GetAverageForwardSlip() > Wheel.SPEEDOMETER_SLIP_THRESHHOLD)
            {
                foreach (ParticleSystem particleSystem in particleSystems)
                {
                    if(!particleSystem.isPlaying)
                    {
                        particleSystem.Play();
                    }
                }
            }
            else
            {
                foreach (ParticleSystem particleSystem in particleSystems)
                {
                    if(particleSystem.isPlaying)
                    {
                        particleSystem.Stop();
                    }
                }
            }
        }
    }    
}
