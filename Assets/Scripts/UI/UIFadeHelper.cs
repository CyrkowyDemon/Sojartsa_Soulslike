using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class UIFadeHelper : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 0.25f;
    private CanvasGroup _canvasGroup;
    private Coroutine _currentFadeRoutine;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void EnsureCanvasGroup()
    {
        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void FadeIn()
    {
        Debug.Log($"[UIFadeHelper] Start FadeIn na {gameObject.name}");
        
        // Jeśli obiekt był wyłączony, upewniamy się, że zaczynamy od zera
        EnsureCanvasGroup();
        if (!gameObject.activeInHierarchy || _canvasGroup.alpha > 0.9f)
        {
            _canvasGroup.alpha = 0f;
        }

        gameObject.SetActive(true);
        
        if (_currentFadeRoutine != null) StopCoroutine(_currentFadeRoutine);
        _currentFadeRoutine = StartCoroutine(FadeRoutine(1f));
    }

    public void FadeOut()
    {
        Debug.Log($"[UIFadeHelper] Start FadeOut na {gameObject.name}");
        if (!gameObject.activeInHierarchy)
        {
            EnsureCanvasGroup();
            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            return;
        }

        if (_currentFadeRoutine != null) StopCoroutine(_currentFadeRoutine);
        _currentFadeRoutine = StartCoroutine(FadeRoutine(0f, true));
    }

    private IEnumerator FadeRoutine(float targetAlpha, bool deactivateAfter = false)
    {
        float startAlpha = _canvasGroup.alpha;
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
        
        if (deactivateAfter && targetAlpha == 0)
        {
            gameObject.SetActive(false);
        }
        Debug.Log($"[UIFadeHelper] Koniec animacji na {gameObject.name}. Alpha = {targetAlpha}");
    }
}
