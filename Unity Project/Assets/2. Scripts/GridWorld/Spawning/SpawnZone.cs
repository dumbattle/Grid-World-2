using System;
using System.Collections.Generic;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Keeps up to <see cref="maxCount"/> mobs alive inside a rectangle of tiles, spawning one at a
    /// time with <see cref="respawnDelayTicks"/> between them. It's the only way to reach its mobs.
    /// Most steps are virtual, so a game can subclass it and return the subclass from its own
    /// <see cref="SpawnZoneData.CreateZone"/>.
    /// </summary>
    public class SpawnZone {
        List<EntityID> mobList = new List<EntityID>();
        List<Vector2Int> freeTiles = new List<Vector2Int>();
        List<Vector2Int> hiddenTiles = new List<Vector2Int>();
        int cooldown;

        /// <summary>In world tiles.</summary>
        public RectInt region { get; }
        public EntityDefinitionSO definition { get; }
        public int maxCount { get; }
        public int respawnDelayTicks { get; }
        /// <summary>Gives each mob its AI. Null for mobs that stand still.</summary>
        public SpawnAIData ai { get; }

        /// <summary>The mobs this zone spawned that still exist, as of its last tick.</summary>
        public IReadOnlyList<EntityID> mobs => mobList;
        /// <summary>Ticks left before the zone may spawn again. Counts down only while the zone is active.</summary>
        public int ticksUntilSpawn => cooldown;

        /// <param name="region">In world tiles.</param>
        public SpawnZone(RectInt region, EntityDefinitionSO definition, int maxCount, int respawnDelayTicks, SpawnAIData ai) {
            if (region.width <= 0 || region.height <= 0) {
                throw new ArgumentException($"A spawn zone needs a non-empty region, not {region}.", nameof(region));
            }
            if (definition == null) {
                throw new ArgumentNullException(nameof(definition));
            }
            if (maxCount < 1) {
                throw new ArgumentOutOfRangeException(nameof(maxCount), $"A spawn zone needs a max count of at least 1, not {maxCount}.");
            }
            if (respawnDelayTicks < 0) {
                throw new ArgumentOutOfRangeException(nameof(respawnDelayTicks), $"A respawn delay can't be negative, not {respawnDelayTicks}.");
            }

            this.region = region;
            this.definition = definition;
            this.maxCount = maxCount;
            this.respawnDelayTicks = respawnDelayTicks;
            this.ai = ai;
        }

        /// <summary>
        /// Once per tick, in the spawn phase. Forgets mobs that no longer exist, whatever removed
        /// them, then counts down and spawns while the zone is active.
        /// </summary>
        public virtual void Tick(SpawnContext ctx) {
            ForgetRemovedMobs(ctx);

            if (!IsActive(ctx)) {
                return;
            }
            if (cooldown > 0) {
                cooldown--;
            }
            if (ShouldSpawn(ctx)) {
                TrySpawn(ctx);
            }
        }

        /// <summary>By default, whether the area holding the zone is active.</summary>
        protected virtual bool IsActive(SpawnContext ctx) {
            return ctx.IsInActiveArea(region.position);
        }

        protected virtual bool ShouldSpawn(SpawnContext ctx) {
            return cooldown == 0 && mobList.Count < maxCount;
        }

        /// <summary>Spawns one mob. Returns false if there was no tile for it, so it retries next tick.</summary>
        protected virtual bool TrySpawn(SpawnContext ctx) {
            if (!TryChooseTile(ctx, out var tile)) {
                return false;
            }

            var id = ctx.SpawnEntity(ChooseDefinition(ctx), tile, ChooseFacing(ctx));
            var mobAI = CreateAI(ctx);
            if (mobAI != null) {
                ctx.AddAI(id, mobAI);
            }
            OnSpawned(ctx, id);
            return true;
        }

        /// <summary>
        /// A random tile from <see cref="CanSpawnOn"/>, preferring tiles outside the view so mobs
        /// don't appear on screen. Falls back to a tile in view if every free tile is visible.
        /// </summary>
        protected virtual bool TryChooseTile(SpawnContext ctx, out Vector2Int tile) {
            freeTiles.Clear();
            hiddenTiles.Clear();

            for (int y = region.yMin; y < region.yMax; y++) {
                for (int x = region.xMin; x < region.xMax; x++) {
                    var candidate = new Vector2Int(x, y);
                    if (!CanSpawnOn(ctx, candidate)) {
                        continue;
                    }
                    freeTiles.Add(candidate);
                    if (!ctx.IsTileInView(candidate)) {
                        hiddenTiles.Add(candidate);
                    }
                }
            }

            var choices = hiddenTiles.Count > 0 ? hiddenTiles : freeTiles;
            if (choices.Count == 0) {
                tile = default;
                return false;
            }
            tile = choices[ctx.RandomRange(0, choices.Count)];
            return true;
        }

        /// <summary>By default, a tile with no entity that can be entered from at least one direction.</summary>
        protected virtual bool CanSpawnOn(SpawnContext ctx, Vector2Int tile) {
            if (ctx.GetEntityAt(tile).IsValid()) {
                return false;
            }
            foreach (var direction in AIDirections.all) {
                if (ctx.CanEnter(tile, direction)) {
                    return true;
                }
            }
            return false;
        }

        protected virtual EntityDefinitionSO ChooseDefinition(SpawnContext ctx) {
            return definition;
        }

        protected virtual Direction4 ChooseFacing(SpawnContext ctx) {
            return AIDirections.all[ctx.RandomRange(0, AIDirections.all.Length)];
        }

        /// <summary>A fresh AI for each mob, kept inside the zone's region. Null to stand still.</summary>
        protected virtual EntityAI CreateAI(SpawnContext ctx) {
            return ai?.CreateAI(region);
        }

        /// <summary>Tracks the mob and starts the respawn delay. Call the base method when overriding.</summary>
        protected virtual void OnSpawned(SpawnContext ctx, EntityID id) {
            mobList.Add(id);
            cooldown = respawnDelayTicks;
        }

        /// <summary>
        /// The mob no longer exists and has already left <see cref="mobs"/>. Restarts the respawn
        /// delay by default.
        /// </summary>
        protected virtual void OnRemoved(SpawnContext ctx, EntityID id) {
            cooldown = respawnDelayTicks;
        }

        void ForgetRemovedMobs(SpawnContext ctx) {
            for (int i = mobList.Count - 1; i >= 0; i--) {
                var id = mobList[i];
                if (!ctx.EntityExists(id)) {
                    mobList.RemoveAt(i);
                    OnRemoved(ctx, id);
                }
            }
        }
    }
}
