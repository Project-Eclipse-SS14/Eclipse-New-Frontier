
using Content.Shared._Corvax.TTS;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;
public sealed partial class HumanoidAppearanceComponent
{
    // Corvax-TTS-Start
    /// <summary>
    ///     Current voice. Used for correct cloning.
    /// </summary>
    [DataField("voice")]
    public ProtoId<TTSVoicePrototype> Voice { get; set; } = HumanoidCharacterProfile.DefaultVoice;
    // Corvax-TTS-End
}