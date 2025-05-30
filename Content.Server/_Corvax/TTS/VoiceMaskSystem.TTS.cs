using Content.Server._Corvax.TTS;
using Content.Shared._Corvax.TTS;
using Content.Shared.VoiceMask;
using static Content.Server._Corvax.TTS.TTSSystem;

namespace Content.Server.VoiceMask;

public partial class VoiceMaskSystem
{
    private void InitializeTTS()
    {
        SubscribeLocalEvent<VoiceMaskComponent, TransformSpeakerVoiceEvent>(OnSpeakerVoiceTransform);
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskChangeVoiceMessage>(OnChangeVoice);
    }

    private void OnSpeakerVoiceTransform(EntityUid uid, VoiceMaskComponent component, TransformSpeakerVoiceEvent args)
    {
        args.VoiceId = component.VoiceId;
    }

    private void OnChangeVoice(Entity<VoiceMaskComponent> entity, ref VoiceMaskChangeVoiceMessage msg)
    {
        if (msg.Voice is { } id && !_proto.HasIndex<TTSVoicePrototype>(id))
            return;

        entity.Comp.VoiceId = msg.Voice;

        _popupSystem.PopupEntity(Loc.GetString("voice-mask-voice-popup-success"), entity);

        UpdateUI(entity);
    }
}
