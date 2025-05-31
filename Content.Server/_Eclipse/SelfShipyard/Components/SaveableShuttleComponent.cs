namespace Content.Server._Eclipse.SelfShipyard.Components
{
    [RegisterComponent]
    public sealed partial class SaveableShuttleComponent : Component
    {
        [DataField]
        public string? PrototypeId;
    }
}
