using System.Numerics; // Frontier: wallmount jukebox
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Eclipse.Audio.BoomBox;

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedBoomBoxSystem))]
public sealed partial class BoomBoxComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? AudioStream;

    public byte[] SongBytes;

    /// <summary>
    /// RSI state for the boombox being on.
    /// </summary>
    [DataField]
    public string? OnState;

    /// <summary>
    /// RSI state for the boombox being on.
    /// </summary>
    [DataField]
    public string? OffState;
}

[Serializable, NetSerializable]
public sealed class BoomBoxPlayingMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class BoomBoxPauseMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class BoomBoxStopMessage : BoundUserInterfaceMessage;


[Serializable, NetSerializable]
public sealed class BoomBoxSetTimeMessage(float songTime) : BoundUserInterfaceMessage
{
    public float SongTime { get; } = songTime;
}