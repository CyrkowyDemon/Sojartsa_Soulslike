using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace Sojartsa.UI
{
    /// <summary>
    /// Globalny system okienek potwierdzenia (Are you sure?).
    /// Użycie: ConfirmationDialog.Instance.Show("Tytuł", "Wiadomość", () => { akcja(); });
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ConfirmationDialog : MonoBehaviour
    {
        public static ConfirmationDialog Instance { get; private set; }

        [Header("Referencje UI")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Transform windowTransform; // Środkowy panel (okienko), który będzie "wyskakiwał"
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        [Header("Ustawienia Animacji")]
        [SerializeField] private float animationDuration = 0.25f;

        private Action _onConfirm;
        private Action _onCancel;
        private GameObject _previousSelectedObject;

        private void Awake()
        {
            // Auto-fetch na wypadek gdy nie podpięto CanvasGroup w Inspektorze
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            
            // Zabezpieczenie FromSoftware-style: jeśli mamy Canvas, musimy mieć GraphicRaycaster, inaczej myszka nie działa!
            Canvas myCanvas = GetComponent<Canvas>();
            if (myCanvas != null)
            {
                GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    raycaster = gameObject.AddComponent<GraphicRaycaster>();
                    Debug.LogWarning("[ConfirmationDialog] Wykryto brak GraphicRaycaster na Canvas! Dodałem go automatycznie na starcie, aby przyciski reagowały na myszkę.");
                }
            }

            // Singleton pattern - upewniamy się, że w grze jest tylko jedno okienko
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Podpinamy przyciski pod metody
            confirmButton.onClick.AddListener(OnConfirmClicked);
            cancelButton.onClick.AddListener(OnCancelClicked);

            // Wyłączamy okienko na start (żeby nie wisiało na ekranie)
            HideInstantly();
        }

        /// <summary>
        /// Otwiera okienko potwierdzenia.
        /// </summary>
        /// <param name="title">Tytuł (np. UWAGA)</param>
        /// <param name="message">Treść pytania</param>
        /// <param name="onConfirm">Kod, który wykona się po wciśnięciu TAK</param>
        /// <param name="onCancel">Opcjonalny kod po wciśnięciu NIE</param>
        public void Show(string title, string message, Action onConfirm, Action onCancel = null)
        {
            _onConfirm = onConfirm;
            _onCancel = onCancel;

            titleText.text = title;
            messageText.text = message;

            if (EventSystem.current != null)
                _previousSelectedObject = EventSystem.current.currentSelectedGameObject;

            gameObject.SetActive(true);

            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, animationDuration).SetUpdate(true);

            windowTransform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            windowTransform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack).SetUpdate(true);

            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            Debug.Log($"[ConfirmationDialog] Show() wywołane! CanvasGroup interactable={canvasGroup.interactable}, blocksRaycasts={canvasGroup.blocksRaycasts}");
            Debug.Log($"[ConfirmationDialog] confirmButton={confirmButton}, cancelButton={cancelButton}");

            SelectButton(cancelButton.gameObject);
        }

        private void OnConfirmClicked()
        {
            Debug.Log("[ConfirmationDialog] Kliknięto TAK!");
            _onConfirm?.Invoke();
            Hide();
        }

        public void Cancel()
        {
            OnCancelClicked();
        }

        private void OnCancelClicked()
        {
            Debug.Log("[ConfirmationDialog] Kliknięto NIE!");
            _onCancel?.Invoke();
            Hide();
        }

        private void Hide()
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Animacja znikania
            windowTransform.DOScale(new Vector3(0.8f, 0.8f, 0.8f), animationDuration).SetEase(Ease.InBack).SetUpdate(true);
            canvasGroup.DOFade(0f, animationDuration).SetUpdate(true).OnComplete(() =>
            {
                gameObject.SetActive(false);
                
                // Magia: przywracamy focus tam, gdzie był przed otwarciem okienka!
                if (EventSystem.current != null && _previousSelectedObject != null)
                {
                    EventSystem.current.SetSelectedGameObject(_previousSelectedObject);
                }
            });
        }

        private void HideInstantly()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        private void SelectButton(GameObject button)
        {
            if (button == null || !gameObject.activeInHierarchy) return;
            StartCoroutine(SelectRoutine(button));
        }

        private System.Collections.IEnumerator SelectRoutine(GameObject button)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            yield return new WaitForEndOfFrame();
            if (EventSystem.current != null)
            {
                if (EventSystem.current.currentSelectedGameObject == null)
                {
                    EventSystem.current.SetSelectedGameObject(button);
                }
            }
        }
    }
}
