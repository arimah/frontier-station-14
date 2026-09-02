using Content.Server._NF.Atmos.Systems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Atmos.Components;

[RegisterComponent]
[Access(typeof(AtmosForceFieldProjectorSystem))]
public sealed partial class AtmosForceFieldProjectorComponent : Component
{
    /// <summary>
    /// True if the projector wants to project a force field, i.e. it's been turned on
    /// by a signal. The projector needs to be enabled and powered to actually produce
    /// a force field; see <see cref="IsProjecting"/>.
    /// </summary>
    [DataField]
    public bool TurnedOn = false;

    /// <summary>
    /// True if the projector is currently turned on (<see cref="TurnedOn"/>) and
    /// projecting a force field. If the projector loses power, the field will also
    /// dissipate.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsProjecting = false;

    /// <summary>
    /// The port that turns the force field on.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> OnPort = "On";

    /// <summary>
    /// The port that turns the force field off.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> OffPort = "Off";

    /// <summary>
    /// The port that toggles the force field. Can be a momentary signal or a stateful signal.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";

    /// <summary>
    /// The port that is invoked when the projector loses power completely.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> PowerLostPort = "PowerLost";

    /// <summary>
    /// The entity prototype to spawn when the force field is turned on. Every projector has
    /// at most one force field, linked through <see cref="LinkedForceField"/>
    /// </summary>
    [DataField]
    public EntProtoId ForceFieldPrototype = "NFAtmosForceField";

    /// <summary>
    /// The currently active force field. If the projector is turned off, this field is null.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? LinkedForceField;

    [DataField]
    /// <summary>
    /// Power load (in watts) when turned off, i.e. when <see cref="TurnedOn"/> is false.
    /// </summary>
    public float IdleLoad = 250;

    [DataField]
    /// <summary>
    /// Power load (in watts) when turned on, i.e. when <see cref="TurnedOn"/> is true.
    /// </summary>
    /// <remarks>
    /// Note: This value is used even if <see cref="IsProjecting"/> is false. This results in a more
    /// even power load and prevents power flicker after a power outage. Otherwise, the power load
    /// increases as soon as power returns, which may cause power to go out again, which lowers the
    /// load and causes power to return, and...
    /// </remarks>
    public float ActiveLoad = 750;
}
