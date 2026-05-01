using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sojartsa.Inventory;
using Sojartsa.UI.DragDrop;

/// <summary>
/// Slot na stole barterowym (3x3). 
/// Implementuje IDragSource (można z niego podnieść) i IDropTarget (można na niego upuścić).
/// NIE zna InventorySlotUI – komunikuje się wyłącznie przez ItemPayload.
/// </summary>
public class BarterGridSlotUI : MonoBehaviour, IDropTarget, IDragSource
{
    public int slotIndex; // Od 0 do 8 (które to miejsce na stole)
    
    [Header("UI")]
    public Image iconImage;
    public TMP_Text amountText;

    private void OnEnable()
    {
        // Podpinamy się pod zmiany w barterze
        if (BarterTradingController.Instance != null)
            BarterTradingController.Instance.OnBarterChanged += RefreshFromData;
    }

    private void OnDisable()
    {
        if (BarterTradingController.Instance != null)
            BarterTradingController.Instance.OnBarterChanged -= RefreshFromData;
    }

    /// <summary>
    /// Odświeża wygląd slotu na podstawie danych w BarterTradingController.
    /// Wywoływane przez event, NIE w Update().
    /// </summary>
    public void RefreshFromData()
    {
        if (BarterTradingController.Instance == null) return;
        if (slotIndex >= BarterTradingController.Instance.barterSlots.Count) return;

        var slotData = BarterTradingController.Instance.barterSlots[slotIndex];

        if (slotData == null || slotData.IsEmpty)
        {
            if (iconImage) 
            {
                iconImage.enabled = true; // Musi być włączone dla Raycastów!
                iconImage.sprite = null;
                iconImage.color = new Color(1, 1, 1, 0); // Przezroczyste
            }
            if (amountText) amountText.text = "";
        }
        else
        {
            if (iconImage)
            {
                iconImage.enabled = true;
                iconImage.sprite = slotData.item.icon;
                iconImage.color = new Color(1, 1, 1, 1); // Widoczne
            }
            if (amountText) amountText.text = slotData.amount > 1 ? slotData.amount.ToString() : "";
        }
    }

    // --- IDragSource Implementation ---
    public bool CanDrag()
    {
        if (BarterTradingController.Instance == null) return false;
        return !BarterTradingController.Instance.barterSlots[slotIndex].IsEmpty;
    }

    public Sprite GetDragIcon()
    {
        return iconImage != null ? iconImage.sprite : null;
    }

    public ItemPayload GetTransferPayload()
    {
        if (BarterTradingController.Instance == null) return null;
        var slotData = BarterTradingController.Instance.barterSlots[slotIndex];

        return new ItemPayload
        {
            Source = ItemPayload.SourceType.BarterTable,
            Item = slotData.item,
            Amount = slotData.amount,
            SlotIndex = slotIndex,
            Offer = null
        };
    }

    public void OnDragStarted() { }
    public void OnDragEnded() { RefreshFromData(); }

    // --- IDropTarget Implementation ---
    public ItemPayload GetTargetPayload()
    {
        return new ItemPayload
        {
            Source = ItemPayload.SourceType.BarterTable,
            Item = null,
            Amount = 0,
            SlotIndex = slotIndex,
            Offer = null
        };
    }

    public void OnDropCompleted()
    {
        RefreshFromData();
    }
}
