public class EdgeTtsVoice
{
    public string VoiceName { get; set; }
    public string VoiceCompleteName { get; set; }
    public string VoiceGender { get; set; }
    public string VoiceLanguage { get; set; }

    public EdgeTtsVoice(string voiceName, string voiceCompleteName, string voiceGender, string voiceLanguage)
    {
        VoiceName = voiceName;
        VoiceCompleteName = voiceCompleteName;
        VoiceGender = voiceGender;
        VoiceLanguage = voiceLanguage;
    }
}