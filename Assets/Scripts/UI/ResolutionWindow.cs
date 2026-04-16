using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

    private void Awake()
    {
        // Okno startuje schowane
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Wywoływane przez przycisk rozdzielczości w GraphicUI.
    /// </summary>
    public void Open()
    {
        BuildResolutionList();
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Zamknij okno. Wywoływane przez przyciski na liście lub przycisk "X".
    /// </summary>
    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void BuildResolutionList()
    {
        // Wyczyść stare przyciski (w razie ponownego otwarcia okna)
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

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

        // Spawnujemy przycisk dla każdej unikalnej rozdzielczości
        for (int i = 0; i < _uniqueResolutions.Count; i++)
        {
            Vector2Int res = _uniqueResolutions[i];
            int capturedIndex = i; // KLUCZOWE: przekazujemy kopię, żeby closure działało

            Button btn = Instantiate(resolutionButtonPrefab, contentParent);

            // Ustawiamy tekst przycisku
            TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = $"{res.x} x {res.y}";

            // Podpinamy akcję: klik = wybór tej rozdzielczości
            btn.onClick.AddListener(() => SelectResolution(capturedIndex));

            // Podświetlamy aktualną rozdzielczość
            int curW = Screen.currentResolution.width;
            int curH = Screen.currentResolution.height;
            if (res.x == curW && res.y == curH)
            {
                // Możesz tu też wizualnie zaznaczyć aktywny przycisk (np. zmiana koloru)
                var colors = btn.colors;
                colors.normalColor = new Color(0.9f, 0.75f, 0.3f, 1f); // Złoty highlight
                btn.colors = colors;
            }
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
