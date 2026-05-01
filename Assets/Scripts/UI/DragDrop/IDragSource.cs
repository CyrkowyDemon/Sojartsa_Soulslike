using UnityEngine;

namespace Sojartsa.UI.DragDrop
{
    /// <summary>
    /// Interfejs dla wszystkiego, co można złapać myszką i przeciągnąć.
    /// Każdy element UI (slot plecaka, oferta sklepu, pole stołu) implementuje to samo.
    /// </summary>
    public interface IDragSource
    {
        /// <summary>Czy w ogóle da się to podnieść? (np. pusty slot = false)</summary>
        bool CanDrag();

        /// <summary>Ikona wyświetlana pod kursorem podczas przeciągania.</summary>
        Sprite GetDragIcon();

        /// <summary>Zwraca uniwersalną paczkę opisującą CO i SKĄD przenosimy.</summary>
        ItemPayload GetTransferPayload();

        /// <summary>Wywoływane na początku drag (np. ukryj ikonę w slocie).</summary>
        void OnDragStarted();

        /// <summary>Wywoływane na końcu drag (np. przywróć ikonę).</summary>
        void OnDragEnded();
    }
}
