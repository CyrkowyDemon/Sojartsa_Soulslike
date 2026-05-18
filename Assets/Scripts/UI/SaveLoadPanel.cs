using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Sojartsa.UI
{
    /// <summary>
    /// Zarządza listą zapisów (Scroll View) w menu wczytywania.
    /// </summary>
    public class SaveLoadPanel : MonoBehaviour
    {
        [Header("Konfiguracja Listy")]
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private Transform container; // Content w ScrollView
        
        [Header("Ustawienia")]
        [SerializeField] private int maxSlots = 10;
        
        [Header("Input")]
        [SerializeField] private InputReader inputReader;

        private List<GameObject> _spawnedSlots = new List<GameObject>();
        private int _focusedSlotIndex = -1;

        public void SetFocusedSlot(int slotIndex)
        {
            _focusedSlotIndex = slotIndex;
        }

        private void OnEnable()
        {
            if (inputReader != null) inputReader.DeleteSaveEvent += OnDeletePressed;
            RefreshList();
        }

        private void OnDisable()
        {
            if (inputReader != null) inputReader.DeleteSaveEvent -= OnDeletePressed;
        }

        private void OnDeletePressed()
        {
            Debug.Log($"[SaveLoadPanel] OnDeletePressed odebrane! Aktywny slot: {_focusedSlotIndex}");
            if (_focusedSlotIndex >= 0 && _focusedSlotIndex < maxSlots && SaveManager.Instance != null && SaveManager.Instance.HasSave(_focusedSlotIndex))
            {
                SaveData meta = SaveManager.Instance.GetSaveMetadata(_focusedSlotIndex);
                string charName = meta != null ? meta.characterName : "Nieznany Wędrowiec";

                if (ConfirmationDialog.Instance != null)
                {
                    ConfirmationDialog.Instance.Show(
                        "USUŃ ZAPIS",
                        $"Czy na pewno chcesz BEZPOWROTNIE usunąć postać \"{charName}\"?",
                        () =>
                        {
                            Debug.Log($"[SaveLoadPanel] Usuwanie zapisu {_focusedSlotIndex}");
                            SaveManager.Instance.DeleteGame(_focusedSlotIndex);
                            _focusedSlotIndex = -1;
                            RefreshList();
                        }
                    );
                }
                else
                {
                    SaveManager.Instance.DeleteGame(_focusedSlotIndex);
                    _focusedSlotIndex = -1;
                    RefreshList();
                }
            }
            else
            {
                Debug.Log($"[SaveLoadPanel] Nie usunięto, bo slot jest pusty, zły index lub brak SaveManagera.");
            }
        }

        public void RefreshList()
        {
            // Czyścimy starą listę
            foreach (var slot in _spawnedSlots) Destroy(slot);
            _spawnedSlots.Clear();

            if (SaveManager.Instance == null) return;

            // Tworzymy nowe sloty
            for (int i = 0; i < maxSlots; i++)
            {
                SaveData meta = SaveManager.Instance.GetSaveMetadata(i);
                
                // Pokazujemy TYLKO te sloty które istnieją:
                if (meta == null) continue;

                GameObject go = Instantiate(slotPrefab, container);
                _spawnedSlots.Add(go);

                SaveSlotUI slotUI = go.GetComponent<SaveSlotUI>();
                if (slotUI != null)
                {
                    slotUI.Setup(i, meta);
                }
            }
        }
    }
}
