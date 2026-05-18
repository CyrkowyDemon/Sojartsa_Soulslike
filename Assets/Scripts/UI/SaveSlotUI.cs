using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Sojartsa.UI
{
    /// <summary>
    /// Skrypt siedzący na prefabie pojedynczego slotu zapisu w menu.
    /// </summary>
    public class SaveSlotUI : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private TextMeshProUGUI locationText;
        [SerializeField] private TextMeshProUGUI playTimeText;
        [SerializeField] private Button selectButton;

        private int _slotIndex;

        /// <summary>
        /// Wypełnia pola tekstowe danymi z zapisu.
        /// </summary>
        public void Setup(int slotIndex, SaveData data)
        {
            _slotIndex = slotIndex;
            
            if (data == null)
            {
                characterNameText.text = "PUSTY SLOT";
                locationText.text = "---";
                playTimeText.text = "00:00:00";
            }
            else
            {
                characterNameText.text = data.characterName;
                locationText.text = data.locationDisplayName;
                
                if (SaveManager.Instance != null)
                {
                    playTimeText.text = SaveManager.Instance.FormatTime(data.playTimeSeconds);
                }
            }

            // Podpinamy kliknięcie
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnClicked);
        }

        private void OnClicked()
        {
            if (SaveManager.Instance != null)
            {
                SaveData data = SaveManager.Instance.GetSaveMetadata(_slotIndex);
                string charName = data != null ? data.characterName : "Nieznany Wędrowiec";

                if (ConfirmationDialog.Instance != null)
                {
                    ConfirmationDialog.Instance.Show(
                        "WCZYTAJ GRĘ",
                        $"Czy chcesz wczytać postać \"{charName}\"?",
                        () => SaveManager.Instance.LoadGame(_slotIndex)
                    );
                }
                else
                {
                    SaveManager.Instance.LoadGame(_slotIndex);
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetFocused();
        }

        public void OnSelect(BaseEventData eventData)
        {
            SetFocused();
        }

        private void SetFocused()
        {
            Debug.Log($"[SaveSlotUI] Najechanie myszką/Padem na slot {_slotIndex}");
            SaveLoadPanel panel = GetComponentInParent<SaveLoadPanel>();
            if (panel != null)
            {
                panel.SetFocusedSlot(_slotIndex);
            }
        }
    }
}
