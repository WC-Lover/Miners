using System;
using UnityEngine;

public class ParticleSystemController : MonoBehaviour
{
    [SerializeField] ParticleSystem particles;

    private void Start()
    {
        GetComponentInParent<Resource>().OnResourceInteraction += Resource_OnResourceInteraction;
    }

    private void Resource_OnResourceInteraction()
    {
        if (!particles.isPlaying) particles.Play();
    }
}
