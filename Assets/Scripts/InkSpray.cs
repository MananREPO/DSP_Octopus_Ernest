using System;
using UnityEngine;

public class InkSpray : MonoBehaviour
{
    public ParticleSystem inkVfxObject;
    public ParticleSystem inkVfxObject2;

    public KeyCode inkKey = KeyCode.E;
    public event Action<Vector3> OnInkSprayed;

    private void Update()
    {
        if (Input.GetKeyDown(inkKey))
        {
            PlayInk();
        }
    }

    private void PlayInk()
    {
        bool isSwimming = Input.GetKey(KeyCode.W);

        ParticleSystem chosenVFX = isSwimming ? inkVfxObject : inkVfxObject2;

        chosenVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        chosenVFX.Play();
        FindFirstObjectByType<SharkAI>()?.NotifyInk(transform.root.position);

    }
}