using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class LocaleSelector : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown languageDropdown;
    private bool active = false;
    private IEnumerator Start()
    {
        // Wait for localization to initialize to know the current language
        yield return LocalizationSettings.InitializationOperation;

        // Find the index of the currently active locale in the available list
        int currentLocaleIndex = 0;
        var selectedLocale = LocalizationSettings.SelectedLocale;
        for (int i = 0; i < LocalizationSettings.AvailableLocales.Locales.Count; i++)
        {
            if (LocalizationSettings.AvailableLocales.Locales[i] == selectedLocale)
            {
                currentLocaleIndex = i;
                break;
            }
        }
        // Set the dropdown to the correct index without triggering the Change event
        if (languageDropdown != null)
        {
            languageDropdown.SetValueWithoutNotify(currentLocaleIndex);
        }
    }
    public void ChanageLocal(int localeID)
    {
        if (active) return;
        StartCoroutine(SetLocale(localeID));
    }
    IEnumerator SetLocale(int _localeID)
    {
        active = true;
        yield return LocalizationSettings.InitializationOperation;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[_localeID];
        PlayerPrefs.SetString("selected-locale", LocalizationSettings.SelectedLocale.Identifier.Code);
        PlayerPrefs.Save();
        active = false;
    }
}
