using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Widok UI sklepu barterowego. Pokazuje oferty handlarza i panel skupu.
/// Podpina się pod event OnBarterChanged, żeby odświeżać wynik skupu automatycznie.
/// </summary>
public class BarterUI : MonoBehaviour
{
    public static BarterUI Instance { get; private set; }

    [Header("Panele")]
    public GameObject mainPanel;
    public GameObject buyView;
    public GameObject sellView;

    [Header("Oferty (Przeciągnij swoje 15 slotów)")]
    public List<BarterDisplaySlot> offerSlots = new List<BarterDisplaySlot>();

    [Header("Skup (1x1 Wynik)")]
    public TMP_Text sellValueText;

    [Header("Tytuły")]
    public TMP_Text shopNameText;

    [Header("Input")]
    [SerializeField] private InputReader inputReader;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Upewnijmy się, że każdy przypisany w Inspektorze slot ma odpowiednie skrypty D&D
        foreach (var slot in offerSlots)
        {
            if (slot == null) continue;

            if (slot.GetComponent<CanvasGroup>() == null)
                slot.gameObject.AddComponent<CanvasGroup>();
            
            if (slot.GetComponent<Sojartsa.Inventory.UI.InventoryDragHandler>() == null)
                slot.gameObject.AddComponent<Sojartsa.Inventory.UI.InventoryDragHandler>();
        }

        if (mainPanel) mainPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (inputReader != null)
            inputReader.CancelEvent += OnCancelPressed;
    }

    private void OnDisable()
    {
        if (inputReader != null)
            inputReader.CancelEvent -= OnCancelPressed;
    }

    private void OnCancelPressed()
    {
        if (mainPanel != null && mainPanel.activeSelf)
            CloseUI();
    }

    public void OpenBuy(TradeShopData data)
    {
        if (mainPanel) mainPanel.SetActive(true);
        if (buyView) buyView.SetActive(true);
        if (sellView) sellView.SetActive(false);

        if (shopNameText) shopNameText.text = data.shopName;

        Debug.Log($"[BARTER UI] OpenBuy: Data ma {data?.offers?.Count} ofert. Znaleziono {offerSlots.Count} slotów UI.");

        if (data == null || data.offers == null) return;

        for (int i = 0; i < offerSlots.Count; i++)
        {
            if (i < data.offers.Count)
                offerSlots[i].Setup(data.offers[i]);
            else
                offerSlots[i].Setup(null);
        }
    }

    public void OpenSell(TradeShopData data)
    {
        if (mainPanel) mainPanel.SetActive(true);
        if (buyView) buyView.SetActive(false);
        if (sellView) sellView.SetActive(true);

        if (shopNameText) shopNameText.text = data.shopName + " - Skup";
        
        RefreshSellDisplay();
    }

    public void CloseUI()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (BarterTradingController.Instance != null)
        {
            BarterTradingController.Instance.CloseTrade();
        }
        
        // Uwolnij NPC ze stanu Menu
        if (NPCMenuUI.Instance != null)
        {
            NPCMenuUI.Instance.CloseMenu();
        }
    }

    /// <summary>
    /// Odświeża wyświetlany wynik skupu. Wywoływane przez BarterTradingController.RefreshSellValue().
    /// </summary>
    public void RefreshSellDisplay()
    {
        if (BarterTradingController.Instance == null) return;
        
        var result = BarterTradingController.Instance.sellResultSlot;
        if (sellValueText)
        {
            sellValueText.text = result.IsEmpty ? "0" : result.amount.ToString();
        }
    }

    public void OnClickExit()
    {
        CloseUI();
    }
}
