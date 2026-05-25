using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

/// <summary>
/// Okno wyboru rozdzielczości. Wrzuć ten skrypt na Panel (okienko) w Unity.
/// Panel powinien mieć dziecko ScrollRect z Vertical Layout Group wewnątrz (Content).
/// </summary>
public class ResolutionWindow : MonoBehaviour
{
    [Header("Referencje")]
    [Tooltip("Prefab przycisku do listy - ten sam styl co reszta UI")]
    [SerializeField] private Button resolutionButtonPrefab;
    [Tooltip("Obiekt, do którego będą dodawane przyciski (Vertical Layout Group)")]
    [SerializeField] private Transform contentParent;
    [Tooltip("Kto nas wywołał? On nas też zamknie.")]
    [SerializeField] private GraphicUI graphicUI;

    // Przefiltrowana lista unikalnych rozdzielczości (bez duplikatów 60Hz/144Hz)
    private List<Vector2Int> _uniqueResolutions = new List<Vector2Int>();

    // Dynamiczny blocker w tle (do zamykania kliknięciem poza oknem)
    private GameObject _blockerInstance;

    private void Awake()
    {
        // Okno startuje schowane
        gameObject.SetActive(false);

        // Auto-wyszukiwanie brakujących referencji dla wygody
        RepairScrollViewReferences();
        AutoFindReferences();
    }

    private void OnDisable()
    {
        DestroyBlocker();
    }

    private void OnDestroy()
    {
        DestroyBlocker();
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;

        // STYL FROMSOFTWARE: Zamykanie prawym przyciskiem myszy, Escape lub przyciskiem B na padzie
        bool cancelPressed = false;

        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame ||
                UnityEngine.InputSystem.Keyboard.current.backspaceKey.wasPressedThisFrame)
            {
                cancelPressed = true;
            }
        }

        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            if (UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame)
            {
                cancelPressed = true;
            }
        }

        if (UnityEngine.InputSystem.Gamepad.current != null)
        {
            if (UnityEngine.InputSystem.Gamepad.current.buttonEast.wasPressedThisFrame)
            {
                cancelPressed = true;
            }
        }

        if (cancelPressed)
        {
            Close();
        }
    }

    /// <summary>
    /// Wywoływane przez przycisk rozdzielczości w GraphicUI.
    /// </summary>
    public void Open()
    {
        gameObject.SetActive(true);
        CreateBlocker();
        // Najpierw aktywujemy obiekt, żeby EventSystem mógł poprawnie zaznaczyć przyciski
        BuildResolutionList();
    }

    /// <summary>
    /// Zamknij okno. Wywoływane przez przyciski na liście lub kliknięcie poza listą.
    /// </summary>
    public void Close()
    {
        gameObject.SetActive(false);
        DestroyBlocker();
    }

    private void CreateBlocker()
    {
        if (_blockerInstance != null) return;

        // Tworzymy nowy obiekt tła
        _blockerInstance = new GameObject("ResolutionPanel_Blocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        _blockerInstance.transform.SetParent(transform.parent, false);

        // Ustawiamy kolejność rysowania (blocker musi być tuż pod panelem rozdzielczości)
        _blockerInstance.transform.SetSiblingIndex(transform.GetSiblingIndex());

        // Konfigurujemy RectTransform, żeby pokrył cały ekran (nawet przy małym rodzicu)
        RectTransform blockerRect = _blockerInstance.GetComponent<RectTransform>();
        blockerRect.anchorMin = new Vector2(0.5f, 0.5f);
        blockerRect.anchorMax = new Vector2(0.5f, 0.5f);
        blockerRect.anchoredPosition = Vector2.zero;
        blockerRect.sizeDelta = new Vector2(4000f, 4000f); // ogromny rozmiar pokrywający cały ekran

        // Nadajemy lekko ciemny odcień tła (efekt kinowy, styl Sekiro/Dark Souls)
        Image blockerImg = _blockerInstance.GetComponent<Image>();
        blockerImg.color = new Color(0f, 0f, 0f, 0.6f);

        // Podpinamy zamykanie pod kliknięcie
        Button blockerBtn = _blockerInstance.GetComponent<Button>();
        blockerBtn.transition = Selectable.Transition.None;
        blockerBtn.onClick.AddListener(Close);
    }

    private void DestroyBlocker()
    {
        if (_blockerInstance != null)
        {
            Destroy(_blockerInstance);
            _blockerInstance = null;
        }
    }

    private void AutoFindReferences()
    {
        if (graphicUI == null)
        {
            graphicUI = GetComponentInParent<GraphicUI>();
        }

        if (contentParent == null)
        {
            var scrollRect = GetComponentInChildren<ScrollRect>(true);
            if (scrollRect != null)
            {
                contentParent = scrollRect.content;
            }
        }

        // Zabezpieczenie: jeśli contentParent wciąż nie ma kluczowych komponentów układu
        if (contentParent != null)
        {
            // Zawsze pobieramy lub dodajemy pionową grupę układu i wymuszamy idealne parametry
            if (!contentParent.TryGetComponent<VerticalLayoutGroup>(out var layout))
            {
                layout = contentParent.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 10f;
            layout.padding = new RectOffset(10, 10, 10, 10);

            // Zawsze pobieramy lub dodajemy ContentSizeFitter i wymuszamy idealne parametry
            if (!contentParent.TryGetComponent<ContentSizeFitter>(out var fitter))
            {
                fitter = contentParent.gameObject.AddComponent<ContentSizeFitter>();
            }
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }
    }

    private void RepairScrollViewReferences()
    {
        var scrollRect = GetComponentInChildren<ScrollRect>(true);
        if (scrollRect == null) return;

        Transform viewTransform = scrollRect.transform;
        Transform viewport = viewTransform.Find("Viewport");
        Transform content = viewTransform.Find("Viewport/Content");
        Transform scrollbar = viewTransform.Find("Scrollbar Vertical");

        if (viewport != null && scrollRect.viewport == null)
        {
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
        }

        if (content != null && scrollRect.content == null)
        {
            scrollRect.content = content.GetComponent<RectTransform>();
        }

        if (scrollbar != null && scrollRect.verticalScrollbar == null)
        {
            scrollRect.verticalScrollbar = scrollbar.GetComponent<Scrollbar>();
        }
    }

    private void BuildResolutionList()
    {
        // Zabezpieczenie na wypadek braku referencji na tym etapie
        AutoFindReferences();

        if (contentParent == null)
        {
            Debug.LogError("[ResolutionWindow] Brak contentParent! Nie można zbudować listy.");
            return;
        }

        // Wyczyść stare przyciski
        foreach (Transform child in contentParent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        _uniqueResolutions.Clear();

        // Pobierz wszystkie rozdzielczości z systemu
        Resolution[] allResolutions = Screen.resolutions;

        // Filtrujemy duplikaty (tylko unikalne pary Width x Height)
        HashSet<Vector2Int> seen = new HashSet<Vector2Int>();
        // Iterujemy od końca, żeby zacząć od NAJWIĘKSZYCH
        for (int i = allResolutions.Length - 1; i >= 0; i--)
        {
            var res = allResolutions[i];
            Vector2Int key = new Vector2Int(res.width, res.height);
            if (seen.Contains(key)) continue;

            seen.Add(key);
            _uniqueResolutions.Add(key);
        }

        // Pobieramy aktualnie zapisaną rozdzielczość
        int currentSavedWidth = 0;
        int currentSavedHeight = 0;
        if (SettingsManager.Instance != null && allResolutions.Length > 0)
        {
            int index = Mathf.Clamp(SettingsManager.Instance.resolutionIndex, 0, allResolutions.Length - 1);
            currentSavedWidth = allResolutions[index].width;
            currentSavedHeight = allResolutions[index].height;
        }

        if (currentSavedWidth == 0 || currentSavedHeight == 0)
        {
            currentSavedWidth = Screen.width;
            currentSavedHeight = Screen.height;
        }

        Button buttonToSelect = null;

        // Spawnujemy przycisk dla każdej unikalnej rozdzielczości
        for (int i = 0; i < _uniqueResolutions.Count; i++)
        {
            Vector2Int res = _uniqueResolutions[i];
            int capturedIndex = i; // closure

            if (resolutionButtonPrefab == null)
            {
                Debug.LogError("[ResolutionWindow] Brak prefabrykatu przycisku!");
                return;
            }

            Button btn = Instantiate(resolutionButtonPrefab, contentParent);
            
            // Czyścimy skrypt dialogowy, jeśli istnieje w prefabrykacie
            if (btn.TryGetComponent<DialogueChoiceButton>(out var dialogueBtn))
            {
                Destroy(dialogueBtn);
            }

            // Ustawiamy tekst przycisku
            TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = $"{res.x} x {res.y}";

            // Podpinamy akcję: klik = wybór tej rozdzielczości
            btn.onClick.AddListener(() => SelectResolution(capturedIndex));

            // Podświetlamy aktualną rozdzielczość
            if (res.x == currentSavedWidth && res.y == currentSavedHeight)
            {
                var colors = btn.colors;
                colors.normalColor = new Color(0.9f, 0.75f, 0.3f, 1f); // Złoty highlight
                btn.colors = colors;
                buttonToSelect = btn;
            }
        }

        // Dodajemy przycisk "Powrót" na dole listy
        Button backBtn = Instantiate(resolutionButtonPrefab, contentParent);
        if (backBtn.TryGetComponent<DialogueChoiceButton>(out var backDialogueBtn))
        {
            Destroy(backDialogueBtn);
        }
        TextMeshProUGUI backLabel = backBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (backLabel != null) backLabel.text = "Powrót";
        backBtn.onClick.AddListener(Close);

        // Selektujemy odpowiedni przycisk do nawigacji padem/klawiaturą
        if (buttonToSelect != null)
        {
            SelectButton(buttonToSelect.gameObject);
        }
        else
        {
            SelectButton(backBtn.gameObject);
        }
    }

    private void SelectButton(GameObject button)
    {
        if (button == null) return;
        StartCoroutine(SelectRoutine(button));
    }

    private System.Collections.IEnumerator SelectRoutine(GameObject button)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        
        yield return new WaitForEndOfFrame();

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(button);
        }
    }

    private void SelectResolution(int index)
    {
        if (index < 0 || index >= _uniqueResolutions.Count) return;

        Vector2Int chosen = _uniqueResolutions[index];

        // Znajdź indeks w oryginalnej tablicy Screen.resolutions (najwyższe Hz dla tej rozdzielczości)
        Resolution[] all = Screen.resolutions;
        int bestIndex = 0;
        float bestHz = 0f;

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].width == chosen.x && all[i].height == chosen.y)
            {
                float hz = (float)all[i].refreshRateRatio.value;
                if (hz > bestHz)
                {
                    bestHz = hz;
                    bestIndex = i;
                }
            }
        }

        Debug.Log($"<color=cyan>[RESOLUTION] Wybrano: {chosen.x}x{chosen.y} (index={bestIndex})</color>");

        SettingsManager.Instance.SaveGraphicsSettings(
            SettingsManager.Instance.qualityIndex,
            SettingsManager.Instance.screenModeIndex,
            bestIndex,
            SettingsManager.Instance.showBlood
        );

        // Informujemy GraphicUI żeby zaktualizowało tekst przycisku
        if (graphicUI != null) graphicUI.UpdateUIElements();

        Close();
    }
}

