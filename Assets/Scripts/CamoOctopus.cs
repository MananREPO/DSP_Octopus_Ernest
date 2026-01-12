using System;
using System.Collections;
using UnityEngine;

public class CamoOctopus : MonoBehaviour
{
    public Renderer targetRenderer;

    public Material[] camoMaterials;

    public KeyCode camoKey = KeyCode.F;
    public float camoDuration = 5f;
    public float fadeTime = 0.3f;

    private bool isCamouflaged = false;
    private bool isBusy = false;

    private Material[] originalMaterials;
    private Color[] camoBaseColors;

    public Animator animator;

    public bool IsCamouflaged => isCamouflaged;
    public event Action<bool> OnCamoChanged;

    [Header("Audio")]
    [SerializeField] private AudioClip camoSFX;

    void Start()
    {
        targetRenderer = GetComponentInChildren<Renderer>();

        originalMaterials = targetRenderer.materials;

        camoBaseColors = new Color[camoMaterials.Length];

        for (int i = 0; i < camoMaterials.Length; i++)
        {
            camoBaseColors[i] = camoMaterials[i].color;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(camoKey) && !isBusy && targetRenderer != null)
        {
            StartCoroutine(CamouflageRoutine());
        }
    }

    IEnumerator CamouflageRoutine()
    {
        isBusy = true;
        isCamouflaged = true;
        OnCamoChanged?.Invoke(true);
        if (camoSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(camoSFX, 0.7f);
        }
        //animator.SetTrigger("isCamo");
        targetRenderer.materials = camoMaterials;


        float startAlpha = 1f;
        float endAlpha = 0.2f;
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startAlpha, endAlpha, t / fadeTime);

            for (int i = 0; i < camoMaterials.Length; i++)
            {
                Color baseColor = camoBaseColors[i];
                baseColor.a = a;
                camoMaterials[i].color = baseColor;
            }

            yield return null;
        }

        yield return new WaitForSeconds(camoDuration);

        t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(endAlpha, startAlpha, t / fadeTime);

            for (int i = 0; i < camoMaterials.Length; i++)
            {
                Color baseColor = camoBaseColors[i];
                baseColor.a = a;
                camoMaterials[i].color = baseColor;
            }

            yield return null;
        }

        for (int i = 0; i < camoMaterials.Length; i++)
        {
            Color baseColor = camoBaseColors[i];
            baseColor.a = 1f;
            camoMaterials[i].color = baseColor;
        }

        targetRenderer.materials = originalMaterials;

        isCamouflaged = false;
        OnCamoChanged?.Invoke(false);
        isBusy = false;
    }
}