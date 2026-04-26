using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace Sojartsa.Inventory.UI
{
    /// <summary>
    /// Reprezentacja wizualna pojedynczego slotu w UI.
    /// </summary>
    public class InventorySlotUI : MonoBehaviour, IPointerDownHandler
    {
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                // Wyłączamy nawigację, żeby przycisk nie "zostawał" zaznaczony
                Navigation customNav = new Navigation();
                customNav.mode = Navigation.Mode.None;
                _button.navigation = customNav;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Natychmiastowe odznaczenie przy dotknięciu
            // if (EventSystem.current != null)
            //     EventSystem.current.SetSelectedGameObject(null);
        }
        [Header("Elementy UI")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI amountText;

        [Header("Tło Slotu")]
        [SerializeField] private Sprite emptySprite;
        [SerializeField] private Sprite filledSprite;

        private InventorySlot _slot;

        /// <summary>
        /// Odświeża wygląd slotu na podstawie przekazanych danych.
        /// </summary>
        public void UpdateSlot(InventorySlot newSlot)
        {
            Debug.Log($"UpdateSlot wywołany dla: {gameObject.name}. Czy nowy slot pusty? {(newSlot == null || newSlot.IsEmpty)}");
            _slot = newSlot;

            if (_slot == null || _slot.IsEmpty)
            {
                if (iconImage != null) iconImage.enabled = false;
                amountText.text = "";
                SetBackgroundSprite(emptySprite);
                return;
            }

            if (iconImage != null)
            {
                iconImage.enabled = true;
                iconImage.sprite = _slot.item.icon;
            }
            SetBackgroundSprite(filledSprite);
            amountText.text = (_slot.amount > 1) ? _slot.amount.ToString() : "";
        }

        /// <summary>
        /// Metoda wywoływana po kliknięciu w slot w UI.
        /// </summary>
        public void OnSlotClick()
        {
            if (_slot == null || _slot.IsEmpty)
            {
                if (ItemDetailsUI.Instance != null) ItemDetailsUI.Instance.ResetDetails();
                return;
            }

            // Pokazujemy detale przedmiotu i podgląd statystyk
            if (ItemDetailsUI.Instance != null)
                ItemDetailsUI.Instance.ShowItemDetails(_slot.item);
            
            if (PlayerStatsWindowUI.Instance != null)
                PlayerStatsWindowUI.Instance.PreviewItemBonus(_slot.item);

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
        private void SetBackgroundSprite(Sprite sprite)
        {
            if (sprite == null) return;

            // Zmieniamy tło przez Image na samym przycisku
            Image bg = GetComponent<Image>();
            if (bg != null) bg.sprite = sprite;
        }

        // --- DRAG & DROP HELPERS ---

        public bool IsEmpty() => _slot == null || _slot.IsEmpty;
        public Sprite GetIcon() => _slot?.item?.icon;
        public InventorySlot GetSlotData() => _slot;

        public void SetVisualsActive(bool active)
        {
            if (iconImage != null) iconImage.enabled = active && !IsEmpty();
            if (amountText != null) amountText.enabled = active && !IsEmpty();
        }

        // ---
    }
}
