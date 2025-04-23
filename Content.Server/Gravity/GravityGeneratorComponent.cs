using Content.Shared.Gravity;
using Robust.Shared.Serialization;

namespace Content.Server.Gravity
{
    [RegisterComponent]
    [Access(typeof(GravityGeneratorSystem))]
    public sealed partial class GravityGeneratorComponent : SharedGravityGeneratorComponent, ISerializationHooks
    {
        [DataField("lightRadiusMin")] public float LightRadiusMin { get; set; }
        [DataField("lightRadiusMax")] public float LightRadiusMax { get; set; }

        /// <summary>
        /// Is the gravity generator currently "producing" gravity?
        /// </summary>
        [ViewVariables]
        public bool GravityActive { get; set; } = false;

        void ISerializationHooks.AfterDeserialization()
        {
            var entityManager = IoCManager.Resolve<EntityManager>();
            if (!entityManager.Initialized)
            {
                return;
            }
            try
            {
                // No way to check if EntitySysManager is initialized directly, so we're checking it this way
                var _ = entityManager.EntitySysManager.DependencyCollection;
            }
            catch (InvalidOperationException)
            {
                return;
            }
            var gravitySystem = entityManager.SystemOrNull<GravitySystem>();
            if (gravitySystem == null)
            {
                return;
            }
            if (!entityManager.TransformQuery.TryGetComponent(Owner, out var xform)) {
                return;
            }
            gravitySystem.RefreshGravity(xform.ParentUid);
        }
    }
}
