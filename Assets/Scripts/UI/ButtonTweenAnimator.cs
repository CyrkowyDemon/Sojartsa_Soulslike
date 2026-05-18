using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace Sojartsa.UI
{
    /// <summary>
    /// Obsługuje płynne pulsowanie Alpha na Image przy użyciu DOTween.
    /// Wymaga, aby na komponencie Button ustawić Transition na "None".
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ButtonTweenAnimator : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        [Header("Ustawienia Przezroczystości (Alpha)")]
        [Range(0f, 1f)] public float unselectedAlpha = 0f; // Niezaznaczony (przezroczysty)
        [Range(0f, 1f)] public float thresholdA = 1f;      // Maksymalne świecenie przy wjechaniu
        [Range(0f, 1f)] public float thresholdB = 0.5f;    // Minimalne świecenie podczas pulsowania

        [Header("Ustawienia Czasu")]
        public float fadeToSelectedTime = 0.2f;   // Jak szybko pojawia się po najechaniu
        public float pingPongLoopTime = 0.8f;     // Czas przejścia A -> B (częstotliwość bicia serca)
        public float fadeToDeselectedTime = 0.2f; // Jak szybko znika po odznaczeniu

        private Image _image;
        private Tween _currentTween;

        private void Awake()
        {
            _image = GetComponent<Image>();
            
            // Ustawiamy początkowy stan bez animacji
            SetAlpha(unselectedAlpha);
        }

        public void OnSelect(BaseEventData eventData)
        {
            // Zabijamy poprzednią animację (żeby uniknąć błędów przy szybkim machaniu myszką)
            _currentTween?.Kill();

            // Krok 1: Płynnie podnosimy do progu A
            _currentTween = _image.DOFade(thresholdA, fadeToSelectedTime)
                .SetUpdate(true) // SetUpdate(true) sprawia, że animacja działa nawet w menu pauzy (gdy czas stoi)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => 
                {
                    // Krok 2: Po dotarciu do A, odpalamy nieskończone pulsowanie między A i B
                    _currentTween = _image.DOFade(thresholdB, pingPongLoopTime)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetUpdate(true)
                        .SetEase(Ease.InOutSine); // InOutSine daje ładne, miękkie pulsowanie ("oddychanie")
                });
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _currentTween?.Kill();
            
            // Płynnie zgaszamy do początkowej przezroczystości (0)
            _currentTween = _image.DOFade(unselectedAlpha, fadeToDeselectedTime)
                .SetUpdate(true)
                .SetEase(Ease.OutQuad);
        }

        private void SetAlpha(float alpha)
        {
            if (_image == null) return;
            Color c = _image.color;
            c.a = alpha;
            _image.color = c;
        }

        private void OnDisable()
        {
            // Zabezpieczenie przed dziwnymi błędami, gdy zmienimy scenę lub wyłączymy panel
            _currentTween?.Kill();
            SetAlpha(unselectedAlpha);
        }
    }
}
