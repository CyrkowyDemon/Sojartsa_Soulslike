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
        DontDestroyOnLoad(gameObject); // Mordo, robimy go nieśmiertelnym, żeby podróżował z Main Menu do gry!
        
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

    /// <summary>
    /// Natychmiastowe ustawienie przezroczystości (bez animacji).
    /// </summary>
    public void SetAlpha(float alpha)
    {
        StopAllCoroutines();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            // Magia: wyłączamy interakcje jeśli jest przezroczysty, włączamy jeśli jest czarny/ciemnieje
            canvasGroup.blocksRaycasts = (alpha > 0f);
            canvasGroup.interactable = (alpha > 0f);
        }
        
        // Audio: 0 głośności jeśli ekran czarny (alpha 1), 1 głośności jeśli czysty (alpha 0)
        float targetVolume = (alpha > 0.5f) ? 0f : 1f;
        _masterBus.setVolume(targetVolume);
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration, Action onComplete)
    {
        if (canvasGroup == null)
        {
            Debug.LogError("<color=red>[FADER] Brak referencji do CanvasGroup!</color>");
            onComplete?.Invoke();
            yield break;
        }

        // Przy jakimkolwiek fadowaniu domyślnie blokujemy interakcje (gracz nie może przeklikać gry podczas animacji)
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

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
        
        // Jeśli skończyliśmy rozjaśniać (czyli obraz jest w pełni widoczny, a kurtyna ma przezroczystość 0), odblokowujemy kliknięcia!
        if (targetAlpha == 0f)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        
        onComplete?.Invoke();
    }
}
