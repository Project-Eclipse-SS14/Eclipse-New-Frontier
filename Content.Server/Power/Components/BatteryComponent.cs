using Content.Server.Power.EntitySystems;
using Content.Shared.Guidebook;

namespace Content.Server.Power.Components
{
    /// <summary>
    ///     Battery node on the pow3r network. Needs other components to connect to actual networks.
    /// </summary>
    [RegisterComponent]
    [Virtual]
    [Access(typeof(BatterySystem))]
    public partial class BatteryComponent : Component
    {
        public string SolutionName = "battery";

        /// <summary>
        /// Maximum charge of the battery in joules (ie. watt seconds)
        /// </summary>
        [DataField]
        [GuidebookData]
        public float MaxCharge;

        /// <summary>
        /// Current charge of the battery in joules (ie. watt seconds)
        /// </summary>
        [DataField("startingCharge")]
        public float CurrentCharge;

        /// <summary>
        /// The price per one joule. Default is 1 credit for 20kJ.
        /// </summary>
        [DataField]
        public float PricePerJoule = 0.00005f; // Eclipse 0.0001 -> 0.00005
    }

    /// <summary>
    ///     Raised when a battery's charge or capacity changes (capacity affects relative charge percentage).
    /// </summary>
    [ByRefEvent]
    public readonly record struct ChargeChangedEvent(float Charge, float MaxCharge);
}
