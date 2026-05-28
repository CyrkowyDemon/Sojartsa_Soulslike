using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Sojartsa.UI
{
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

        private void Awake()
        {
            // Zabezpieczenie: książka startuje zamknięta i ukryta
            gameObject.SetActive(false);
        }

        private void Start()
        {
            // Podpięcie zdarzeń edycji
            if (leftInputField != null)
                leftInputField.onValueChanged.AddListener((val) => SaveLeftPageText(val));
            if (rightInputField != null)
                rightInputField.onValueChanged.AddListener((val) => SaveRightPageText(val));

            // Setup przycisków nawigacji
            if (nextPageButton != null)
            {
                nextPageButton.onClick.RemoveAllListeners();
                nextPageButton.onClick.AddListener(FlipForward);
            }
            if (prevPageButton != null)
            {
                prevPageButton.onClick.RemoveAllListeners();
                prevPageButton.onClick.AddListener(FlipBackward);
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

        /// <summary>
        /// Wybiera główną górną zakładkę.
        /// </summary>
        public void SelectTopTab(int index)
        {
            if (index < 0 || index >= topTabs.Count) return;

            _activeTopTabIndex = index;
            _currentPagePairIndex = 0;

            // Przełączamy widoczność kontenerów zakładek bocznych
            for (int i = 0; i < topTabs.Count; i++)
            {
                if (topTabs[i].subTabsContainer != null)
                {
                    topTabs[i].subTabsContainer.SetActive(i == index);
                }
            }

            // Jeśli zakładka ma pod-zakładki (boczne), wybierz pierwszą z nich
            if (topTabs[index].subTabs.Count > 0)
            {
                SelectSubTab(0);
            }
            else
            {
                _activeSubTabIndex = -1;
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

            _activeSubTabIndex = subIndex;
            _currentPagePairIndex = 0;

            UpdatePageVisuals();
            OnTabChanged?.Invoke();
            Debug.Log($"<color=green>[BOOK] Wybrano pod-zakładkę: {topTab.subTabs[subIndex].subTabName}</color>");
        }

        /// <summary>
        /// Zwraca listę stron aktualnie otwartej zakładki/pod-zakładki.
        /// </summary>
        public List<BookPageData> GetActivePagesList()
        {
            if (_activeTopTabIndex < 0 || _activeTopTabIndex >= topTabs.Count)
                return null;

            var topTab = topTabs[_activeTopTabIndex];
            if (_activeSubTabIndex >= 0 && _activeSubTabIndex < topTab.subTabs.Count)
            {
                return topTab.subTabs[_activeSubTabIndex].pages;
            }
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
            {
                return topTab.subTabs[_activeSubTabIndex].pageLimit;
            }
            return topTab.pageLimit;
        }

        public bool CanAddPage()
        {
            var pages = GetActivePagesList();
            if (pages == null) return false;
            return pages.Count < GetActivePageLimit();
        }

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
            {
                _currentPagePairIndex--;
            }

            if (pages.Count == 0)
            {
                AddNewPage("", null, "", true);
            }

            UpdatePageVisuals();
        }

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

            // --- LEWA STRONA ---
            if (leftIndex < pages.Count)
            {
                var leftPage = pages[leftIndex];

                if (leftPage.isEditable)
                {
                    if (leftInputField != null)
                    {
                        leftInputField.gameObject.SetActive(true);
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

                if (rightPage.isEditable)
                {
                    if (rightInputField != null)
                    {
                        rightInputField.gameObject.SetActive(true);
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
                prevPageButton.gameObject.SetActive(_currentPagePairIndex > 0);
            }

            if (nextPageButton != null)
            {
                nextPageButton.gameObject.SetActive((_currentPagePairIndex + 1) * 2 < pages.Count);
            }
        }

        public void FlipForward()
        {
            var pages = GetActivePagesList();
            if (pages == null) return;

            if ((_currentPagePairIndex + 1) * 2 < pages.Count)
            {
                _currentPagePairIndex++;
                UpdatePageVisuals();
                OnPageTurned?.Invoke();
            }
        }

        public void FlipBackward()
        {
            if (_currentPagePairIndex > 0)
            {
                _currentPagePairIndex--;
                UpdatePageVisuals();
                OnPageTurned?.Invoke();
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

        private void SaveLeftPageText(string val)
        {
            var pages = GetActivePagesList();
            if (pages == null) return;

            int index = _currentPagePairIndex * 2;
            if (index < pages.Count && pages[index].isEditable)
            {
                pages[index].textContent = val;
            }
        }

        private void SaveRightPageText(string val)
        {
            var pages = GetActivePagesList();
            if (pages == null) return;

            int index = _currentPagePairIndex * 2 + 1;
            if (index < pages.Count && pages[index].isEditable)
            {
                pages[index].textContent = val;
            }
        }

        /// <summary>
        /// Wywoływane przez BookCornerTrigger, gdy gracz najeżdża lub zjeżdża z dolnego rogu strony.
        /// </summary>
        public void OnCornerHover(bool isRightPage, bool isHovering)
        {
            var pages = GetActivePagesList();
            if (pages == null) return;

            if (isRightPage)
            {
                if (rightPageBackgroundImage != null && rightPageNormalSprite != null && rightPageCurlSprite != null)
                {
                    // Zginamy róg tylko wtedy, gdy możemy przewinąć do przodu
                    bool canFlip = (_currentPagePairIndex + 1) * 2 < pages.Count;
                    rightPageBackgroundImage.sprite = (isHovering && canFlip) ? rightPageCurlSprite : rightPageNormalSprite;
                }
            }
            else
            {
                if (leftPageBackgroundImage != null && leftPageNormalSprite != null && leftPageCurlSprite != null)
                {
                    // Zginamy róg tylko wtedy, gdy możemy przewinąć w tył
                    bool canFlip = _currentPagePairIndex > 0;
                    leftPageBackgroundImage.sprite = (isHovering && canFlip) ? leftPageCurlSprite : leftPageNormalSprite;
                }
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
                OnCornerHover(true, false); // Wyłączamy curl po przewróceniu strony
            }
            else
            {
                FlipBackward();
                OnCornerHover(false, false);
            }
        }
    }
}
