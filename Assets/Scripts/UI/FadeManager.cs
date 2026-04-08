using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

/// <summary>
/// Mordo, to jest nasza "Kurtyna". Odpowiada za płynne przyciemnianie 
/// i rozjaśnianie ekranu (Fade Out/Fade In). 
/// Wszystko po to, żeby gracz nie widział, jak w tle Unity buduje świat.
/// </summary>
public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float defaultFadeDuration = 1.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    public void FadeOut(float duration, Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(1f, duration, onComplete));
    }

    public void FadeIn(float duration, Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(0f, duration, onComplete));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration, Action onComplete)
    {
        if (canvasGroup == null)
        {
            Debug.LogError("<color=red>[FADER] Brak referencji do CanvasGroup! Teleportacja będzie 'ostra'.</color>");
            onComplete?.Invoke();
            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }
}
