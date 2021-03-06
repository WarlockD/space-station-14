﻿#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.Atmos;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class AtmosphereSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPauseManager _pauseManager = default!;
#pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();

            _mapManager.TileChanged += OnTileChanged;
            EntityQuery = new MultipleTypeEntityQuery(new List<Type>(){typeof(IGridAtmosphereComponent)});
        }

        public IGridAtmosphereComponent? GetGridAtmosphere(GridId gridId)
        {
            // TODO Return space grid atmosphere for invalid grids or grids with no atmos
            var grid = _mapManager.GetGrid(gridId);

            if (!_entityManager.TryGetEntity(grid.GridEntityId, out var gridEnt)) return null;

            return gridEnt.TryGetComponent(out IGridAtmosphereComponent atmos) ? atmos : null;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var gridEnt in RelevantEntities)
            {
                var grid = gridEnt.GetComponent<IMapGridComponent>();
                if (_pauseManager.IsGridPaused(grid.GridIndex))
                    continue;

                gridEnt.GetComponent<IGridAtmosphereComponent>().Update(frameTime);
            }
        }

        private void OnTileChanged(object? sender, TileChangedEventArgs eventArgs)
        {
            // When a tile changes, we want to update it only if it's gone from
            // space -> not space or vice versa. So if the old tile is the
            // same as the new tile in terms of space-ness, ignore the change

            if (eventArgs.NewTile.Tile.IsEmpty == eventArgs.OldTile.IsEmpty)
            {
                return;
            }

            GetGridAtmosphere(eventArgs.NewTile.GridIndex)?.Invalidate(eventArgs.NewTile.GridIndices);
        }
    }
}
