using Content.Shared.Containers.ItemSlots;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Containers;
using Content.Shared.DoAfter;
using Content.Shared._Sunrise.MindBrearer;
using Content.Shared.Access.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Administration.Managers;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Doors.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Intellicard;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Repairable;
using Content.Shared.StationAi;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.MindBearer;

public abstract partial class MindBearerSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindBearerComponent, AfterInteractEvent>(OnMindBearerInteract);
        SubscribeLocalEvent<MindBearerComponent, MindBearerDoAfterEvent>(OnMindBearerDoAfter);
    }

    private void OnMindBearerInteract(Entity<MindBearerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } targetEnt)
            return;

        if (!TryComp<MindContainerComponent>(args.Target, out var targetContainer))
            return;

        if (HasComp<MindBearerComponent>(args.Target))
            return;

        if (!TryComp<MindBearerComponent>(args.Used, out var bearerComp))
            return;

        var bearerHasMind = _mind.TryGetMind(args.Used, out var bearerMind, out _);
        var targetHasMind = _mind.TryGetMind(targetEnt, out var targetMind, out _);

        if ((bearerHasMind && targetHasMind) || (!bearerHasMind && !targetHasMind))
        {
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, bearerHasMind ? bearerComp.UploadTime : bearerComp.DownloadTime, new MindBearerDoAfterEvent(), targetEnt, ent.Owner)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            BreakOnDropItem = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    public void OnMindBearerDoAfter(Entity<MindBearerComponent> ent, ref MindBearerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TryComp<MindContainerComponent>(args.Args.Target, out var targetContainer) ||
            targetContainer.Mind == null)
            return;
    }
}

[Serializable, NetSerializable]
public sealed partial class MindBearerDoAfterEvent : SimpleDoAfterEvent;
