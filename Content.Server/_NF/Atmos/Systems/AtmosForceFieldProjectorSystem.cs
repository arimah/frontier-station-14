using Content.Server._NF.Atmos.Components;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Content.Shared.Examine;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Robust.Server.GameObjects;

namespace Content.Server._NF.Atmos.Systems;

public sealed class AtmosForceFieldProjectorSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly AirtightSystem _airtight = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiver = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosForceFieldProjectorComponent, ComponentStartup>(OnProjectorStartup);
        SubscribeLocalEvent<AtmosForceFieldProjectorComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<AtmosForceFieldProjectorComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<AtmosForceFieldProjectorComponent, ExaminedEvent>(OnProjectorExamined);
        SubscribeLocalEvent<AtmosForceFieldProjectorComponent, ComponentShutdown>(OnProjectorShutdown);

        SubscribeLocalEvent<AtmosForceFieldComponent, ComponentShutdown>(OnForceFieldShutdown);
    }

    private void OnProjectorStartup(Entity<AtmosForceFieldProjectorComponent> ent, ref ComponentStartup args)
    {
        UpdateProjectorState(ent);
    }

    private void OnPowerChanged(Entity<AtmosForceFieldProjectorComponent> ent, ref PowerChangedEvent args)
    {
        UpdateProjectorState(ent);

        if (!args.Powered)
        {
            _deviceLink.InvokePort(ent, ent.Comp.PowerLostPort);
        }
    }

    private void OnSignalReceived(Entity<AtmosForceFieldProjectorComponent> ent, ref SignalReceivedEvent args)
    {
        var state = SignalState.Momentary;
        args.Data?.TryGetValue(DeviceNetworkConstants.LogicState, out state);

        if (args.Port == ent.Comp.OnPort)
        {
            if (state != SignalState.Low)
            {
                ent.Comp.TurnedOn = true;
            }
        }
        else if (args.Port == ent.Comp.OffPort)
        {
            if (state != SignalState.Low)
            {
                ent.Comp.TurnedOn = false;
            }
        }
        else if (args.Port == ent.Comp.TogglePort)
        {
            ent.Comp.TurnedOn = state switch
            {
                SignalState.Momentary => !ent.Comp.TurnedOn,
                SignalState.High => true,
                SignalState.Low => false,
                _ => ent.Comp.TurnedOn, // should be impossible, treat as a no-op
            };
        }

        UpdateProjectorState(ent);
    }

    private void OnProjectorExamined(Entity<AtmosForceFieldProjectorComponent> ent, ref ExaminedEvent args)
    {
        if (TryComp<BatteryComponent>(ent, out var battery))
        {
            var charge = battery.CurrentCharge / battery.MaxCharge * 100;
            args.PushMarkup(Loc.GetString("power-cell-component-examine-details", ("currentCharge", $"{charge:F0}")));
        }
    }

    private void OnProjectorShutdown(Entity<AtmosForceFieldProjectorComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.LinkedForceField is { } fieldUid &&
            LifeStage(fieldUid) < EntityLifeStage.Terminating)
        {
            // Stop the force field from spawning more force fields when deleted.
            ent.Comp.IsProjecting = false;
            Del(fieldUid);
        }
    }

    private void OnForceFieldShutdown(Entity<AtmosForceFieldComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ParentProjector is { } parentUid &&
            TryComp<AtmosForceFieldProjectorComponent>(parentUid, out var projector))
        {
            // Note: It's possible to unlink a force field this way by just VVing the AtmosForceFieldComponent
            // out of existence.
            projector.LinkedForceField = null;
            // If the projector should have a force field, spawn a new one. Otherwise, this is a no-op.
            EnsureForceField((parentUid, projector));
        }
    }

    private void UpdateProjectorState(Entity<AtmosForceFieldProjectorComponent> ent)
    {
        if (!TryComp<ApcPowerReceiverComponent>(ent, out var apcPower) ||
            !TryComp<AirtightComponent>(ent, out var airtight))
            return;

        var desiredLoad = ent.Comp.TurnedOn ? ent.Comp.ActiveLoad : ent.Comp.IdleLoad;
        _powerReceiver.SetLoad(ent, apcPower, desiredLoad);

        var isProjecting = ent.Comp.TurnedOn && apcPower.Powered;
        if (isProjecting != ent.Comp.IsProjecting)
        {
            ent.Comp.IsProjecting = isProjecting;
            _airtight.SetAirblocked((ent, airtight), ent.Comp.IsProjecting);
            EnsureForceField(ent);
        }
    }

    /// <summary>
    /// Ensures that the projector has a force field if it needs one, or doesn't have one
    /// if it shouldn't.
    /// </summary>
    private void EnsureForceField(Entity<AtmosForceFieldProjectorComponent> projector)
    {
        var hasValidForceField = projector.Comp.LinkedForceField is { } linkedFieldUid &&
            LifeStage(linkedFieldUid) < EntityLifeStage.Terminating;
        if (projector.Comp.IsProjecting && !hasValidForceField)
        {
            var xform = Transform(projector);
            var fieldUid = SpawnAttachedTo(projector.Comp.ForceFieldPrototype, xform.Coordinates, rotation: xform.LocalRotation);
            _xform.SetParent(fieldUid, projector);
            Comp<AtmosForceFieldComponent>(fieldUid).ParentProjector = projector;
            projector.Comp.LinkedForceField = fieldUid;
        }
        else if (!projector.Comp.IsProjecting && hasValidForceField)
        {
            var fieldUid = projector.Comp.LinkedForceField;
            projector.Comp.LinkedForceField = null;
            Del(fieldUid);
        }
    }
}
