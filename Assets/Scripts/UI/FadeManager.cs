using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using FMODUnity;

/// <summary>
/// Mordo, to jest nasza "Kurtyna". Odpowiada za płynne przyciemnianie 
/// i rozjaśnianie ekranu (Fade Out/Fade In). 
/// Teraz zintegrowana z FMOD, żeby dźwięk płynnie cichł przy zmianie sceny.
/// </summary>
public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float defaultFadeDuration = 1.0f;
    
    // Szyna główna FMOD (wszystkie dźwięki pod nią podlegają)
    private FMOD.Studio.Bus _masterBus;

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

        // Pobieramy Master Bus. Domyślnie w FMOD to "bus:/"
        _masterBus = RuntimeManager.GetBus("bus:/");
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
            Debug.LogError("<color=red>[FADER] Brak referencji do CanvasGroup!</color>");
            onComplete?.Invoke();
            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        // Sprawdzamy aktualną głośność szyny
        _masterBus.getVolume(out float startVolume);
        
        // Celujemy w 0 (cisza) przy ściemnianiu lub 1 (pełna moc) przy rozjaśnianiu
        float targetVolume = (targetAlpha > 0.5f) ? 0f : 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // 1. Obraz (płynnie od startu do końca czasu)
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);

            // 2. Audio (Zgodnie z życzeniem: zcisza się szybciej - wyzeruje się przy 80% progresu)
            float audioProgress = Mathf.Clamp01(progress / 0.8f); 
            float currentVolume = Mathf.Lerp(startVolume, targetVolume, audioProgress);
            _masterBus.setVolume(currentVolume);

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        _masterBus.setVolume(targetVolume);
        
        onComplete?.Invoke();
    }
}
