using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScreenBlink : MonoBehaviour
{
    [SerializeField] private Image fadePanel;
    [SerializeField] private TMP_Text dayText;

    [SerializeField] private float waitBeforeBlink = 5f;
    [SerializeField] private float fadeInTime = 0.35f;
    [SerializeField] private float holdBlackTime = 0.7f;
    [SerializeField] private float textHoldTime = 1.2f;
    [SerializeField] private float fadeOutTime = 0.5f;

    private Coroutine routine;

    private void Awake()
    {
        SetAlpha(fadePanel, 0f);
        SetAlpha(dayText, 0f);
    }

    public void PlayNextDayBlink()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        yield return new WaitForSeconds(waitBeforeBlink);

        yield return FadeImage(fadePanel, 0f, 1f, fadeInTime);

        yield return new WaitForSeconds(0.1f);
        SetAlpha(dayText, 1f);

        yield return new WaitForSeconds(textHoldTime);

        SetAlpha(dayText, 0f);

        yield return new WaitForSeconds(holdBlackTime);

        yield return FadeImage(fadePanel, 1f, 0f, fadeOutTime);

        routine = null;
    }

    private IEnumerator FadeImage(Image img, float from, float to, float duration)
    {
        if (img == null) yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            SetAlpha(img, a);
            yield return null;
        }
        SetAlpha(img, to);
    }

    private void SetAlpha(Image img, float a)
    {
        if (img == null) return;
        var c = img.color;
        c.a = a;
        img.color = c;
    }

    private void SetAlpha(TMP_Text txt, float a)
    {
        if (txt == null) return;
        var c = txt.color;
        c.a = a;
        txt.color = c;
    }
}