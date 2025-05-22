using Content.Shared.Doors.Components;
using Content.Shared.Roles;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SprayPainter.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PaintableAirlockComponent : Component, ISerializationHooks
{
    /// <summary>
    /// Group of styles this airlock can be painted with, e.g. glass, standard or external.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<AirlockGroupPrototype> Group = string.Empty;

    /// <summary>
    /// Department this airlock is painted as, or none.
    /// Must be specified in prototypes for turf war to work.
    /// To better catch any mistakes, you need to explicitly state a non-styled airlock has a null department.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<DepartmentPrototype>? Department;

    /// <summary>
    /// Stores last sprite that was set on an airlock, used to restore airlock's sprite when loading a map
    /// </summary>
    [DataField]
    public string LastSetSprite = string.Empty;

    void ISerializationHooks.AfterDeserialization()
    {
        if (string.IsNullOrEmpty(LastSetSprite))
            return;

        var entityManager = IoCManager.Resolve<EntityManager>();
        if (!entityManager.Initialized)
            return;

        try
        {
            // No way to check if EntitySysManager is initialized directly, so we're checking it this way
            var _ = entityManager.EntitySysManager.DependencyCollection;
        }
        catch (InvalidOperationException)
        {
            return;
        }
        var appearance = entityManager.SystemOrNull<SharedAppearanceSystem>();
        if (appearance != null)
        {
            appearance.SetData(Owner, DoorVisuals.BaseRSI, LastSetSprite);
        }
    }
}
