using System;
using System.Collections;
using UnityEngine;

public class InkSpray : MonoBehaviour
{
    public ParticleSystem swimInkPrefab;
    public ParticleSystem idleInkPrefab;

    public Transform spawnPoint;
    public float spawnForwardOffset = 1.0f;
    public float launchSpeed = 10f;
    public float pushDuration = 0.35f;

    public KeyCode inkKey = KeyCode.E;

    public event Action<Vector3> OnInkSprayed;

    private SharkAI shark;

    private void Awake()
    {
        shark = FindFirstObjectByType<SharkAI>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(inkKey))
            ShootInkBurst();
    }

    private void ShootInkBurst()
    {
        bool isSwimming = Input.GetKey(KeyCode.W);
        ParticleSystem prefab = isSwimming ? swimInkPrefab : idleInkPrefab;

        if (prefab == null)
        {
            Debug.LogWarning("InkSpray: Missing ink prefab reference.");
            return;
        }

        Transform root = transform.root;
        Vector3 dir = -root.forward;

        Vector3 pos;
        Quaternion rot;

        if (spawnPoint != null)
        {
            pos = spawnPoint.position;
            rot = spawnPoint.rotation;
            dir = -spawnPoint.forward;
        }
        else
        {
            pos = root.position + dir * spawnForwardOffset;
            rot = Quaternion.LookRotation(dir, Vector3.up);
        }

        ParticleSystem ink = Instantiate(prefab, pos, rot);

        ink.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ink.Play();

        Rigidbody rb = ink.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = dir * launchSpeed;
            StartCoroutine(StopPushAfter(rb, pushDuration));
        }
        else
        {
            StartCoroutine(PushTransformForward(ink.transform, dir, launchSpeed, pushDuration));
        }

        OnInkSprayed?.Invoke(pos);
        shark?.NotifyInk(ink.transform);

        Destroy(ink.gameObject, GetParticleLifetime(ink) + 0.5f);
    }

    private IEnumerator StopPushAfter(Rigidbody rb, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }

    private IEnumerator PushTransformForward(Transform t, Vector3 dir, float speed, float duration)
    {
        float timer = 0f;
        while (timer < duration && t != null)
        {
            t.position += dir * speed * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null;
        }
    }

    private float GetParticleLifetime(ParticleSystem ps)
    {
        var main = ps.main;

        float dur = main.duration;

        float life = main.startLifetime.constantMax;

        return dur + life;
    }
}