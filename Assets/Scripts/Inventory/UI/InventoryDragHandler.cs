using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sojartsa.UI.DragDrop;

namespace Sojartsa.Inventory.UI
{
    /// <summary>
    /// Uniwersalny handler Drag & Drop.
    /// NIE ZNA żadnego konkretnego systemu – operuje wyłącznie na interfejsach.
    /// 
    /// Obsługuje:
    /// - LPM (Lewy Przycisk): Podnosi cały stack
    /// - PPM (Prawy Przycisk): Podnosi połowę stacka (split)
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class InventoryDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private Canvas _mainCanvas;
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        
        private static GameObject _dragIconObject;
        private static Image _dragIconImage;
        
        private IDragSource _mySource;
        private static IDragSource _currentSource;
        private static ItemPayload _currentPayload;

        private void Awake()
        {
            _mySource = GetComponent<IDragSource>();
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _mainCanvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_mySource == null || !_mySource.CanDrag())
            {
                eventData.pointerDrag = null;
                return;
            }

            _currentSource = _mySource;
            _currentPayload = _mySource.GetTransferPayload();
            _currentSource.OnDragStarted();

            // Wyłączamy kolizje, żeby myszka widziała co jest POD slotem
            if (_canvasGroup != null)
                _canvasGroup.blocksRaycasts = false;

            // Tworzymy ikonę "ducha"
            if (_dragIconObject == null)
            {
                _dragIconObject = new GameObject("DragIcon");
                _dragIconImage = _dragIconObject.AddComponent<Image>();
                _dragIconImage.raycastTarget = false;
            }

            _dragIconObject.transform.SetParent(_mainCanvas.transform, false);
            _dragIconObject.transform.SetAsLastSibling();
            
            RectTransform rt = _dragIconObject.GetComponent<RectTransform>();
            rt.sizeDelta = _rectTransform.sizeDelta;

            _dragIconImage.sprite = _currentSource.GetDragIcon();
            _dragIconImage.enabled = true;
            _dragIconObject.SetActive(true);
            
            Color c = _dragIconImage.color;
            c.a = 0.6f;
            _dragIconImage.color = c;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragIconObject != null)
                _dragIconObject.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Przywracamy kolizje
            if (_canvasGroup != null)
                _canvasGroup.blocksRaycasts = true;

            if (_dragIconObject != null)
                _dragIconObject.SetActive(false);

            // Szukamy celu pod kursorem
            GameObject over = eventData.pointerCurrentRaycast.gameObject;
            Debug.Log($"[DRAG] Puszczono na: {(over != null ? over.name : "NULL")}");
            
            if (over != null && _currentPayload != null)
            {
                IDropTarget target = over.GetComponentInParent<IDropTarget>();
                if (target == null)
                {
                    Debug.Log($"[DRAG] Obiekt {over.name} nie ma w rodzicach IDropTarget!");
                }
                else if (ItemTransferManager.Instance == null)
                {
                    Debug.Log("[DRAG] BŁĄD: ItemTransferManager.Instance jest NULL!");
                }
                else
                {
                    ItemPayload targetPayload = target.GetTargetPayload();
                    bool success = ItemTransferManager.Instance.Execute(_currentPayload, targetPayload);
                    Debug.Log($"[DRAG] Transfer z {_currentPayload.Source} na {targetPayload.Source}. Sukces: {success}");
                    
                    if (success)
                        target.OnDropCompleted();
                }
            }

            // Informujemy źródło, że przeciąganie się skończyło
            if (_currentSource != null)
            {
                _currentSource.OnDragEnded();
                _currentSource = null;
                _currentPayload = null;
            }
            
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        /// <summary>
        /// PPM na slocie = dzielenie stacka (split).
        /// Podnosi połowę stacka i kładzie na pierwszy wolny slot.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right) return;
            if (_mySource == null || !_mySource.CanDrag()) return;

            ItemPayload payload = _mySource.GetTransferPayload();
            if (payload == null || payload.IsEmpty || payload.Amount <= 1) return;

            // Dzielimy stack na pół
            if (ItemTransferManager.Instance != null)
            {
                ItemTransferManager.Instance.SplitStack(payload);
            }
        }
    }
}
