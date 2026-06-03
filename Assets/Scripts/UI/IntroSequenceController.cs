using UnityEngine;
using TMPro;
using System.Collections;
using System.Linq;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Sojartsa.UI;

/// <summary>
/// Mordo, to jest kontroler naszego Intro (placeholdera).
/// Wyświetla po kolei linijki tekstu, pozwala graczowi na pominięcie i na końcu
/// ładuje właściwy poziom gry korzystając z LoadingScreenManager.
/// </summary>
public class IntroSequenceController : MonoBehaviour
{
    [Header("UI Elementy")]
    [SerializeField] private TextMeshProUGUI textDisplay;
    [SerializeField] private CanvasGroup textCanvasGroup;
    [SerializeField] private GameObject skipPromptObject; // Np. mały napis "Naciśnij dowolny przycisk, by pominąć"

    [Header("Ustawienia Animacji (DOTween)")]
    [SerializeField] private float textFadeDuration = 1.5f;
    [SerializeField] private float textDisplayDuration = 3.0f;
    [SerializeField] private float delayBetweenTexts = 0.5f;

    [Header("Teksty Intro (Placeholder)")]
    [TextArea(3, 10)]
    [SerializeField] private string[] introTexts = new string[]
    {
        "Gdy pierwotne światło zaczęło przygasać, ziemie Sojartsa spowił gęsty, nieprzenikniony mrok...",
        "Starożytne bestie, niegdyś uśpione w głębinach, przebudziły się, wiedzione zapachem słabnącej magii.",
        "Wielu śmiałków wyruszyło w podróż, by odnaleźć źródło skazy, lecz żaden z nich nie powrócił.",
        "Teraz ty, bezimienny wędrowcze, stajesz u progu zapomnianych krain...",
        "Twoja podróż zaczyna się właśnie tutaj."
    };

    [Header("Domyślny Poziom Startowy")]
    [Tooltip("Używany tylko jako awaryjny ratunek, jeśli PlayerPrefs nie przekaże poprawnej sceny docelowej")]
    [SerializeField] private string fallbackStartScene = "HUB";

    private string _targetSceneName;
    private bool _isSkipping = false;
    private Coroutine _introCoroutine;

    private void Start()
    {
        // 1. Pobieramy zapisaną w PlayerPrefs docelową scenę gry
        _targetSceneName = PlayerPrefs.GetString("IntroTargetScene", fallbackStartScene);
        Debug.Log($"[Intro] Przygotowano docelową scenę: {_targetSceneName}");

        // Wyłączamy interakcje i zerujemy alphy na starcie
        if (textCanvasGroup != null) textCanvasGroup.alpha = 0f;
        if (skipPromptObject != null) skipPromptObject.SetActive(false);

        // Upewniamy się, że ekran jest czarny na starcie, a potem płynnie się rozjaśnia
        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.SetAlpha(1f);
            FadeManager.Instance.FadeIn(1.5f, () => 
            {
                // Po rozjaśnieniu ekranu startujemy napisy
                _introCoroutine = StartCoroutine(PlayIntroRoutine());
            });
        }
        else
        {
            _introCoroutine = StartCoroutine(PlayIntroRoutine());
        }
    }

    private void Update()
    {
        // Wykrywamy dowolny klawisz lub przycisk kontrolera do pominięcia Intro (nowy Input System)
        if (!_isSkipping)
        {
            bool skipPressed = false;

            #if ENABLE_INPUT_SYSTEM || true
            // Klawiatura
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.anyKey.wasPressedThisFrame)
            {
                skipPressed = true;
            }
            // Myszka
            else if (UnityEngine.InputSystem.Mouse.current != null && 
                (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame || 
                 UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame))
            {
                skipPressed = true;
            }
            // Gamepad - sprawdzamy konkretne przyciski (A/Cross, B/Circle, itd.)
            else if (UnityEngine.InputSystem.Gamepad.current != null)
            {
                var pad = UnityEngine.InputSystem.Gamepad.current;
                if (pad.buttonSouth.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame ||
                    pad.buttonEast.wasPressedThisFrame  || pad.buttonWest.wasPressedThisFrame  ||
                    pad.startButton.wasPressedThisFrame)
                {
                    skipPressed = true;
                }
            }
            #endif

            if (skipPressed)
            {
                SkipIntro();
            }
        }
    }

    private IEnumerator PlayIntroRoutine()
    {
        if (textDisplay == null || textCanvasGroup == null)
        {
            Debug.LogError("[Intro] Brak przypisanych referencji w Inspektorze!");
            EndIntro();
            yield break;
        }

        // Pokaż na chwilę podpowiedź o pominięciu na dole ekranu, po czym ją wygaszamy
        if (skipPromptObject != null)
        {
            skipPromptObject.SetActive(true);
            CanvasGroup skipCG = skipPromptObject.GetComponent<CanvasGroup>();
            if (skipCG != null)
            {
                skipCG.alpha = 0f;
                skipCG.DOFade(0.5f, 2f).SetLoops(-1, LoopType.Yoyo);
            }
        }

        for (int i = 0; i < introTexts.Length; i++)
        {
            // 1. Ustawienie tekstu
            textDisplay.text = introTexts[i];

            // 2. Fade In tekstu
            textCanvasGroup.alpha = 0f;
            Tween fadeInTween = textCanvasGroup.DOFade(1f, textFadeDuration);
            yield return fadeInTween.WaitForCompletion();

            // 3. Wyświetlanie tekstu przez określony czas
            yield return new WaitForSeconds(textDisplayDuration);

            // 4. Fade Out tekstu
            Tween fadeOutTween = textCanvasGroup.DOFade(0f, textFadeDuration);
            yield return fadeOutTween.WaitForCompletion();

            // 5. Krótka przerwa między blokami tekstu
            yield return new WaitForSeconds(delayBetweenTexts);
        }

        EndIntro();
    }

    private void SkipIntro()
    {
        _isSkipping = true;
        Debug.Log("[Intro] Gracz zdecydował o pominięciu Intro.");

        if (_introCoroutine != null)
        {
            StopCoroutine(_introCoroutine);
        }

        // Zabijamy trwające tweens na tekście
        if (textCanvasGroup != null)
        {
            textCanvasGroup.DOKill();
            textCanvasGroup.alpha = 0f;
        }

        if (skipPromptObject != null)
        {
            skipPromptObject.SetActive(false);
        }

        // Płynny fade out i przejście
        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.FadeOut(1.0f, () =>
            {
                EndIntro();
            });
        }
        else
        {
            EndIntro();
        }
    }

    private void EndIntro()
    {
        Debug.Log($"[Intro] Ładowanie docelowej sceny: {_targetSceneName}");
        
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.LoadScene(_targetSceneName);
        }
        else
        {
            SceneManager.LoadScene(_targetSceneName);
        }
    }
}
