using Content.Shared.Guidebook;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared._Eclipse.SelfShipyard.Prototypes;

[Prototype]
public sealed class OwnedVesselPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<OwnedVesselPrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    ///     Vessel name.
    /// </summary>
    [DataField] public string Name = string.Empty;

    /// <summary>
    ///     Short description of the vessel.
    /// </summary>
    [DataField] public string Description = string.Empty;

    /// <summary>
    ///     The price of the vessel
    /// </summary>
    [DataField(required: true)]
    public int Price;

    /// <summary>
    ///     The access required to buy the product. (e.g. Command, Mail, Bailiff, etc.)
    /// </summary>
    [DataField]
    public string Access = string.Empty;

    /// Frontier - Add this field for the MapChecker script.
    /// <summary>
    ///     The MapChecker override group for this vessel.
    /// </summary>
    [DataField("mapchecker_group_override")]
    public string MapcheckerGroup = string.Empty;

    /// <summary>
    ///     Relative directory path to the given shuttle, i.e. `/Maps/Shuttles/yourshittle.yml`
    /// </summary>
    [DataField(required: true)]
    public ResPath ShuttlePath = default!;

    /// <summary>
    ///     Guidebook page associated with a shuttle
    /// </summary>
    [DataField]
    public ProtoId<GuideEntryPrototype>? GuidebookPage = default!;

    /// <summary>
    ///     The price markup of the vessel testing
    /// </summary>
    [DataField]
    public float MinPriceMarkup = 1.05f;

    /// <summary>
    /// Components to be added to any spawned grids.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry AddComponents { get; set; } = new();
}