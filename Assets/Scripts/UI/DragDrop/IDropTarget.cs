namespace Sojartsa.UI.DragDrop
{
    /// <summary>
    /// Interfejs dla wszystkiego, na co można upuścić przeciągany obiekt.
    /// Cel nie musi wiedzieć KTO rzucił – dostaje tylko uniwersalną paczkę (ItemPayload).
    /// </summary>
    public interface IDropTarget
    {
        /// <summary>
        /// Zwraca informację o SOBIE (typ celu + indeks slotu), żeby ItemTransferManager
        /// wiedział, gdzie gracz chce coś upuścić.
        /// </summary>
        ItemPayload GetTargetPayload();

        /// <summary>
        /// Wywoływane PO udanym transferze, żeby cel mógł odświeżyć swój wygląd.
        /// </summary>
        void OnDropCompleted();
    }
}
