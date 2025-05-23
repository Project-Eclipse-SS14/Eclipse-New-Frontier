using System.IO;
using Content.Shared._Eclipse.Audio.BoomBox;
using Content.Shared._Eclipse.CCVar;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Eclipse.Audio.BoomBox;

public sealed partial class BoomBoxSystem : SharedBoomBoxSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IAudioManager _audioManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IClientNetManager _networkManager = default!;

    private float _audioEndBuffer;

    public override void Initialize()
    {
        base.Initialize();

        _networkManager.RegisterNetMessage<MsgBoomBoxSong>();

        SubscribeLocalEvent<BoomBoxAudioComponent, ComponentStartup>(OnAudioStartup);
        SubscribeLocalEvent<BoomBoxAudioComponent, ComponentShutdown>(OnAudioShutdown);
        SubscribeLocalEvent<BoomBoxAudioComponent, EntityPausedEvent>(OnAudioPaused);
        SubscribeLocalEvent<BoomBoxAudioComponent, AfterAutoHandleStateEvent>(OnAudioState);

        _cfg.OnValueChanged(EclipseCCVars.BoomBoxAudioEndBuffer, (value) => _audioEndBuffer = value, true);
    }

    private void OnAudioStartup(EntityUid uid, BoomBoxAudioComponent component, ComponentStartup args)
    {
        if (!_timing.ApplyingState && !_timing.IsFirstTimePredicted)
        {
            return;
        }

        // Source has already been set
        if (component.Loaded)
        {
            return;
        }

        SetupSource((uid, component));
        component.Loaded = true;
    }

    private void SetupSource(Entity<BoomBoxAudioComponent> entity, TimeSpan? length = null)
    {
        var component = entity.Comp;
        var stream = _audioManager.LoadAudioOggVorbis(new MemoryStream(component.FileBytes));
        length ??= stream.Length;

        // If audio came into range then start playback at the correct position.
        var offset = ((entity.Comp.PauseTime ?? _timing.CurTime) - component.AudioStart).TotalSeconds;

        if (TryAudioLimit())
        {
            var newSource = _audioManager.CreateAudioSource(stream);

            if (newSource == null)
            {
                Log.Error($"Error creating audio source for boombox {entity.Owner.Id}");
                DebugTools.Assert(false);
            }
            else
            {
                component.Source = newSource;
            }
        }

        // Need to set all initial data for first frame.
        ApplyAudioParams(component.Params, component);
        component.Source.Global = component.Global;

        // Don't play until first frame so occlusion etc. are correct.
        component.Gain = 0f;

        // If the offset < buffer than just play it from the start.
        if (offset < SharedAudioSystem.AudioDespawnBuffer)
        {
            offset = 0;
        }
        // Not enough audio to play
        else if (offset > length.Value.TotalSeconds - _audioEndBuffer)
        {
            component.StopPlaying();
            return;
        }

        if (offset > 0)
        {
            component.PlaybackPosition = (float)offset;
        }
    }

    private void OnAudioShutdown(EntityUid uid, BoomBoxAudioComponent component, ComponentShutdown args)
    {
        // Breaks with prediction?
        component.Source.Dispose();

        RemoveAudioLimit();
    }

    private void OnAudioPaused(EntityUid uid, BoomBoxAudioComponent component, ref EntityPausedEvent args)
    {
        component.Pause();
    }

    private void OnAudioState(Entity<BoomBoxAudioComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        var component = entity.Comp;

        if (component.LifeStage < ComponentLifeStage.Initialized)
            return;

        ApplyAudioParams(component.Params, component);
        component.Source.Global = component.Global;

        component.Source.SetAuxiliary(null);

        switch (component.State)
        {
            case AudioState.Playing:
                component.StartPlaying();
                break;
            case AudioState.Paused:
                component.Pause();
                break;
            case AudioState.Stopped:
                component.StopPlaying();
                component.PlaybackPosition = 0f;
                return;
        }

        // If playback position changed then update it.
        var position = (float)((entity.Comp.PauseTime ?? _timing.CurTime) - entity.Comp.AudioStart).TotalSeconds;
        var currentPosition = entity.Comp.Source.PlaybackPosition;
        var diff = Math.Abs(position - currentPosition);

        // Don't try to set the audio too far ahead.
        if (entity.Comp.FileBytes is not null)
        {
            var length = _audioManager.LoadAudioOggVorbis(new MemoryStream(entity.Comp.FileBytes)).Length;
            if (position > length.TotalSeconds - _audioEndBuffer)
            {
                entity.Comp.StopPlaying();
                return;
            }
        }

        // If the difference is minor then we'll just keep playing it.
        if (diff > 0.1f)
        {
            entity.Comp.PlaybackPosition = position;
        }
    }

    /// <summary>
    /// Applies the audioparams to the underlying audio source.
    /// </summary>
    private void ApplyAudioParams(AudioParams audioParams, IAudioSource source)
    {
        source.Pitch = audioParams.Pitch;
        source.Volume = audioParams.Volume;
        source.RolloffFactor = audioParams.RolloffFactor;
        source.MaxDistance = _audio.GetAudioDistance(audioParams.MaxDistance);
        source.ReferenceDistance = _audio.GetAudioDistance(audioParams.ReferenceDistance);
        source.Looping = audioParams.Loop;
    }

    /// <summary>
    /// Gets the timespan of the specified audio.
    /// </summary>
    public TimeSpan GetAudioLength(byte[] fileBytes)
    {
        return _audioManager.LoadAudioOggVorbis(new MemoryStream(fileBytes)).Length;
    }

    public bool IsPlaying(EntityUid? stream, BoomBoxAudioComponent? component = null)
    {
        if (stream == null || !Resolve(stream.Value, ref component, false))
            return false;

        return component.State == AudioState.Playing;
    }

    public BoomBoxAudioMetadata GetAudioMetadata(byte[] fileBytes)
    {
        var data = _audioManager.LoadAudioOggVorbis(new MemoryStream(fileBytes));
        return new BoomBoxAudioMetadata(data.Length, data.ChannelCount, data.Title, data.Artist);
    }

    public void SendSongData(EntityUid uid, byte[] songBytes)
    {
        var msg = new MsgBoomBoxSong()
        {
            EntityUid = uid,
            SongBytes = songBytes
        };

        _networkManager.ClientSendMessage(msg);
    }
}