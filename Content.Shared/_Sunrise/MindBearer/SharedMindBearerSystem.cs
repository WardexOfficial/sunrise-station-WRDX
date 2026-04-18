using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Network;

namespace Content.Shared._Sunrise.MindBearer;

public abstract partial class SharedMindBearerSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindBearerComponent, AfterInteractEvent>(OnMindBearerInteract);
        SubscribeLocalEvent<MindBearerComponent, MindBearerDoAfterEvent>(OnMindBearerDoAfter);
    }

    private void OnMindBearerInteract(Entity<MindBearerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (ent.Comp.UsesLeft <= 0)
        {
            _popup.PopupClient(Loc.GetString("mind-bearer-no-uses-left"), args.Target.Value, args.User, PopupType.Medium);
            return;
        }

        if (!_whitelist.IsWhitelistPass(ent.Comp.AllowTargets, args.Target.Value))
        {
            _popup.PopupClient(Loc.GetString("mind-bearer-interact-not-allowed"), args.Target.Value, args.User, PopupType.Medium);
            return;
        }

        if (!HasComp<MindContainerComponent>(args.Target.Value))
        {
            _popup.PopupClient(Loc.GetString("mind-bearer-target-not-have-mind-container"), args.Target.Value, args.User, PopupType.Medium);
            return;
        }

        if (HasComp<MindBearerComponent>(args.Target.Value))
        {
            _popup.PopupClient(Loc.GetString("mind-bearer-interact-not-allowed"), args.Target.Value, args.User, PopupType.Medium);
            return;
        }

        if (_mind.TryGetMind(args.Target.Value, out _, out _))
        {
            _popup.PopupClient(Loc.GetString("mind-bearer-target-already-has-mind"), args.Target.Value, args.User, PopupType.Medium);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.UseTime, new MindBearerDoAfterEvent(), args.Used, args.Target, args.Used)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            BreakOnDropItem = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameTool,
            AttemptFrequency = AttemptFrequency.EveryTick,
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs))
            args.Handled = true;
    }

    protected virtual void OnMindBearerDoAfter(Entity<MindBearerComponent> ent, ref MindBearerDoAfterEvent args)
    {
        if (args.Cancelled || args.Args.Target == null || args.Args.Used == null)
            return;

        if (!_mind.TryGetMind(args.Args.User, out var mindId, out _) ||
            _mind.TryGetMind(args.Args.Target.Value, out _, out _))
        {
            _popup.PopupClient(Loc.GetString("mind-bearer-transfer-failed"), args.Args.Target.Value, args.Args.User, PopupType.Medium);
            return;
        }

        _mind.TransferTo(mindId, args.Args.Target.Value);

        if (_netManager.IsServer)
        {
            ent.Comp.UsesLeft--;
            Dirty(ent);
        }
    }
}
