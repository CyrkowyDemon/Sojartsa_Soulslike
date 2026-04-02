using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AutoScrollGamepad : MonoBehaviour
{
    [Header("Podepnij tu swój Scroll View")]
    public ScrollRect scrollRect;
    
    private GameObject lastSelected;

    void Update()
    {
        // 1. Sprawdzamy, co jest aktualnie podświetlone (wybrane padem)
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        
        // 2. Jeśli nic nie jest wybrane lub zaznaczenie się nie zmieniło od ostatniej klatki - przerywamy
        if (selected == null || selected == lastSelected) return;

        // 3. Sprawdzamy, czy wybrany przycisk w ogóle znajduje się wewnątrz tej konkretnej listy
        if (!selected.transform.IsChildOf(scrollRect.content)) return;

        // 4. Aktualizujemy ostatnio wybrany element i przesuwamy listę
        lastSelected = selected;
        SnapTo(selected.GetComponent<RectTransform>());
    }

    private void SnapTo(RectTransform target)
    {
        // Wymuszamy aktualizację UI, żeby uniknąć błędów z pozycjonowaniem
        Canvas.ForceUpdateCanvases();

        // Pobieramy pozycję okna widoku (Viewport) oraz pozycję przycisku, na którym stoimy
        Vector2 viewportLocalPosition = scrollRect.viewport.localPosition;
        Vector2 targetLocalPosition = target.localPosition;

        // Magia: Przesuwamy cały Content (zawartość listy) w pionie w przeciwnym kierunku do przycisku
        scrollRect.content.localPosition = new Vector2(
            scrollRect.content.localPosition.x,
            0 - (viewportLocalPosition.y + targetLocalPosition.y)
        );
    }
}