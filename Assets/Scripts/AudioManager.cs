using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    [SerializeField] AudioClip zoneMusic;
    [Header("Audio Sources")]
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioSource chaseSource;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 1.5f;
    private float baseAmbientVolume;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        baseAmbientVolume = ambientSource.volume;
        chaseSource.volume = 0;
        chaseSource.Play();
    }
    private void Start()
    {
        AudioManager.Instance?.ChangeAmbientMusic(zoneMusic);
    }
    public void ChangeAmbientMusic(AudioClip newClip)
    {
        if (ambientSource.clip == newClip) return;
        StartCoroutine(FadeTrack(ambientSource, newClip, baseAmbientVolume));
    }


    public void SetChaseState(bool isHunting)
    {
        float targetChaseVol = isHunting ? 1f : 0f;
        float targetAmbientVol = isHunting ? 0.2f : baseAmbientVolume; 

        StopAllCoroutines();
        StartCoroutine(FadeVolume(chaseSource, targetChaseVol));
        StartCoroutine(FadeVolume(ambientSource, targetAmbientVol));
    }

    private IEnumerator FadeTrack(AudioSource source, AudioClip newClip, float targetVolume)
    {
        yield return StartCoroutine(FadeVolume(source, 0));
        source.clip = newClip;
        source.Play();
        yield return StartCoroutine(FadeVolume(source, targetVolume));
    }

    private IEnumerator FadeVolume(AudioSource source, float targetVolume)
    {
        float startVol = source.volume;
        float timer = 0;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(startVol, targetVolume, timer / fadeDuration);
            yield return null;
        }
        source.volume = targetVolume;
    }
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        ambientSource.PlayOneShot(clip, volume);
    }
}