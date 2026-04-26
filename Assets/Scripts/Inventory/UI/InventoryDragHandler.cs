using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sojartsa.Inventory.UI;

namespace Sojartsa.Inventory.UI
{
    /// <summary>
    /// Obsługuje wizualne przeciąganie przedmiotów (Drag & Drop).
    /// </summary>
    public class InventoryDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Referencje")]
        [SerializeField] private InventorySlotUI slotUI;
        
        private Canvas _mainCanvas;
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        
        private static GameObject _dragIconObject;
        private static Image _dragIconImage;
        private static InventorySlotUI _sourceSlot;

        private void Awake()
        {
            slotUI = GetComponent<InventorySlotUI>();
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _mainCanvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log($"[DRAG] OnBeginDrag na slocie: {gameObject.name}");
            // KLUCZOWA BLOKADA: Nie pozwalamy zacząć przeciągania, jeśli slot jest pustY!
            if (slotUI == null || slotUI.IsEmpty()) 
            {
                eventData.pointerDrag = null;
                return;
            }

            _sourceSlot = slotUI;

            // --- WIZUALNE UKRYWANIE SAMEJ IKONKI ---
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = false; // Żeby myszka widziała co jest POD slotem
            }
            
            if (slotUI != null)
                slotUI.SetVisualsActive(false);

            // Tworzymy ikonę "ducha" jeśli nie istnieje
            if (_dragIconObject == null)
            {
                _dragIconObject = new GameObject("DragIcon");
                _dragIconImage = _dragIconObject.AddComponent<Image>();
                _dragIconImage.raycastTarget = false; // Żeby nie blokować upuszczania
            }

            // ZAWSZE przypinamy do aktualnego canvasu (zapobiega to znikaniu, jeśli stary canvas został wyłączony)
            // i dajemy na samą górę, żeby ikonka była nad wszystkim innym!
            _dragIconObject.transform.SetParent(_mainCanvas.transform, false);
            _dragIconObject.transform.SetAsLastSibling();
            
            // Ustawiamy wielkość ducha na taką samą jak slot
            RectTransform rt = _dragIconObject.GetComponent<RectTransform>();
            rt.sizeDelta = _rectTransform.sizeDelta;

            _dragIconImage.sprite = slotUI.GetIcon();
            _dragIconImage.enabled = true;
            _dragIconObject.SetActive(true);
            
            // Lekko przezroczysty duch
            Color c = _dragIconImage.color;
            c.a = 0.6f;
            _dragIconImage.color = c;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Debug.Log("[DRAG] OnDrag");
            // Duch podąża za kursorem
            if (_dragIconObject != null) _dragIconObject.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Debug.Log("[DRAG] OnEndDrag wywołane.");
            // Przywracamy raycasts
            if (_canvasGroup != null)
                _canvasGroup.blocksRaycasts = true;

            if (_dragIconObject == null) return;
            _dragIconObject.SetActive(false);

            // Sprawdzamy co jest pod kursorem
            GameObject over = eventData.pointerCurrentRaycast.gameObject;
            Debug.Log($"[DRAG] Pod myszką po upuszczeniu: {(over != null ? over.name : "NIC")}");
            
            if (over != null)
            {
                InventorySlotUI targetSlot = over.GetComponentInParent<InventorySlotUI>();
                if (targetSlot != null && targetSlot != _sourceSlot)
                {
                    Debug.Log($"[DRAG] Wykryto poprawny target: {targetSlot.gameObject.name}. Robię Swapa!");
                    ExecuteSwap(_sourceSlot, targetSlot);
                }
            }

            // --- ZAWSZE przywracamy wizualia oryginalnego slotu ---
            if (_sourceSlot != null)
                _sourceSlot.SetVisualsActive(true);

            // W nowym systemie Eventów, DragHandler nie musi nikogo szukać.
            // Po prostu zamieniamy dane, a plecak sam powiadomi UI i EquipmentManager.
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        private void ExecuteSwap(InventorySlotUI a, InventorySlotUI b)
        {
            InventoryDisplay displayA = a.GetComponentInParent<InventoryDisplay>();
            InventoryDisplay displayB = b.GetComponentInParent<InventoryDisplay>();
            
            if (displayA == null || displayB == null) return;

            bool isAEquip, isBEquip;
            int indexA = displayA.GetSlotIndex(a, out isAEquip);
            int indexB = displayB.GetSlotIndex(b, out isBEquip);

            if (indexA != -1 && indexB != -1)
            {
                InventoryController inv = InventoryController.Instance;
                if (inv == null) return;

                // Pobieramy prawidłowe listy dla obu slotów
                System.Collections.Generic.List<InventorySlot> listA = isAEquip ? inv.equipmentSlots : (displayA.IsBagMode() ? inv.bagSlots : inv.inventorySlots);
                System.Collections.Generic.List<InventorySlot> listB = isBEquip ? inv.equipmentSlots : (displayB.IsBagMode() ? inv.bagSlots : inv.inventorySlots);

                // Uniwersalna metoda zamiany (działa też wewnątrz tej samej listy)
                inv.SwapSlotsBetweenLists(listA, indexA, listB, indexB);
            }
        }
    }
}
