using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sojartsa.UI.DragDrop;

/// <summary>
/// Slot wyświetlający ofertę handlową NPC (co kupujesz i za ile).
/// Implementuje IDragSource – gracz przeciąga ofertę na slot plecaka, żeby kupić.
/// NIE zna InventorySlotUI – komunikuje się wyłącznie przez ItemPayload.
/// </summary>
public class BarterDisplaySlot : MonoBehaviour, IDragSource
{
    [Header("UI - Wynik")]
    public Image resultIcon;
    public TMP_Text resultAmountText;

    [Header("UI - Koszt 1")]
    public GameObject cost1Parent;
    public Image cost1Icon;
    public TMP_Text cost1AmountText;

    [Header("UI - Koszt 2")]
    public GameObject cost2Parent;
    public Image cost2Icon;
    public TMP_Text cost2AmountText;

    private TradeOfferData _currentOffer;
    public TradeOfferData CurrentOffer => _currentOffer;

    public void Setup(TradeOfferData offer)
    {
        _currentOffer = offer;

        if (offer == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        // Ustawienie wyniku (co gracz dostanie)
        if (resultIcon)
        {
            resultIcon.sprite = offer.resultItem.icon;
            if (resultAmountText) resultAmountText.text = offer.resultAmount > 1 ? offer.resultAmount.ToString() : "";
        }

        // Ustawienie kosztu 1
        if (cost1Parent)
        {
            cost1Parent.SetActive(offer.costItem1 != null);
            if (offer.costItem1 != null)
            {
                if (cost1Icon) cost1Icon.sprite = offer.costItem1.icon;
                if (cost1AmountText) cost1AmountText.text = offer.GetFinalCost1().ToString();
            }
        }

        // Ustawienie kosztu 2
        if (cost2Parent)
        {
            cost2Parent.SetActive(offer.costItem2 != null);
            if (offer.costItem2 != null)
            {
                if (cost2Icon) cost2Icon.sprite = offer.costItem2.icon;
                if (cost2AmountText) cost2AmountText.text = offer.GetFinalCost2().ToString();
            }
        }
    }

    // --- IDragSource Implementation ---

    public bool CanDrag()
    {
        if (_currentOffer == null) return false;
        
        // Sprawdzamy czy gracza stać, zanim pozwolimy mu "podnieść" ikonę
        if (BarterTradingController.Instance != null && !BarterTradingController.Instance.CanAfford(_currentOffer))
        {
            Debug.Log("[BARTER] Nie stać Cię na to!");
            return false;
        }
        return true;
    }

    public Sprite GetDragIcon() => resultIcon != null ? resultIcon.sprite : null;

    public ItemPayload GetTransferPayload()
    {
        return new ItemPayload
        {
            Source = ItemPayload.SourceType.ShopOffer,
            Item = _currentOffer?.resultItem,
            Amount = _currentOffer?.resultAmount ?? 0,
            SlotIndex = -1,
            Offer = _currentOffer
        };
    }

    public void OnDragStarted() { }
    public void OnDragEnded() { }
}
