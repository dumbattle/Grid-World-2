using System;
using System.Collections.Generic;
using LPE;
using LPE.Animation;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Draws entities from state. <see cref="Tick"/> runs once per tick: it gives a pooled sprite to
    /// each entity in an active area, advances its animation, and returns the sprites of entities
    /// that left an active area or were despawned. <see cref="Render"/> runs once per frame and only
    /// positions them.
    /// </summary>
    public class WorldView : IDisposable {
        // Sprites have bottom pivots, so they stand on the bottom edge of their tile.
        static readonly Vector2 tileFoot = new Vector2(0.5f, 0f);

        Transform parent;
        PlaceholderSprites placeholders = new PlaceholderSprites();
        ObjectPool<EntitySprite> pool;
        // Every sprite the pool has created, so Dispose can destroy the pooled ones too.
        List<EntitySprite> created = new List<EntitySprite>();
        Dictionary<EntityID, EntitySprite> id2sprite = new Dictionary<EntityID, EntitySprite>();
        List<EntityID> entities = new List<EntityID>();
        List<EntityID> hidden = new List<EntityID>();

        /// <summary>Sprites showing an entity.</summary>
        public int visibleCount => id2sprite.Count;
        /// <summary>Sprites waiting in the pool.</summary>
        public int pooledCount => pool.availableCount;

        public WorldView(Transform parent) {
            this.parent = parent;
            pool = new ObjectPool<EntitySprite>(CreateSprite);
        }

        public void Tick(RenderContext ctx) {
            entities.Clear();
            ctx.CollectEntities(entities);

            for (int i = 0; i < entities.Count; i++) {
                var id = entities[i];
                if (!ctx.IsEntityInActiveArea(id)) {
                    continue;
                }
                if (!id2sprite.TryGetValue(id, out var sprite)) {
                    sprite = Show(id);
                }
                Animate(ctx, id, sprite);
            }

            HideLeftEntities(ctx);
        }

        public void Render(RenderContext ctx, float alpha) {
            foreach (var pair in id2sprite) {
                var position = ctx.GetBlendedPosition(pair.Key, alpha) + tileFoot;
                pair.Value.renderer.transform.position = new Vector3(position.x, position.y, 0);
            }
        }

        public void Dispose() {
            for (int i = 0; i < created.Count; i++) {
                if (created[i].renderer != null) {
                    UnityObjects.Destroy(created[i].renderer.gameObject);
                }
            }
            created.Clear();
            id2sprite.Clear();
            placeholders.Dispose();
        }

        EntitySprite Show(EntityID id) {
            var sprite = pool.Get();
            // A reused sprite must start its new entity's clip from the beginning.
            sprite.clip = null;
#if UNITY_EDITOR
            sprite.renderer.gameObject.name = $"Entity {id}";
#endif
            sprite.renderer.gameObject.SetActive(true);
            id2sprite.Add(id, sprite);
            return sprite;
        }

        void Animate(RenderContext ctx, EntityID id, EntitySprite sprite) {
            var definition = ctx.GetDefinition(id);
            var facing = ctx.GetFacing(id);
            var step = ctx.GetStep(id);
            var state = EntityAnimationRules.ChooseState(step);
            var clip = EntityAnimationRules.ChooseClip(definition.animation, state, facing);

            if (clip == null) {
                sprite.clip = null;
                sprite.renderer.color = definition.placeholderTint;
                sprite.renderer.sprite = placeholders.Get(facing);
                return;
            }

            sprite.renderer.color = Color.white;
            // The same clip keeps playing, so a walk carries on across chained steps. A new state or
            // facing restarts the cycle.
            if (sprite.clip != clip) {
                sprite.clip = clip;
                sprite.player.Set(sprite.renderer, clip, true);
            }

            int speed = EntityAnimationRules.ChooseSpeed(step, definition);
            for (int i = 0; i < speed; i++) {
                sprite.player.Update();
            }
        }

        // Returns the sprites of entities that were despawned or are no longer in an active area.
        void HideLeftEntities(RenderContext ctx) {
            hidden.Clear();
            foreach (var pair in id2sprite) {
                if (!ctx.EntityExists(pair.Key) || !ctx.IsEntityInActiveArea(pair.Key)) {
                    hidden.Add(pair.Key);
                }
            }

            for (int i = 0; i < hidden.Count; i++) {
                var id = hidden[i];
                var sprite = id2sprite[id];
                id2sprite.Remove(id);
                sprite.renderer.gameObject.SetActive(false);
                pool.Return(sprite);
            }
        }

        EntitySprite CreateSprite() {
            var obj = new GameObject("Entity");
            obj.SetActive(false);
            obj.transform.SetParent(parent, false);

            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = SortingLayerNames.EntityOnGround;
            // Sort by the feet, so an entity in front of another draws over it.
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;

            var sprite = new EntitySprite(renderer);
            created.Add(sprite);
            return sprite;
        }

        class EntitySprite {
            public readonly SpriteRenderer renderer;
            public readonly AnimationPlayer<SpriteRenderer> player = new AnimationPlayer<SpriteRenderer>();
            // The clip the player is on, or null while the entity draws a placeholder.
            public SpriteClip clip;

            public EntitySprite(SpriteRenderer renderer) {
                this.renderer = renderer;
            }
        }
    }
}
