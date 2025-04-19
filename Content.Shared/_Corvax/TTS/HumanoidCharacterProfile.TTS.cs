using Content.Shared._Corvax.TTS;
using Content.Shared.Humanoid;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    public const string DefaultVoice = "dude";

    [DataField]
    public string Voice { get; set; } = DefaultVoice;

    // SHOULD BE NOT PUBLIC, BUT....
    public static bool CanHaveVoice(TTSVoicePrototype voice, Sex sex)
    {
        return voice.RoundStart && sex == Sex.Unsexed || (voice.Sex == sex || voice.Sex == Sex.Unsexed);
    }

    public HumanoidCharacterProfile WithVoice(string voice)
    {
        return new(this) { Voice = voice };
    }
}