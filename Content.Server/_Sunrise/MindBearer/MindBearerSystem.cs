using Content.Shared.Mind.Components;
using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Content.Shared.Ghost.Roles.Raffles;
using Content.Shared.Interaction;
using Content.Shared._Sunrise.MindBearer;
using Content.Shared.Popups;
using Content.Server.Ghost.Roles.Raffles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;

namespace Content.Server._Sunrise.MindBearer;

public sealed partial class MindBearerSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindBearerComponent, AfterInteractEvent>(OnMindBearerInteract);
        SubscribeLocalEvent<MindBearerComponent, MindBearerDoAfterEvent>(OnMindBearerDoAfter);
    }

    private void OnMindBearerInteract(Entity<MindBearerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
        {
            return;
        }

        if (ent.Comp.UsesLeft <= 0)
        {
            _popup.PopupEntity(Loc.GetString("mind-bearer-no-uses-left"), args.Target.Value, args.User, PopupType.Medium);
            return;
        }

        if (!_whitelist.IsWhitelistPass(ent.Comp.AllowTargets, args.Target.Value))
        {
            _popup.PopupEntity(Loc.GetString("mind-bearer-interact-not-allowed"), args.Target.Value, args.User, PopupType.Medium);
            return;
        }

        if (!HasComp<MindContainerComponent>(args.Target.Value) ||
            HasComp<MindBearerComponent>(args.Target.Value) ||
            _mind.TryGetMind(args.Target.Value, out var _, out _))
        {
            _popup.PopupEntity(Loc.GetString("mind-bearer-interact-not-allowed"), args.Target.Value, args.User, PopupType.Medium);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.UseTime, new MindBearerDoAfterEvent(), args.Used, args.Target, args.Used)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            BreakOnDropItem = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs))
            args.Handled = true;
    }

    private void OnMindBearerDoAfter(Entity<MindBearerComponent> ent, ref MindBearerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null || args.Args.Used == null)
        {
            return;
        }

        if (!_mind.TryGetMind(args.Args.User, out var mindId, out _))
        {
            _popup.PopupEntity(Loc.GetString("mind-bearer-interact-not-allowed"), args.Args.Target.Value, args.Args.User, PopupType.Medium);
            return;
        }

        ent.Comp.UsesLeft--;
        Dirty(ent);

        var oldEntity = args.Args.User;
        _mind.TransferTo(mindId, args.Args.Target.Value);

        var settings = new GhostRoleRaffleSettings()
        {
            InitialDuration = 10,
            JoinExtendsDurationBy = 10,
            MaxDuration = 30
        };

        var entMeta = MetaData(oldEntity);
        var ghostRoleComp = EnsureComp<GhostRoleComponent>(oldEntity);
        EnsureComp<GhostTakeoverAvailableComponent>(oldEntity);
        ghostRoleComp.RoleName = entMeta.EntityName;
        ghostRoleComp.RoleDescription = entMeta.EntityName;
        ghostRoleComp.RaffleConfig = new GhostRoleRaffleConfig(settings);

        args.Handled = true;
    }
}
