using System;
using System.Collections.Generic;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Draws entities from state. <see cref="Tick"/> runs once per tick: it gives a pooled sprite to
    /// each entity in an active area, sets its facing, and returns the sprites of entities that left
    /// an active area or were despawned. <see cref="Render"/> runs once per frame and only positions
    /// them.
    /// </summary>
    public class WorldView : IDisposable {
        static readonly Vector2 tileCentre = new Vector2(0.5f, 0.5f);

        Transform parent;
        PlaceholderSprites placeholders = new PlaceholderSprites();
        ObjectPool<SpriteRenderer> pool;
        // Every sprite the pool has created, so Dispose can destroy the pooled ones too.
        List<SpriteRenderer> created = new List<SpriteRenderer>();
        Dictionary<EntityID, SpriteRenderer> id2sprite = new Dictionary<EntityID, SpriteRenderer>();
        List<EntityID> entities = new List<EntityID>();
        List<EntityID> hidden = new List<EntityID>();

        /// <summary>Sprites showing an entity.</summary>
        public int visibleCount => id2sprite.Count;
        /// <summary>Sprites waiting in the pool.</summary>
        public int pooledCount => pool.availableCount;

        public WorldView(Transform parent) {
            this.parent = parent;
            pool = new ObjectPool<SpriteRenderer>(CreateSprite);
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
                    sprite = Show(ctx, id);
                }
                sprite.sprite = placeholders.Get(ctx.GetFacing(id));
            }

            HideLeftEntities(ctx);
        }

        public void Render(RenderContext ctx, float alpha) {
            foreach (var pair in id2sprite) {
                var position = ctx.GetBlendedPosition(pair.Key, alpha) + tileCentre;
                pair.Value.transform.position = new Vector3(position.x, position.y, 0);
            }
        }

        public void Dispose() {
            for (int i = 0; i < created.Count; i++) {
                if (created[i] != null) {
                    UnityObjects.Destroy(created[i].gameObject);
                }
            }
            created.Clear();
            id2sprite.Clear();
            placeholders.Dispose();
        }

        SpriteRenderer Show(RenderContext ctx, EntityID id) {
            var sprite = pool.Get();
            sprite.color = ctx.GetDefinition(id).placeholderTint;
#if UNITY_EDITOR
            sprite.gameObject.name = $"Entity {id}";
#endif
            sprite.gameObject.SetActive(true);
            id2sprite.Add(id, sprite);
            return sprite;
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
                sprite.gameObject.SetActive(false);
                pool.Return(sprite);
            }
        }

        SpriteRenderer CreateSprite() {
            var obj = new GameObject("Entity");
            obj.SetActive(false);
            obj.transform.SetParent(parent, false);

            var sprite = obj.AddComponent<SpriteRenderer>();
            sprite.sortingLayerName = SortingLayerNames.EntityOnGround;
            created.Add(sprite);
            return sprite;
        }
    }
}
