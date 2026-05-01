using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Sojartsa.UI.DragDrop;

namespace Sojartsa.Inventory.UI
{
    /// <summary>
    /// Reprezentacja wizualna pojedynczego slotu w UI.
    /// 
    /// UNIWERSALNY – nie zna żadnego konkretnego systemu (sklepu, barteru).
    /// Wie tylko, że jest slotem w jakimś kontenerze (Inventory, Bag lub Equipment).
    /// </summary>
    public class InventorySlotUI : MonoBehaviour, IPointerDownHandler, IDragSource, IDropTarget
    {
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                Navigation customNav = new Navigation();
                customNav.mode = Navigation.Mode.None;
                _button.navigation = customNav;
            }
        }

        public void OnPointerDown(PointerEventData eventData) { }

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
            _slot = newSlot;

            if (_slot == null || _slot.IsEmpty)
            {
                if (iconImage != null) iconImage.enabled = false;
                if (amountText != null) amountText.text = "";
                SetBackgroundSprite(emptySprite);
                return;
            }

            if (iconImage != null)
            {
                iconImage.enabled = true;
                iconImage.sprite = _slot.item.icon;
            }
            SetBackgroundSprite(filledSprite);
            
            if (amountText != null) 
            {
                amountText.enabled = true;
                amountText.text = (_slot.amount > 1) ? _slot.amount.ToString() : "";
            }
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

        // --- IDragSource Implementation ---
        public bool CanDrag() => !IsEmpty();
        public Sprite GetDragIcon() => GetIcon();
        public void OnDragStarted() => SetVisualsActive(false);
        public void OnDragEnded() => SetVisualsActive(true);

        public ItemPayload GetTransferPayload()
        {
            InventoryDisplay display = GetComponentInParent<InventoryDisplay>();
            if (display == null) return null;

            bool isEquip;
            int index = display.GetSlotIndex(this, out isEquip);
            if (index == -1) return null;

            ItemPayload.SourceType sourceType;
            if (isEquip)
                sourceType = ItemPayload.SourceType.Equipment;
            else if (display.IsBagMode())
                sourceType = ItemPayload.SourceType.Bag;
            else
                sourceType = ItemPayload.SourceType.Inventory;

            return new ItemPayload
            {
                Source = sourceType,
                Item = _slot?.item,
                Amount = _slot?.amount ?? 0,
                SlotIndex = index,
                Offer = null
            };
        }

        // --- IDropTarget Implementation ---
        public ItemPayload GetTargetPayload()
        {
            // Zwracamy info o SOBIE – "kim jestem jako cel"
            InventoryDisplay display = GetComponentInParent<InventoryDisplay>();
            if (display == null) return new ItemPayload();

            bool isEquip;
            int index = display.GetSlotIndex(this, out isEquip);

            ItemPayload.SourceType targetType;
            if (isEquip)
                targetType = ItemPayload.SourceType.Equipment;
            else if (display.IsBagMode())
                targetType = ItemPayload.SourceType.Bag;
            else
                targetType = ItemPayload.SourceType.Inventory;

            return new ItemPayload
            {
                Source = targetType,
                Item = _slot?.item,
                Amount = _slot?.amount ?? 0,
                SlotIndex = index,
                Offer = null
            };
        }

        public void OnDropCompleted()
        {
            // UI odświeży się automatycznie przez event OnInventoryChanged
        }
    }
}
