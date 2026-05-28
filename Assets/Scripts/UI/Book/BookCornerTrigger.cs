using UnityEngine;
using UnityEngine.EventSystems;

namespace Sojartsa.UI
{
    /// <summary>
    /// Skrypt pomocniczy do umieszczenia na przezroczystych przyciskach w dolnych rogach Książki.
    /// Wykrywa najechanie myszką (hover) oraz kliknięcie, a następnie informuje UniversalBook o chęci zgięcia/przewrócenia kartki.
    /// </summary>
    public class BookCornerTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Referencje")]
        [Tooltip("Główny skrypt Książki (jeśli pusty, spróbuje go znaleźć w rodzicach)")]
        [SerializeField] private UniversalBook book;

        [Header("Konfiguracja")]
        [Tooltip("Czy to jest prawy dolny róg (do przodu)? Odznacz dla lewego dolnego rogu (w tył)")]
        [SerializeField] private bool isRightPage;

        private void Awake()
        {
            if (book == null)
            {
                book = GetComponentInParent<UniversalBook>();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (book != null)
            {
                book.OnCornerHover(isRightPage, true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (book != null)
            {
                book.OnCornerHover(isRightPage, false);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (book != null)
            {
                book.OnCornerClicked(isRightPage);
            }
        }
    }
}
