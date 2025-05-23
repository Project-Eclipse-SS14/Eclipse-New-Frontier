using Robust.Shared.Audio.Systems;

namespace Content.Shared._Eclipse.Audio.BoomBox;

public abstract class SharedBoomBoxSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = default!;

    /// <summary>
    /// Sets the audio params volume for an entity.
    /// </summary>
    public void SetGain(EntityUid? entity, float value, BoomBoxAudioComponent? component = null)
    {
        if (entity == null || !Resolve(entity.Value, ref component))
            return;

        var volume = SharedAudioSystem.GainToVolume(value);
        SetVolume(entity, volume, component);
    }

    /// <summary>
    /// Sets the audio params volume for an entity.
    /// </summary>
    public void SetVolume(EntityUid? entity, float value, BoomBoxAudioComponent? component = null)
    {
        if (entity == null || !Resolve(entity.Value, ref component))
            return;

        if (component.Params.Volume.Equals(value))
            return;

        // Not a log error for now because if something has a negative infinity volume (i.e. 0 gain) then subtracting from it can
        // easily cause this and making callers deal with it everywhere is quite annoying.
        if (float.IsNaN(value))
        {
            value = float.NegativeInfinity;
        }

        component.Params.Volume = value;
        component.Volume = value;
        DirtyField(entity.Value, component, nameof(BoomBoxAudioComponent.Params));
    }

    public record BoomBoxAudioMetadata(TimeSpan Length, int ChannelCount, string? Title = null, string? Artist = null);
}