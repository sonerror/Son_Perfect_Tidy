using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using sonnv;
public class VFXStart : PoolMember
{
    [SerializeField] private ParticleSystem particle;
    [SerializeField] private AudioData onDragAudio;

    protected override void Awake()
    {
        base.Awake();
        particle = GetComponent<ParticleSystem>();
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        SoundManager.PlaySFX(onDragAudio.clip, onDragAudio.volume);
        if (particle != null)
        {
            particle.gameObject.SetActive(true);
            particle.Play(true);
        }
    }
}
