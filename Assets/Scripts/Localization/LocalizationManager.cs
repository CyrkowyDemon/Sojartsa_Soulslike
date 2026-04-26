using System.Collections.Generic;
using UnityEngine;

namespace Sojartsa.Localization
{
    public enum Language { PL, EN }

    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        [Header("Ustawienia")]
        public Language currentLanguage = Language.PL;

        // Prosty słownik: Klucz -> Tekst
        private Dictionary<string, string> _localizedStrings = new Dictionary<string, string>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadDatabase();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void LoadDatabase()
        {
            // TUTAJ w przyszłości będziemy wczytywać dane z CSV lub JSON.
            // Na razie wpiszemy kilka testowych kluczy na sztywno, żebyś widział jak to działa.
            _localizedStrings.Clear();

            if (currentLanguage == Language.PL)
            {
                _localizedStrings.Add("item_sword_01_name", "Krótki Miecz");
                _localizedStrings.Add("item_sword_01_desc", "Stary, zardzewiały miecz, który pamięta lepsze czasy.");
                _localizedStrings.Add("item_potion_hp_name", "Mikstura Zdrowia");
                _localizedStrings.Add("item_potion_hp_desc", "Przywraca 50 punktów życia.");
                _localizedStrings.Add("ui_price", "Cena: ");
            }
            else
            {
                _localizedStrings.Add("item_sword_01_name", "Short Sword");
                _localizedStrings.Add("item_sword_01_desc", "An old, rusty sword that has seen better days.");
                _localizedStrings.Add("item_potion_hp_name", "Health Potion");
                _localizedStrings.Add("item_potion_hp_desc", "Restores 50 HP.");
                _localizedStrings.Add("ui_price", "Price: ");
            }
        }

        public string GetText(string key)
        {
            if (string.IsNullOrEmpty(key)) return "[EMPTY KEY]";
            
            if (_localizedStrings.TryGetValue(key, out string value))
            {
                return value;
            }

            return $"[MISSING: {key}]";
        }

        public void ChangeLanguage(Language newLang)
        {
            currentLanguage = newLang;
            LoadDatabase();
            // Tutaj można wywołać event, żeby wszystkie UI się odświeżyły
        }
    }
}
