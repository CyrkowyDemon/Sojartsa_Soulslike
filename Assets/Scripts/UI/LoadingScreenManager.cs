using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System;

namespace Sojartsa.UI
{
    /// <summary>
    /// Menedżer ekranu ładowania (FromSoftware-style).
    /// Zarządza płynnym włączaniem i wyłączaniem panelu ładowania w tle za pomocą CanvasGroup i DOTween.
    /// Bezpieczny dla wątków i Coroutines – obiekt jest stale aktywny w hierarchii, 
    /// a ukrywanie polega wyłącznie na sterowaniu przezroczystością (alpha = 0) oraz blokadą interakcji.
    /// </summary>
    public class LoadingScreenManager : MonoBehaviour
    {
        public static LoadingScreenManager Instance { get; private set; }

        [Header("UI Elementy")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeDuration = 0.5f;

        private void Awake()
        {
            // Singleton: Dbamy, aby w grze był tylko jeden nieśmiertelny menedżer
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

            // Auto-fetch jeśli zapomniano przypisać w Inspektorze
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            
            HideInstantly();
        }

        /// <summary>
        /// Płynne włączenie ekranu ładowania (zmiana przezroczystości i włączenie raycastów).
        /// </summary>
        public void Show()
        {
            if (canvasGroup == null) return;
            
            // Odblokowujemy kliknięcia i interakcję, by gracz nie przeklikał niczego pod spodem
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeDuration).SetUpdate(true); // SetUpdate(true) sprawia, że działa nawet przy Time.timeScale = 0
        }

        /// <summary>
        /// Płynne wyłączenie ekranu ładowania (wyzerowanie przezroczystości i zablokowanie raycastów).
        /// </summary>
        public void Hide()
        {
            if (canvasGroup == null) return;

            canvasGroup.DOFade(0f, fadeDuration).SetUpdate(true).OnComplete(() =>
            {
                // Po schowaniu całkowicie wyłączamy interakcję, by gracz mógł grać
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            });
        }

        /// <summary>
        /// Natychmiastowe ukrycie na start gry (przezroczystość 0, interakcje wyłączone).
        /// </summary>
        public void HideInstantly()
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// Globalna funkcja do wczytywania scen z pełną sekwencją przejścia.
        /// Używana przy starcie nowej gry i wczytywaniu zapisu z poziomu Menu.
        /// </summary>
        public void LoadScene(string sceneName, Action onComplete = null)
        {
            StartCoroutine(LoadSceneRoutine(sceneName, onComplete));
        }

        private System.Collections.IEnumerator LoadSceneRoutine(string sceneName, Action onComplete)
        {
            // 1. FADE DO CZERNI (Ściemniamy grę)
            if (FadeManager.Instance != null)
            {
                FadeManager.Instance.FadeOut(fadeDuration);
                yield return new WaitForSecondsRealtime(fadeDuration);
                FadeManager.Instance.SetAlpha(1f); // Upewniamy się, że tło pod spodem jest w 100% czarne
            }

            // 2. FADE OD CZERNI DO EKRANU ŁADOWANIA (Zaczynamy stoper rzeczywisty)
            float startTime = Time.unscaledTime;
            Show();
            yield return new WaitForSecondsRealtime(fadeDuration); // Czekamy aż ekran ładowania się rozjaśni

            // 3. WCZYTYWANIE ASYNCHRONICZNE (Komputer ładuje dane w tle)
            Debug.Log($"[LoadingScreen] Ładuję scenę: {sceneName}...");
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // Wykonujemy dodatkowe akcje po załadowaniu
            onComplete?.Invoke();

            // Wymuszamy minimalny czas trwania (np. 2 sekundy od startu ładowania w unscaledTime)
            float elapsed = Time.unscaledTime - startTime;
            if (elapsed < 2.0f)
            {
                yield return new WaitForSecondsRealtime(2.0f - elapsed);
            }

            // 4. FADE OD EKRANU ŁADOWANIA DO CZERNI (Ekran ładowania gaśnie, pod spodem jest czarno)
            Hide();
            yield return new WaitForSecondsRealtime(fadeDuration); // Czekamy na zgaśnięcie

            // 5. UNFADE CZERNI (Rozjaśniamy nową scenę)
            if (FadeManager.Instance != null)
            {
                FadeManager.Instance.FadeIn(fadeDuration);
            }
        }
    }
}
