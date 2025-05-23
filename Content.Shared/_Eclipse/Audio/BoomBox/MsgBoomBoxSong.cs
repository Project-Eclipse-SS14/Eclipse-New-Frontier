using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Eclipse.Audio.BoomBox;

/// <summary>
/// Sent server -> client to inform the client of their role bans.
/// </summary>
public sealed class MsgBoomBoxSong : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public EntityUid EntityUid { get; set; }
    public byte[] SongBytes = [];

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        EntityUid = new EntityUid(buffer.ReadVariableInt32());
        var count = buffer.ReadVariableInt32();
        SongBytes = buffer.ReadBytes(count);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(EntityUid.Id);
        buffer.WriteVariableInt32(SongBytes.Length);
        buffer.Write(SongBytes);
    }
}
