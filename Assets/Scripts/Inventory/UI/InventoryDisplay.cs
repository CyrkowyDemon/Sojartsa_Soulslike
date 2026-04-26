using System.Collections.Generic;
using UnityEngine;
using Sojartsa.Inventory.UI;

namespace Sojartsa.Inventory.UI
{
    /// <summary>
    /// Zarządza odświeżaniem panelu ekwipunku lub enchantu.
    ///
    /// TRYBY:
    /// - Inventory (isBag=false, isEnchantPanel=false): pokazuje 27 slotów + Equipment sloty 0,1,2
    /// - Enchant   (isBag=true,  isEnchantPanel=true):  pokazuje 18 bagSlotów + Equipment slot 3
    /// </summary>
    public class InventoryDisplay : MonoBehaviour
    {
        [Header("Ustawienia")]
        [SerializeField] private InventoryController controller;

        [Tooltip("Siatka głównych slotów (27 lub 18 kwadratów)")]
        [SerializeField] private List<InventorySlotUI> uiSlots = new List<InventorySlotUI>();

        [Tooltip("Sloty ekwipunku widoczne w TYM panelu.\n" +
                 "Inventory: przeciągnij tu 3 sloty (Main/Off/Consumable).\n" +
                 "Enchant:   przeciągnij tu 1 slot (Enchant Orb).")]
        [SerializeField] private List<InventorySlotUI> equipmentSlotsUI = new List<InventorySlotUI>();

        [Tooltip("Odznaczone = panel Inventory (grid 27 + eq sloty 0-2).\n" +
                 "Zaznaczone = panel Enchant (bag 18 + eq slot 3).")]
        [SerializeField] private bool isBag = false;

        private void OnEnable()
        {
            if (controller == null) controller = InventoryController.Instance;

            if (controller != null)
            {
                controller.OnInventoryChanged += RefreshDisplay;
                controller.OnEquipmentChanged += RefreshDisplay;
            }

            RefreshDisplay();
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.OnInventoryChanged -= RefreshDisplay;
                controller.OnEquipmentChanged -= RefreshDisplay;
            }
        }

        /// <summary>
        /// Synchronizuje dane z InventoryController z widokiem UI.
        /// </summary>
        public void RefreshDisplay()
        {
            if (controller == null)
            {
                Debug.LogWarning("[InventoryDisplay] Brak przypisanego InventoryController!");
                return;
            }

            // 1. Główna siatka: Bag (18) lub Inventory (27)
            var targetData = isBag ? controller.bagSlots : controller.inventorySlots;
            UpdateSlotList(uiSlots, targetData);

            // 2. Sloty ekwipunku – każdy panel dostaje SWÓJ kawałek listy equipmentSlots
            if (equipmentSlotsUI == null || equipmentSlotsUI.Count == 0) return;

            if (!isBag)
            {
                // Panel Inventory: sloty 0, 1, 2 (Broń, Offhand, Użytkowe)
                // Lista equipmentSlotsUI powinna mieć 3 elementy
                for (int i = 0; i < equipmentSlotsUI.Count; i++)
                {
                    int dataIndex = i; // 0, 1, 2
                    if (dataIndex < controller.equipmentSlots.Count)
                        equipmentSlotsUI[i].UpdateSlot(controller.equipmentSlots[dataIndex]);
                    else
                        equipmentSlotsUI[i].UpdateSlot(null);
                }
            }
            else
            {
                // Panel Enchant: slot 3 (Enchant Orb)
                // Lista equipmentSlotsUI powinna mieć 1 element
                int enchantIndex = 3;
                for (int i = 0; i < equipmentSlotsUI.Count; i++)
                {
                    if (enchantIndex < controller.equipmentSlots.Count)
                        equipmentSlotsUI[i].UpdateSlot(controller.equipmentSlots[enchantIndex]);
                    else
                        equipmentSlotsUI[i].UpdateSlot(null);
                }
            }
        }

        private void UpdateSlotList(List<InventorySlotUI> uiList, List<InventorySlot> dataList)
        {
            for (int i = 0; i < uiList.Count; i++)
            {
                if (i < dataList.Count)
                    uiList[i].UpdateSlot(dataList[i]);
                else
                    uiList[i].UpdateSlot(null);
            }
        }

        // --- POMOCNIKI DLA DRAG & DROP ---

        /// <summary>
        /// Zwraca indeks slotu w odpowiedniej liście danych.
        /// isEquipment=true oznacza, że slot pochodzi z listy equipmentSlots.
        /// enchantOffset: w trybie Enchant equipment slot startuje od indeksu 3.
        /// </summary>
        public int GetSlotIndex(InventorySlotUI slot, out bool isEquipment)
        {
            isEquipment = false;
            int index = uiSlots.IndexOf(slot);
            if (index != -1) return index;

            index = equipmentSlotsUI.IndexOf(slot);
            if (index != -1)
            {
                isEquipment = true;
                // W trybie Enchant: offset = 3, bo to jest 4. slot w kontrolerze
                if (isBag) index += 3;
            }
            return index;
        }

        public bool IsBagMode() => isBag;
    }
}

