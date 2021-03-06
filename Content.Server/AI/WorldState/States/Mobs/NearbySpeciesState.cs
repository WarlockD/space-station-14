using System.Collections.Generic;
using Content.Server.AI.Utils;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Movement;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.WorldState.States.Mobs
{
    [UsedImplicitly]
    public sealed class NearbySpeciesState : CachedStateData<List<IEntity>>
    {
        public override string Name => "NearbySpecies";

        protected override List<IEntity> GetTrueValue()
        {
            var result = new List<IEntity>();

            if (!Owner.TryGetComponent(out AiControllerComponent controller))
            {
                return result;
            }

            foreach (var entity in Visibility.GetEntitiesInRange(Owner.Transform.GridPosition, typeof(SpeciesComponent), controller.VisionRadius))
            {
                if (entity == Owner) continue;
                result.Add(entity);
            }

            return result;
        }
    }
}
