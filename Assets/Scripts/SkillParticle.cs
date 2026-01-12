using UnityEngine;

public class SkillParticle : MonoBehaviour
{

    public ParticleSystem particle;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void StartParticle()
    {
        particle.Play();

    }

    void StopParticle()
    {
        particle.Stop();
    }
}
