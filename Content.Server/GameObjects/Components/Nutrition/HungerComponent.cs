﻿using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Damage;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Nutrition;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    public sealed class HungerComponent : SharedHungerComponent
    {
        // Base stuff
        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseDecayRate
        {
            get => _baseDecayRate;
            set => _baseDecayRate = value;
        }
        private float _baseDecayRate;

        [ViewVariables(VVAccess.ReadWrite)]
        public float ActualDecayRate
        {
            get => _actualDecayRate;
            set => _actualDecayRate = value;
        }
        private float _actualDecayRate;

        // Hunger
        [ViewVariables(VVAccess.ReadOnly)]
        public override HungerThreshold CurrentHungerThreshold => _currentHungerThreshold;
        private HungerThreshold _currentHungerThreshold;

        private HungerThreshold _lastHungerThreshold;

        [ViewVariables(VVAccess.ReadWrite)]
        public float CurrentHunger
        {
            get => _currentHunger;
            set => _currentHunger = value;
        }
        private float _currentHunger;

        [ViewVariables(VVAccess.ReadOnly)]
        public Dictionary<HungerThreshold, float> HungerThresholds => _hungerThresholds;
        private Dictionary<HungerThreshold, float> _hungerThresholds = new Dictionary<HungerThreshold, float>
        {
            {HungerThreshold.Overfed, 600.0f},
            {HungerThreshold.Okay, 450.0f},
            {HungerThreshold.Peckish, 300.0f},
            {HungerThreshold.Starving, 150.0f},
            {HungerThreshold.Dead, 0.0f},
        };

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _baseDecayRate, "base_decay_rate", 0.1f);
        }

        // for shared string dict, since we don't define these anywhere in content
        [UsedImplicitly]
        public static readonly string[] _hungerThresholdImages =
        {
            "/Textures/Interface/StatusEffects/Hunger/Overfed.png",
            "/Textures/Interface/StatusEffects/Hunger/Okay.png",
            "/Textures/Interface/StatusEffects/Hunger/Peckish.png",
            "/Textures/Interface/StatusEffects/Hunger/Starving.png",
            "/Textures/Interface/StatusEffects/Hunger/Dead.png",
        };

        public void HungerThresholdEffect(bool force = false)
        {
            if (_currentHungerThreshold != _lastHungerThreshold || force)
            {
                // Revert slow speed if required
                if (_lastHungerThreshold == HungerThreshold.Starving && _currentHungerThreshold != HungerThreshold.Dead &&
                    Owner.TryGetComponent(out MovementSpeedModifierComponent movementSlowdownComponent))
                {
                    movementSlowdownComponent.RefreshMovementSpeedModifiers();
                }

                // Update UI
                Owner.TryGetComponent(out ServerStatusEffectsComponent statusEffectsComponent);
                statusEffectsComponent?.ChangeStatusEffectIcon(StatusEffect.Hunger, _hungerThresholdImages[ (int)_currentHungerThreshold ]);
                switch (_currentHungerThreshold)
                {
                    case HungerThreshold.Overfed:
                        _lastHungerThreshold = _currentHungerThreshold;
                        _actualDecayRate = _baseDecayRate * 1.2f;
                        return;

                    case HungerThreshold.Okay:
                        _lastHungerThreshold = _currentHungerThreshold;
                        _actualDecayRate = _baseDecayRate;
                        return;

                    case HungerThreshold.Peckish:
                        // Same as okay except with UI icon saying eat soon.
                        _lastHungerThreshold = _currentHungerThreshold;
                        _actualDecayRate = _baseDecayRate * 0.8f;
                        return;

                    case HungerThreshold.Starving:
                        // TODO: If something else bumps this could cause mega-speed.
                        // If some form of speed update system if multiple things are touching it use that.
                        if (Owner.TryGetComponent(out MovementSpeedModifierComponent movementSlowdownComponent1))
                        {
                            movementSlowdownComponent1.RefreshMovementSpeedModifiers();
                        }
                        _lastHungerThreshold = _currentHungerThreshold;
                        _actualDecayRate = _baseDecayRate * 0.6f;
                        return;

                    case HungerThreshold.Dead:
                        return;
                    default:
                        Logger.ErrorS("hunger", $"No hunger threshold found for {_currentHungerThreshold}");
                        throw new ArgumentOutOfRangeException($"No hunger threshold found for {_currentHungerThreshold}");
                }
            }
        }

        protected override void Startup()
        {
            base.Startup();
            // Similar functionality to SS13. Should also stagger people going to the chef.
            _currentHunger = IoCManager.Resolve<IRobustRandom>().Next(
                (int)_hungerThresholds[HungerThreshold.Peckish] + 10,
                (int)_hungerThresholds[HungerThreshold.Okay] - 1);
            _currentHungerThreshold = GetHungerThreshold(_currentHunger);
            _lastHungerThreshold = HungerThreshold.Okay; // TODO: Potentially change this -> Used Okay because no effects.
            HungerThresholdEffect(true);
            Dirty();
        }

        public HungerThreshold GetHungerThreshold(float food)
        {
            HungerThreshold result = HungerThreshold.Dead;
            var value = HungerThresholds[HungerThreshold.Overfed];
            foreach (var threshold in _hungerThresholds)
            {
                if (threshold.Value <= value && threshold.Value >= food)
                {
                    result = threshold.Key;
                    value = threshold.Value;
                }
            }

            return result;
        }

        public void UpdateFood(float amount)
        {
            _currentHunger = Math.Min(_currentHunger + amount, HungerThresholds[HungerThreshold.Overfed]);
        }

        // TODO: If mob is moving increase rate of consumption?
        //  Should use a multiplier as something like a disease would overwrite decay rate.
        public void OnUpdate(float frametime)
        {
            _currentHunger -= frametime * ActualDecayRate;
            var calculatedHungerThreshold = GetHungerThreshold(_currentHunger);
            // _trySound(calculatedThreshold);
            if (calculatedHungerThreshold != _currentHungerThreshold)
            {
                _currentHungerThreshold = calculatedHungerThreshold;
                HungerThresholdEffect();
                Dirty();
            }
            if (_currentHungerThreshold == HungerThreshold.Dead)
            {
                if (Owner.TryGetComponent(out DamageableComponent damage))
                {
                    if (!damage.IsDead())
                    {
                        damage.TakeDamage(DamageType.Brute, 2);
                    }
                }
            }
        }

        public void ResetFood()
        {
            _currentHungerThreshold = HungerThreshold.Okay;
            _currentHunger = HungerThresholds[_currentHungerThreshold];
            HungerThresholdEffect();
        }

        public override ComponentState GetComponentState()
        {
            return new HungerComponentState(_currentHungerThreshold);
        }
    }


}
