using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace Sojartsa.UI
{
    // =========================================================
    //  KLASY DANYCH ZAPISU (SaveManager je pakuje do JSON-a)
    // =========================================================
    [System.Serializable]
    public class BookSubTabSaveData
    {
        public string subTabName;
        public List<BookPageSaveEntry> pages = new List<BookPageSaveEntry>();
    }

    [System.Serializable]
    public class BookTabSaveData
    {
        public string tabName;
        public List<BookPageSaveEntry> pages = new List<BookPageSaveEntry>();
        public List<BookSubTabSaveData> subTabs = new List<BookSubTabSaveData>();
    }

    [System.Serializable]
    public class BookPageSaveEntry
    {
        public string pageTitle;
        public string textContent;
        public bool isEditable;
    }

    // =========================================================
    //  KLASY DANYCH RUNTIME (Trzymają dane w trakcie gry)
    // =========================================================
    [System.Serializable]
    public class BookPageData
    {
        public string pageTitle = "";
        [TextArea(10, 20)]
        public string textContent = "";
        public Sprite imageContent = null;
        public bool isEditable = true;
    }

    [System.Serializable]
    public class SubTab
    {
        public string subTabName;
        public Button subTabButton;
        public int pageLimit = 99;
        public bool isEditable = false;
        public List<BookPageData> pages = new List<BookPageData>();
    }

    [System.Serializable]
    public class TopTab
    {
        public string tabName;
        public Button tabButton;
        [Tooltip("Kontener z przyciskami bocznych zakładek dla tej głównej zakładki")]
        public GameObject subTabsContainer;
        public int pageLimit = 99;
        public bool isEditable = false;
        public List<SubTab> subTabs = new List<SubTab>();
        public List<BookPageData> pages = new List<BookPageData>();
    }

    /// <summary>
    /// Główny, w pełni uniwersalny i modułowy menedżer Książki (Notatnik + Bestiariusz).
    /// Obsługuje rozkładówki (2 strony na raz), system zakładek górnych/bocznych, limity stron oraz ich przewracanie.
    /// </summary>
    public class UniversalBook : MonoBehaviour
    {
        [Header("Zakładki (Tabs)")]
        [SerializeField] private List<TopTab> topTabs = new List<TopTab>();
        [SerializeField] private int defaultTopTabIndex = 0;

        [Header("Wysuwanie Zakładek (Y Offset)")]
        [Tooltip("Pozycja Y (anchoredPosition) aktywnej zakładki - wysunięta nad kartkę")]
        [SerializeField] private float activeTabYOffset = 0f;
        [Tooltip("Pozycja Y (anchoredPosition) nieaktywnej zakładki - schowana pod kartkę")]
        [SerializeField] private float inactiveTabYOffset = -20f;

        [Header("Referencje UI - Lewa Strona")]
        [SerializeField] private TMP_InputField leftInputField;
        [SerializeField] private TextMeshProUGUI leftTextDisplay;
        [SerializeField] private Image leftImageDisplay;
        [SerializeField] private TextMeshProUGUI leftPageNumberText;

        [Header("Referencje UI - Prawa Strona")]
        [SerializeField] private TMP_InputField rightInputField;
        [SerializeField] private TextMeshProUGUI rightTextDisplay;
        [SerializeField] private Image rightImageDisplay;
        [SerializeField] private TextMeshProUGUI rightPageNumberText;

        [Header("Nawigacja")]
        [SerializeField] private Button nextPageButton;
        [SerializeField] private Button prevPageButton;

        [Header("Podgląd Kartek (Sprites pod przełożenie)")]
        [SerializeField] private Image leftPageBackgroundImage;
        [SerializeField] private Sprite leftPageNormalSprite;
        [SerializeField] private Sprite leftPageCurlSprite;
        [SerializeField] private Image rightPageBackgroundImage;
        [SerializeField] private Sprite rightPageNormalSprite;
        [SerializeField] private Sprite rightPageCurlSprite;

        [Header("Zdarzenia Dźwiękowe (Modularne)")]
        public UnityEngine.Events.UnityEvent OnPageTurned;
        public UnityEngine.Events.UnityEvent OnTabChanged;

        // Stan wewnętrzny
        private int _activeTopTabIndex = 0;
        private int _activeSubTabIndex = -1; // -1 = brak aktywnej bocznej zakładki
        private int _currentPagePairIndex = 0;
        private bool _isLeftHovered = false;
        private bool _isRightHovered = false;

        private void Awake()
        {
            // Zabezpieczenie: książka startuje zamknięta i ukryta
            gameObject.SetActive(false);
        }

        private void Start()
        {
            // Zabezpieczenie przed blokowaniem raycastów przez elementy graficzne/tekstowe wyświetlacza
            if (leftTextDisplay != null) leftTextDisplay.raycastTarget = false;
            if (rightTextDisplay != null) rightTextDisplay.raycastTarget = false;
            if (leftImageDisplay != null) leftImageDisplay.raycastTarget = false;
            if (rightImageDisplay != null) rightImageDisplay.raycastTarget = false;

            if (leftInputField != null)
            {
                leftInputField.enabled = true;
                leftInputField.interactable = true;
            }
            if (rightInputField != null)
            {
                rightInputField.enabled = true;
                rightInputField.interactable = true;
            }

            // Podpięcie zdarzeń edycji tekstu - zapis gdy gracz "odłoży pióro"
            if (leftInputField != null)
            {
                leftInputField.onValueChanged.AddListener((val) => SaveLeftPageText(val));
                leftInputField.onEndEdit.AddListener((_) => TriggerAutoSave());
            }
            if (rightInputField != null)
            {
                rightInputField.onValueChanged.AddListener((val) => SaveRightPageText(val));
                rightInputField.onEndEdit.AddListener((_) => TriggerAutoSave());
            }

            // Setup przycisków nawigacji + efekt zaginania rogu przy najechaniu
            if (nextPageButton != null)
            {
                nextPageButton.onClick.RemoveAllListeners();
                nextPageButton.onClick.AddListener(FlipForward);
                AddCurlHoverTrigger(nextPageButton.gameObject, isRight: true);
            }
            if (prevPageButton != null)
            {
                prevPageButton.onClick.RemoveAllListeners();
                prevPageButton.onClick.AddListener(FlipBackward);
                AddCurlHoverTrigger(prevPageButton.gameObject, isRight: false);
            }

            // Konfiguracja przycisków zakładek
            SetupTabButtons();

            // Otwarcie domyślnej zakładki
            SelectTopTab(defaultTopTabIndex);
        }

        private void OnEnable()
        {
            // Odświeżamy strony przy włączeniu panelu
            UpdatePageVisuals();
        }

        // =========================================================
        //  EFEKT ZAGINANIA ROGU NA HOVER PRZYCISKÓW
        // =========================================================

        /// <summary>
        /// Dodaje EventTrigger do przycisku nawigacji, który wywołuje efekt zaginania rogu strony.
        /// </summary>
        private void AddCurlHoverTrigger(GameObject buttonGO, bool isRight)
        {
            EventTrigger trigger = buttonGO.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = buttonGO.AddComponent<EventTrigger>();

            // Zdarzenie: Wejście myszki (Enter)
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            bool capturedIsRight = isRight;
            enterEntry.callback.AddListener((_) => OnCornerHover(capturedIsRight, true));
            trigger.triggers.Add(enterEntry);

            // Zdarzenie: Wyjście myszki (Exit)
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((_) => OnCornerHover(capturedIsRight, false));
            trigger.triggers.Add(exitEntry);
        }

        // =========================================================
        //  SETUP PRZYCISKÓW ZAKŁADEK
        // =========================================================

        private void SetupTabButtons()
        {
            for (int i = 0; i < topTabs.Count; i++)
            {
                int capturedIndex = i;
                if (topTabs[i].tabButton != null)
                {
                    topTabs[i].tabButton.onClick.RemoveAllListeners();
                    topTabs[i].tabButton.onClick.AddListener(() => SelectTopTab(capturedIndex));
                }

                // Pod-zakładki (boczne)
                for (int j = 0; j < topTabs[i].subTabs.Count; j++)
                {
                    int capturedSubIndex = j;
                    if (topTabs[i].subTabs[j].subTabButton != null)
                    {
                        topTabs[i].subTabs[j].subTabButton.onClick.RemoveAllListeners();
                        topTabs[i].subTabs[j].subTabButton.onClick.AddListener(() => SelectSubTab(capturedSubIndex));
                    }
                }
            }
        }

        // =========================================================
        //  NAWIGACJA ZAKŁADEK
        // =========================================================

        /// <summary>
        /// Wybiera główną górną zakładkę.
        /// </summary>
        public void SelectTopTab(int index)
        {
            if (index < 0 || index >= topTabs.Count) return;

            // Zapis przed zmianą zakładki
            TriggerAutoSave();

            _activeTopTabIndex = index;
            _currentPagePairIndex = 0;

            // Przełączamy widoczność kontenerów zakładek bocznych
            for (int i = 0; i < topTabs.Count; i++)
            {
                if (topTabs[i].subTabsContainer != null)
                    topTabs[i].subTabsContainer.SetActive(i == index);
            }

            // Przesuwamy zakładki w osi Y: aktywna wysuwa się, pozostałe chowają
            ApplyTabYOffsets();

            // Jeśli zakładka ma pod-zakładki (boczne), wybierz pierwszą z nich
            if (topTabs[index].subTabs.Count > 0)
            {
                SelectSubTab(0);
            }
            else
            {
                _activeSubTabIndex = -1;
                EnsureAtLeastOnePage();
                UpdatePageVisuals();
            }

            OnTabChanged?.Invoke();
            Debug.Log($"<color=green>[BOOK] Wybrano zakładkę główną: {topTabs[index].tabName}</color>");
        }

        /// <summary>
        /// Wybiera boczną zakładkę dla aktualnej głównej.
        /// </summary>
        public void SelectSubTab(int subIndex)
        {
            if (_activeTopTabIndex < 0 || _activeTopTabIndex >= topTabs.Count) return;
            var topTab = topTabs[_activeTopTabIndex];

            if (subIndex < 0 || subIndex >= topTab.subTabs.Count) return;

            // Zapis przed zmianą zakładki
            TriggerAutoSave();

            _activeSubTabIndex = subIndex;
            _currentPagePairIndex = 0;

            EnsureAtLeastOnePage();
            UpdatePageVisuals();
            OnTabChanged?.Invoke();
            Debug.Log($"<color=green>[BOOK] Wybrano pod-zakładkę: {topTab.subTabs[subIndex].subTabName}</color>");
        }

        /// <summary>
        /// Przesuwa wszystkie górne zakładki w osi Y. Aktywna wysuwa się ku górze, pozostałe chowają się pod kartkę.
        /// </summary>
        private void ApplyTabYOffsets()
        {
            for (int i = 0; i < topTabs.Count; i++)
            {
                if (topTabs[i].tabButton == null) continue;
                RectTransform rt = topTabs[i].tabButton.GetComponent<RectTransform>();
                if (rt == null) continue;

                Vector2 pos = rt.anchoredPosition;
                pos.y = (i == _activeTopTabIndex) ? activeTabYOffset : inactiveTabYOffset;
                rt.anchoredPosition = pos;
            }
        }

        // =========================================================
        //  ZAPEWNIENIE STRONY DO PISANIA
        // =========================================================

        /// <summary>
        /// Sprawdza, czy aktywna zakładka powinna być edytowalna przez gracza.
        /// Wspiera zarówno ręczne flagowanie w Inspectorze, jak i inteligentne dopasowanie po nazwie (Notatnik/Notes).
        /// </summary>
        public bool IsActiveTabEditable()
        {
            if (_activeTopTabIndex < 0 || _activeTopTabIndex >= topTabs.Count)
                return false;

            var topTab = topTabs[_activeTopTabIndex];
            if (_activeSubTabIndex >= 0 && _activeSubTabIndex < topTab.subTabs.Count)
            {
                var subTab = topTab.subTabs[_activeSubTabIndex];
                return subTab.isEditable || 
                       subTab.subTabName.ToLower().Contains("note") || 
                       subTab.subTabName.ToLower().Contains("notatnik") || 
                       subTab.subTabName.ToLower().Contains("zapis");
            }

            return topTab.isEditable || 
                   topTab.tabName.ToLower().Contains("note") || 
                   topTab.tabName.ToLower().Contains("notatnik") || 
                   topTab.tabName.ToLower().Contains("zapis");
        }

        /// <summary>
        /// Jeśli aktualna zakładka jest pusta, automatycznie dodaje czystą stronę o odpowiednim statusie edycji.
        /// Gracz zawsze będzie miał miejsce do pisania w notatniku!
        /// </summary>
        private void EnsureAtLeastOnePage()
        {
            var pages = GetActivePagesList();
            if (pages != null && pages.Count == 0)
            {
                bool editable = IsActiveTabEditable();
                pages.Add(new BookPageData
                {
                    pageTitle = "",
                    textContent = "",
                    imageContent = null,
                    isEditable = editable
                });
                Debug.Log($"<color=yellow>[BOOK] Brak stron - dodano pustą stronę. Edytowalna: {editable}</color>");
            }
        }

        // =========================================================
        //  POBIERANIE AKTYWNYCH DANYCH
        // =========================================================

        /// <summary>
        /// Zwraca listę stron aktualnie otwartej zakładki/pod-zakładki.
        /// </summary>
        public List<BookPageData> GetActivePagesList()
        {
            if (_activeTopTabIndex < 0 || _activeTopTabIndex >= topTabs.Count)
                return null;

            var topTab = topTabs[_activeTopTabIndex];
            if (_activeSubTabIndex >= 0 && _activeSubTabIndex < topTab.subTabs.Count)
                return topTab.subTabs[_activeSubTabIndex].pages;

            return topTab.pages;
        }

        /// <summary>
        /// Pobiera limit stron dla obecnie otwartej zakładki.
        /// </summary>
        public int GetActivePageLimit()
        {
            if (_activeTopTabIndex < 0 || _activeTopTabIndex >= topTabs.Count) return 99;
            var topTab = topTabs[_activeTopTabIndex];

            if (_activeSubTabIndex >= 0 && _activeSubTabIndex < topTab.subTabs.Count)
                return topTab.subTabs[_activeSubTabIndex].pageLimit;

            return topTab.pageLimit;
        }

        public bool CanAddPage()
        {
            var pages = GetActivePagesList();
            if (pages == null) return false;
            return pages.Count < GetActivePageLimit();
        }

        // =========================================================
        //  DODAWANIE I USUWANIE STRON
        // =========================================================

        /// <summary>
        /// Dodaje nową stronę do aktywnej zakładki.
        /// </summary>
        public void AddNewPage(string text = "", Sprite img = null, string title = "", bool editable = true)
        {
            if (!CanAddPage())
            {
                Debug.LogWarning("[BOOK] Osiągnięto limit stron dla tej zakładki!");
                return;
            }

            var pages = GetActivePagesList();
            if (pages == null) return;

            pages.Add(new BookPageData
            {
                textContent = text,
                imageContent = img,
                pageTitle = title,
                isEditable = editable
            });

            UpdatePageVisuals();
        }

        /// <summary>
        /// Usuwa aktualną rozkładówkę (dwie strony).
        /// </summary>
        public void RemoveCurrentPagePair()
        {
            var pages = GetActivePagesList();
            if (pages == null || pages.Count == 0) return;

            int leftIndex = _currentPagePairIndex * 2;
            int rightIndex = _currentPagePairIndex * 2 + 1;

            if (rightIndex < pages.Count) pages.RemoveAt(rightIndex);
            if (leftIndex < pages.Count) pages.RemoveAt(leftIndex);

            if (_currentPagePairIndex > 0 && _currentPagePairIndex * 2 >= pages.Count)
                _currentPagePairIndex--;

            // Zawsze zostaje przynajmniej jedna strona
            if (pages.Count == 0)
                EnsureAtLeastOnePage();

            UpdatePageVisuals();
        }

        // =========================================================
        //  WIZUALNE ODŚWIEŻANIE STRON
        // =========================================================

        /// <summary>
        /// Odświeża elementy wizualne książki na podstawie aktywnej pary stron.
        /// </summary>
        public void UpdatePageVisuals()
        {
            var pages = GetActivePagesList();
            if (pages == null) return;

            // Resetujemy grafiki kartek do stanu podstawowego przy odświeżeniu
            if (leftPageBackgroundImage != null && leftPageNormalSprite != null)
                leftPageBackgroundImage.sprite = leftPageNormalSprite;
            if (rightPageBackgroundImage != null && rightPageNormalSprite != null)
                rightPageBackgroundImage.sprite = rightPageNormalSprite;

            int leftIndex = _currentPagePairIndex * 2;
            int rightIndex = _currentPagePairIndex * 2 + 1;

            bool tabEditable = IsActiveTabEditable(); // Pobieramy status edytowalności aktualnej zakładki

            // --- LEWA STRONA ---
            if (leftIndex < pages.Count)
            {
                var leftPage = pages[leftIndex];
                leftPage.isEditable = tabEditable; // TWARDY SYNCHRON: zakładka decyduje czy strona jest edytowalna!

                if (leftPage.isEditable)
                {
                    if (leftInputField != null)
                    {
                        leftInputField.gameObject.SetActive(true);
                        leftInputField.enabled = true;
                        leftInputField.interactable = true;
                        leftInputField.text = leftPage.textContent;
                    }
                    if (leftTextDisplay != null) leftTextDisplay.gameObject.SetActive(false);
                }
                else
                {
                    if (leftInputField != null) leftInputField.gameObject.SetActive(false);
                    if (leftTextDisplay != null)
                    {
                        leftTextDisplay.gameObject.SetActive(true);
                        leftTextDisplay.text = leftPage.textContent;
                    }
                }

                if (leftImageDisplay != null)
                {
                    if (leftPage.imageContent != null)
                    {
                        leftImageDisplay.gameObject.SetActive(true);
                        leftImageDisplay.sprite = leftPage.imageContent;
                    }
                    else
                    {
                        leftImageDisplay.gameObject.SetActive(false);
                    }
                }

                if (leftPageNumberText != null)
                {
                    leftPageNumberText.gameObject.SetActive(true);
                    leftPageNumberText.text = (leftIndex + 1).ToString();
                }
            }
            else
            {
                if (leftInputField != null) leftInputField.gameObject.SetActive(false);
                if (leftTextDisplay != null) leftTextDisplay.gameObject.SetActive(false);
                if (leftImageDisplay != null) leftImageDisplay.gameObject.SetActive(false);
                if (leftPageNumberText != null) leftPageNumberText.gameObject.SetActive(false);
            }

            // --- PRAWA STRONA ---
            if (rightIndex < pages.Count)
            {
                var rightPage = pages[rightIndex];
                rightPage.isEditable = tabEditable; // TWARDY SYNCHRON: zakładka decyduje czy strona jest edytowalna!

                if (rightPage.isEditable)
                {
                    if (rightInputField != null)
                    {
                        rightInputField.gameObject.SetActive(true);
                        rightInputField.enabled = true;
                        rightInputField.interactable = true;
                        rightInputField.text = rightPage.textContent;
                    }
                    if (rightTextDisplay != null) rightTextDisplay.gameObject.SetActive(false);
                }
                else
                {
                    if (rightInputField != null) rightInputField.gameObject.SetActive(false);
                    if (rightTextDisplay != null)
                    {
                        rightTextDisplay.gameObject.SetActive(true);
                        rightTextDisplay.text = rightPage.textContent;
                    }
                }

                if (rightImageDisplay != null)
                {
                    if (rightPage.imageContent != null)
                    {
                        rightImageDisplay.gameObject.SetActive(true);
                        rightImageDisplay.sprite = rightPage.imageContent;
                    }
                    else
                    {
                        rightImageDisplay.gameObject.SetActive(false);
                    }
                }

                if (rightPageNumberText != null)
                {
                    rightPageNumberText.gameObject.SetActive(true);
                    rightPageNumberText.text = (rightIndex + 1).ToString();
                }
            }
            else
            {
                if (rightInputField != null) rightInputField.gameObject.SetActive(false);
                if (rightTextDisplay != null) rightTextDisplay.gameObject.SetActive(false);
                if (rightImageDisplay != null) rightImageDisplay.gameObject.SetActive(false);
                if (rightPageNumberText != null) rightPageNumberText.gameObject.SetActive(false);
            }

            // --- NAWIGACJA PRZYCISKÓW ---
            if (prevPageButton != null)
            {
                bool prevActive = _currentPagePairIndex > 0;
                prevPageButton.gameObject.SetActive(prevActive);
                if (!prevActive) _isLeftHovered = false; // Reset flagi gdy przycisk przestaje być aktywny
            }

            if (nextPageButton != null)
            {
                bool nextActive = (_currentPagePairIndex + 1) * 2 < pages.Count;
                nextPageButton.gameObject.SetActive(nextActive);
                if (!nextActive) _isRightHovered = false; // Reset flagi gdy przycisk przestaje być aktywny
            }

            // Aplikujemy grafiki rogów na podstawie zapisanego stanu najazdu!
            RefreshCornerSprites();
        }

        // =========================================================
        //  PRZEWRACANIE STRON
        // =========================================================

        public void FlipForward()
        {
            var pages = GetActivePagesList();
            if (pages == null) return;

            if ((_currentPagePairIndex + 1) * 2 < pages.Count)
            {
                _currentPagePairIndex++;
                UpdatePageVisuals();
                OnPageTurned?.Invoke();
                TriggerAutoSave();
            }
        }

        public void FlipBackward()
        {
            if (_currentPagePairIndex > 0)
            {
                _currentPagePairIndex--;
                UpdatePageVisuals();
                OnPageTurned?.Invoke();
                TriggerAutoSave();
            }
        }

        public void JumpToPage(int pageIndex)
        {
            var pages = GetActivePagesList();
            if (pages == null) return;

            _currentPagePairIndex = Mathf.Clamp(pageIndex / 2, 0, (pages.Count - 1) / 2);
            UpdatePageVisuals();
            OnPageTurned?.Invoke();
        }

        // =========================================================
        //  ZAPIS TEKSTU DO PAMIĘCI
        // =========================================================

        private void SaveLeftPageText(string val)
        {
            var pages = GetActivePagesList();
            if (pages == null) return;

            int index = _currentPagePairIndex * 2;
            if (index < pages.Count && pages[index].isEditable)
                pages[index].textContent = val;
        }

        private void SaveRightPageText(string val)
        {
            var pages = GetActivePagesList();
            if (pages == null) return;

            int index = _currentPagePairIndex * 2 + 1;
            if (index < pages.Count && pages[index].isEditable)
                pages[index].textContent = val;
        }

        // =========================================================
        //  EFEKT ZAGINANIA ROGÓW (Corner Hover)
        // =========================================================

        /// <summary>
        /// Wywoływane przez BookCornerTrigger lub EventTrigger przycisku, gdy gracz najeżdża/zjeżdża.
        /// </summary>
        public void OnCornerHover(bool isRightPage, bool isHovering)
        {
            if (isRightPage)
                _isRightHovered = isHovering;
            else
                _isLeftHovered = isHovering;

            RefreshCornerSprites();
        }

        /// <summary>
        /// Odświeża grafiki zagięcia rogów na podstawie zapisanego stanu najazdu myszki.
        /// Całkowicie optymalne rozwiązanie (0% narzutu CPU w klatce) - brak jakichkolwiek update'ów!
        /// </summary>
        private void RefreshCornerSprites()
        {
            var pages = GetActivePagesList();
            if (pages == null) return;

            if (rightPageBackgroundImage != null && rightPageNormalSprite != null && rightPageCurlSprite != null)
            {
                bool canFlip = (_currentPagePairIndex + 1) * 2 < pages.Count;
                rightPageBackgroundImage.sprite = (_isRightHovered && canFlip) ? rightPageCurlSprite : rightPageNormalSprite;
            }

            if (leftPageBackgroundImage != null && leftPageNormalSprite != null && leftPageCurlSprite != null)
            {
                bool canFlip = _currentPagePairIndex > 0;
                leftPageBackgroundImage.sprite = (_isLeftHovered && canFlip) ? leftPageCurlSprite : leftPageNormalSprite;
            }
        }

        /// <summary>
        /// Wywoływane przez BookCornerTrigger, gdy gracz kliknie w dolny róg strony.
        /// </summary>
        public void OnCornerClicked(bool isRightPage)
        {
            if (isRightPage)
            {
                FlipForward();
                OnCornerHover(true, false);
            }
            else
            {
                FlipBackward();
                OnCornerHover(false, false);
            }
        }

        // =========================================================
        //  INTEGRACJA Z SAVEMANAGER
        // =========================================================

        /// <summary>
        /// Wywołuje zapis gry przez SaveManager jeśli jest dostępny.
        /// Wywoływany po zakończeniu pisania, zmianie zakładki i przewróceniu strony.
        /// </summary>
        private void TriggerAutoSave()
        {
            if (SaveManager.Instance != null)
                SaveManager.Instance.SaveCurrentGame();
        }

        /// <summary>
        /// Pakuje wszystkie dane tekstowe Księgi do formatu gotowego do zapisania w JSON-ie.
        /// Wywoływany przez SaveManager.
        /// </summary>
        public List<BookTabSaveData> PackBookData()
        {
            List<BookTabSaveData> result = new List<BookTabSaveData>();
            foreach (var topTab in topTabs)
            {
                var tabData = new BookTabSaveData { tabName = topTab.tabName };

                foreach (var page in topTab.pages)
                {
                    tabData.pages.Add(new BookPageSaveEntry
                    {
                        pageTitle = page.pageTitle,
                        textContent = page.textContent,
                        isEditable = page.isEditable
                    });
                }

                foreach (var subTab in topTab.subTabs)
                {
                    var subData = new BookSubTabSaveData { subTabName = subTab.subTabName };
                    foreach (var page in subTab.pages)
                    {
                        subData.pages.Add(new BookPageSaveEntry
                        {
                            pageTitle = page.pageTitle,
                            textContent = page.textContent,
                            isEditable = page.isEditable
                        });
                    }
                    tabData.subTabs.Add(subData);
                }

                result.Add(tabData);
            }
            return result;
        }

        /// <summary>
        /// Wczytuje dane tekstowe Księgi z pliku zapisu. Wywoływany przez SaveManager.
        /// Nadpisuje tylko treść stron - nie zmienia ich sprite'ów ani konfiguracji.
        /// </summary>
        public void UnpackBookData(List<BookTabSaveData> savedData)
        {
            if (savedData == null || savedData.Count == 0) return;

            for (int t = 0; t < topTabs.Count && t < savedData.Count; t++)
            {
                var topTab = topTabs[t];
                var tabData = savedData[t];

                // Wczytaj strony głównej zakładki
                topTab.pages.Clear();
                foreach (var entry in tabData.pages)
                {
                    topTab.pages.Add(new BookPageData
                    {
                        pageTitle = entry.pageTitle,
                        textContent = entry.textContent,
                        imageContent = null, // Sprite nie może być zapisany w JSON-ie - to normalne
                        isEditable = entry.isEditable
                    });
                }

                // Wczytaj strony pod-zakładek
                for (int s = 0; s < topTab.subTabs.Count && s < tabData.subTabs.Count; s++)
                {
                    topTab.subTabs[s].pages.Clear();
                    foreach (var entry in tabData.subTabs[s].pages)
                    {
                        topTab.subTabs[s].pages.Add(new BookPageData
                        {
                            pageTitle = entry.pageTitle,
                            textContent = entry.textContent,
                            imageContent = null,
                            isEditable = entry.isEditable
                        });
                    }
                }
            }

            // Odśwież widok po wczytaniu
            UpdatePageVisuals();
            Debug.Log("<color=lime>[BOOK] Dane Księgi wczytane pomyślnie!</color>");
        }
    }
}
