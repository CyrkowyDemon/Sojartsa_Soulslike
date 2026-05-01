using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    [Header("Referencje UI")]
    public GameObject shopPanel;
    public Transform itemsParent;
    public GameObject shopItemPrefab;
    public TMP_Text currencyText;
    public TMP_Text shopNameText;

    private ShopData _currentShopData;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (shopPanel != null) shopPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged += UpdateCurrencyDisplay;
        }
    }

    private void OnDisable()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged -= UpdateCurrencyDisplay;
        }
    }

    public void OpenShop(ShopData data)
    {
        _currentShopData = data;
        if (shopNameText != null) shopNameText.text = data.shopName;
        
        shopPanel.SetActive(true);
        UpdateCurrencyDisplay(CurrencyManager.Instance.CurrentCurrency);
        RefreshShopList();
        
        SelectFirstItem();
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
    }

    private void UpdateCurrencyDisplay(int amount)
    {
        if (currencyText != null) currencyText.text = $"Posiadasz: {amount} monet";
    }

    public void RefreshShopList()
    {
        foreach (Transform child in itemsParent) Destroy(child.gameObject);

        foreach (var entry in _currentShopData.itemsForSale)
        {
            if (entry.item == null) continue;

            GameObject itemObj = Instantiate(shopItemPrefab, itemsParent);
            
            TMP_Text nameText = itemObj.transform.Find("Name")?.GetComponent<TMP_Text>();
            TMP_Text priceText = itemObj.transform.Find("Price")?.GetComponent<TMP_Text>();
            Image iconImage = itemObj.transform.Find("Icon")?.GetComponent<Image>();
            Button buyButton = itemObj.GetComponent<Button>();

            if (nameText) nameText.text = entry.item.itemName;
            if (priceText) priceText.text = $"{entry.GetPrice()} monet";
            if (iconImage) iconImage.sprite = entry.item.icon;

            if (buyButton)
            {
                buyButton.onClick.AddListener(() => TryBuyItem(entry));
            }
        }
    }

    private void TryBuyItem(ShopData.ShopEntry entry)
    {
        int price = entry.GetPrice();

        if (CurrencyManager.Instance.SpendCurrency(price))
        {
            if (InventoryController.Instance.AddItem(entry.item, 1))
            {
                Debug.Log($"<color=green>[SHOP] Kupiono: {entry.item.itemName}</color>");
                if (entry.stockCount > 0)
                {
                    entry.stockCount--;
                    if (entry.stockCount == 0) RefreshShopList();
                }
            }
            else
            {
                Debug.LogWarning("[SHOP] Brak miejsca w ekwipunku! Zwracam środki.");
                CurrencyManager.Instance.AddCurrency(price);
            }
        }
    }

    private void SelectFirstItem()
    {
        if (itemsParent != null && itemsParent.childCount > 0 && UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(itemsParent.GetChild(0).gameObject);
        }
    }
}
