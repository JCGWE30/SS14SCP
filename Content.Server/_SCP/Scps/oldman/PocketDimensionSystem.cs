using Content.Server._SCP.Scps.oldman.Components;
using Content.Server.GameTicking;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Actions;
using Content.Shared.Humanoid;
using System.Numerics;
using Content.Shared.Coordinates;
using Content.Shared.Movement.Components;

namespace Content.Server._SCP.Scps.oldman;


public sealed class PocketDimensionSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    private ISawmill _sawmill = default!;

    public const string pocketDimensionMapPath = "/Maps/Test/admin_test_arena.yml";

    public override void Initialize()
    {
        _sawmill = _logManager.GetSawmill("pocketdimensionlogs");
        base.Initialize();

        SubscribeLocalEvent<PocketDimensionSenderComponent, MeleeHitEvent>(OnSend);

        SubscribeLocalEvent<PocketDimensionSenderComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PocketDimensionSenderComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PocketDimensionSenderComponent, TogglePocketDimensionDoAfter>(OnTogglePocketDimeison);
    }

    private void OnSend(EntityUid owner, PocketDimensionSenderComponent comp, MeleeHitEvent args)
    {
        if (comp.pocketDimensionGrid == null)
            return;
        foreach (var entity in args.HitEntities)
        {
            if (!TryComp<HumanoidAppearanceComponent>(entity, out var person))
                return;
            _xformSystem.SetCoordinates(entity, new EntityCoordinates(comp.pocketDimensionGrid.Value, Vector2.Zero));
        }
    }

    private void OnTogglePocketDimeison(EntityUid owner, PocketDimensionSenderComponent comp, TogglePocketDimensionDoAfter args)
    {

        if (!comp.traversing)
            return;

        comp.traversing = false;

        if (args.Cancelled)
            return;

        if (comp.pocketDimensionGrid == null)
            return;

        if(comp.inPocketDimension)
        { 
            _xformSystem.SetCoordinates(owner, comp.lastLocation);
        }
        else
        {
            if (!TryComp<MobMoverComponent>(owner, out var mover))
                return;
            comp.lastLocation = mover.LastPosition;
            _xformSystem.SetCoordinates(owner, new EntityCoordinates(comp.pocketDimensionGrid.Value, Vector2.Zero));
        }
        comp.inPocketDimension = !comp.inPocketDimension;
    }

    private void OnStartup(EntityUid owner, PocketDimensionSenderComponent comp, ComponentStartup args)
    {
        _actions.AddAction(owner, comp.traversePocketAction);
        if (comp.pocketDimensionGrid == null)
        {
            var map = _mapManager.GetMapEntityId(_mapManager.CreateMap());
            _metaDataSystem.SetEntityName(map, "Pocket Dimension");

            var grids = _map.LoadMap(Comp<MapComponent>(map).MapId, pocketDimensionMapPath);
            if (grids.Count > 0)
            {
                _metaDataSystem.SetEntityName(grids[0], "Pocket Dimension Grid");
                comp.pocketDimensionGrid = grids[0];
            }
            comp.pocketDimensionMap = map;
        }
    }

    private void OnShutdown(EntityUid owner, PocketDimensionSenderComponent comp, ComponentShutdown args)
    {
        if (comp.pocketDimensionGrid == null)
            return;
        _mapManager.DeleteGrid(comp.pocketDimensionGrid.Value);
        _mapManager.DeleteMap(Comp<MapComponent>(comp.pocketDimensionMap).MapId);
    }

}
