using Content.Server.Construction.Components;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Server.Materials;
using Content.Shared.Damage;

namespace Content.Server._Eclipse.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public sealed partial class EmptyAndDamageMaterialStorageBehavior : IThresholdBehavior
    {
        [DataField]
        public DamageSpecifier Damage = default!;

        public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
        {
            var materialStorageSystem = system.EntityManager.System<MaterialStorageSystem>();
            var damageableSystem = system.EntityManager.System<DamageableSystem>();

            var entities = materialStorageSystem.EjectAllMaterial(owner);

            foreach (var ent in entities)
            {
                damageableSystem.TryChangeDamage(ent, Damage);
            }
        }
    }
}
