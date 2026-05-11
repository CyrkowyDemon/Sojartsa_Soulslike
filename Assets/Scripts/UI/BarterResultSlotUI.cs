using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sojartsa.Inventory;
using Sojartsa.UI.DragDrop;

/// <summary>
/// Mordo, to jest specjalny skrypt dla Twojego slotu "WYNIK" (1x1).
/// On nie tylko pokazuje ile dostaniesz, ale też pozwala Ci "odebrać" kasę (Finalize Sale).
/// </summary>
public class BarterResultSlotUI : MonoBehaviour, IDragSource
{
    [Header("UI")]
    public Image iconImage;
    public TMP_Text amountText;

    private void OnEnable()
    {
        if (BarterTradingController.Instance != null)
            BarterTradingController.Instance.OnBarterChanged += Refresh;
    }

    private void OnDisable()
    {
        if (BarterTradingController.Instance != null)
            BarterTradingController.Instance.OnBarterChanged -= Refresh;
    }

    public void Refresh()
    {
        if (BarterTradingController.Instance == null) return;
        
        var slot = BarterTradingController.Instance.sellResultSlot;
        
        if (slot.IsEmpty)
        {
            if (iconImage) iconImage.color = new Color(1, 1, 1, 0);
            if (amountText) amountText.text = "";
        }
        else
        {
            if (iconImage)
            {
                iconImage.sprite = slot.item.icon;
                iconImage.color = new Color(1, 1, 1, 1);
            }
            if (amountText) amountText.text = slot.amount.ToString();
        }
    }

    // --- IDragSource Implementation ---
    public bool CanDrag()
    {
        return BarterTradingController.Instance != null && !BarterTradingController.Instance.sellResultSlot.IsEmpty;
    }

    public Sprite GetDragIcon() => iconImage ? iconImage.sprite : null;

    public ItemPayload GetTransferPayload()
    {
        var slot = BarterTradingController.Instance.sellResultSlot;
        return new ItemPayload
        {
            Source = ItemPayload.SourceType.BarterTable, // Traktujemy to jako stół, ale z indeksem -1
            Item = slot.item,
            Amount = slot.amount,
            SlotIndex = -1 // Specjalny indeks dla wyniku
        };
    }

    public void OnDragStarted() { }

    public void OnDragEnded()
    {
        // Mordo, jeśli gracz "wyjął" kasę ze slotu wyniku, to finalizujemy sprzedaż!
        if (BarterTradingController.Instance != null)
        {
            BarterTradingController.Instance.FinalizeSale();
        }
    }
}
