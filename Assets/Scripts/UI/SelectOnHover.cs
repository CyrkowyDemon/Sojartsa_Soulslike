using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sojartsa.UI
{
    /// <summary>
    /// Wymusza zaznaczenie (Focus) w EventSystemie, gdy myszka najedzie na obiekt.
    /// Rozwiązuje problem podwójnego podświetlenia przy grze na Myszce + Padzie.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class SelectOnHover : MonoBehaviour, IPointerEnterHandler, IDeselectHandler
    {
        private Selectable _selectable;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Upewniamy się, że nie wymusimy zaznaczenia na zablokowanym (szarym) przycisku
            if (_selectable != null && _selectable.interactable)
            {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }
        }

        public void OnDeselect(BaseEventData eventData)
        {
            // Kiedy Unity próbuje nas odznaczyć (bo np. zjechaliśmy myszką i kliknęliśmy w tło)
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(ReacquireFocus());
            }
        }

        private IEnumerator ReacquireFocus()
        {
            // Czekamy 1 klatkę, żeby upewnić się, co Unity zrobiło
            yield return null;

            // Jeśli po tej 1 klatce w systemie nie ma ŻADNEGO zaznaczonego przycisku
            // to znaczy, że gracz kliknął w tło. Wtedy siłą przywracamy focus na ten przycisk!
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            {
                if (_selectable != null && _selectable.interactable)
                {
                    EventSystem.current.SetSelectedGameObject(gameObject);
                }
            }
        }
    }
}
