using Content.Server._NF.Atmos.Systems;

namespace Content.Server._NF.Atmos.Components;

[RegisterComponent]
[Access(typeof(AtmosForceFieldProjectorSystem))]
public sealed partial class AtmosForceFieldComponent : Component
{
    /// <summary>
    /// The projector that owns the force field.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? ParentProjector;
}
