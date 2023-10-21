public class GoogleTtsVoice
{
    public string LanguageCode { get; set; }
    public string LanguageName { get; set; }

    public GoogleTtsVoice(string languageCode, string languageName)
    {
        LanguageCode = languageCode;
        LanguageName = languageName;
    }
}