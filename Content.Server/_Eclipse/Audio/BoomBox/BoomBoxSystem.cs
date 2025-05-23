using System.Numerics;
using Content.Shared._Eclipse.Audio.BoomBox;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;
using NVorbis;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Network;

namespace Content.Server._Eclipse.Audio.BoomBox;

public sealed class BoomBoxSystem : SharedBoomBoxSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly INetManager _networkManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        _networkManager.RegisterNetMessage<MsgBoomBoxSong>(OnJukeboxSelected);

        SubscribeLocalEvent<BoomBoxComponent, BoomBoxPlayingMessage>(OnJukeboxPlay);
        SubscribeLocalEvent<BoomBoxComponent, BoomBoxPauseMessage>(OnJukeboxPause);
        SubscribeLocalEvent<BoomBoxComponent, BoomBoxStopMessage>(OnJukeboxStop);
        SubscribeLocalEvent<BoomBoxComponent, BoomBoxSetTimeMessage>(OnJukeboxSetTime);
        SubscribeLocalEvent<BoomBoxComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnJukeboxSelected(MsgBoomBoxSong msg)
    {
        var uid = msg.EntityUid;
        if (!TryComp<BoomBoxComponent>(uid, out var component))
        {
            return;
        }

        if (!Audio.IsPlaying(component.AudioStream))
        {
            component.SongBytes = msg.SongBytes;
            component.AudioStream = Audio.Stop(component.AudioStream);
        }

        Dirty(uid, component);
    }

    private void OnJukeboxPause(Entity<BoomBoxComponent> ent, ref BoomBoxPauseMessage args)
    {
        Audio.SetState(ent.Comp.AudioStream, AudioState.Paused);
    }

    private void OnJukeboxSetTime(EntityUid uid, BoomBoxComponent component, BoomBoxSetTimeMessage args)
    {
        if (TryComp<ActorComponent>(args.Actor, out var actorComp))
        {
            var offset = actorComp.PlayerSession.Channel.Ping * 1.5f / 1000f;
            Audio.SetPlaybackPosition(component.AudioStream, args.SongTime + offset);
        }
    }

    private void OnJukeboxStop(Entity<BoomBoxComponent> entity, ref BoomBoxStopMessage args)
    {
        Stop(entity);
    }

    private void Stop(Entity<BoomBoxComponent> entity)
    {
        Audio.SetState(entity.Comp.AudioStream, AudioState.Stopped);
        Dirty(entity);
    }

    private void OnComponentShutdown(EntityUid uid, BoomBoxComponent component, ComponentShutdown args)
    {
        component.AudioStream = Audio.Stop(component.AudioStream);
    }

    private void OnJukeboxPlay(EntityUid uid, BoomBoxComponent component, ref BoomBoxPlayingMessage args)
    {
        if (Exists(component.AudioStream))
        {
            Audio.SetState(component.AudioStream, AudioState.Playing);
        }
        else
        {
            component.AudioStream = Audio.Stop(component.AudioStream);

            if (component.SongBytes is null)
            {
                return;
            }

            if (TrySetupAudio(component.SongBytes, AudioParams.Default.WithMaxDistance(10f), out var entity))
            {
                _xform.SetCoordinates(entity.Value, new EntityCoordinates(uid, Vector2.Zero));
                component.AudioStream = entity;
            }

            Dirty(uid, component);
        }
    }

    private bool TrySetupAudio(byte[] fileBytes, AudioParams? audioParams, [NotNullWhen(true)] out Entity<BoomBoxAudioComponent>? ent, bool initialize = true, TimeSpan? length = null)
    {
        var uid = EntityManager.CreateEntityUninitialized("Audio", MapCoordinates.Nullspace);

        _metadata.SetEntityName(uid, $"Audio (boombox {uid})", raiseEvents: false);
        audioParams ??= AudioParams.Default;
        var comp = AddComp<BoomBoxAudioComponent>(uid);
        comp.FileBytes = fileBytes;
        comp.Params = audioParams.Value;
        comp.AudioStart = _timing.CurTime;

        if (!audioParams.Value.Loop)
        {
            if (length is null)
            {
                BoomBoxAudioMetadata loadedMetadata;
                using (var reader = new VorbisReader(new MemoryStream(fileBytes), false))
                {
                    try
                    {
                        reader.Initialize();
                    }
                    catch (InvalidDataException)
                    {
                        ent = null;
                        return false;
                    }
                    loadedMetadata = new BoomBoxAudioMetadata(reader.TotalTime, reader.Channels, reader.Tags.Title, reader.Tags.Artist);
                }
                length = loadedMetadata.Length;
            }

            var despawn = AddComp<TimedDespawnComponent>(uid);
            // Don't want to clip audio too short due to imprecision.
            despawn.Lifetime = (float)length.Value.TotalSeconds + SharedAudioSystem.AudioDespawnBuffer;
        }

        if (initialize)
        {
            EntityManager.InitializeAndStartEntity(uid);
        }

        ent = new Entity<BoomBoxAudioComponent>(uid, comp);
        return true;
    }
}