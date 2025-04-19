using Content.Shared.Preferences;

namespace Content.Server.VoiceMask;

public sealed partial class VoiceMaskComponent
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string VoiceId = HumanoidCharacterProfile.DefaultVoice;
}