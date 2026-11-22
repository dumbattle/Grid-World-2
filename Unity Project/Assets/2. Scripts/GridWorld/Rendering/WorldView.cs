using System;
using System.Collections.Generic;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Draws entities from state. <see cref="Tick"/> runs once per tick: it creates sprites for new
    /// entities, destroys those of despawned ones, and sets their facing. <see cref="Render"/> runs
    /// once per frame and only positions them.
    /// </summary>
    public class WorldView : IDisposable {
        static readonly Vector2 tileCentre = new Vector2(0.5f, 0.5f);

        Transform parent;
        PlaceholderSprites placeholders = new PlaceholderSprites();
        Dictionary<EntityID, SpriteRenderer> id2sprite = new Dictionary<EntityID, SpriteRenderer>();
        List<EntityID> entities = new List<EntityID>();
        List<EntityID> removed = new List<EntityID>();

        public WorldView(Transform parent) {
            this.parent = parent;
        }

        public void Tick(RenderContext ctx) {
            entities.Clear();
            ctx.CollectEntities(entities);

            for (int i = 0; i < entities.Count; i++) {
                var id = entities[i];
                if (!id2sprite.TryGetValue(id, out var sprite)) {
                    sprite = CreateSprite(id);
                    sprite.color = ctx.GetDefinition(id).placeholderTint;
                    id2sprite.Add(id, sprite);
                }
                sprite.sprite = placeholders.Get(ctx.GetFacing(id));
            }

            RemoveDespawned(ctx);
        }

        public void Render(RenderContext ctx, float alpha) {
            foreach (var pair in id2sprite) {
                var position = ctx.GetBlendedPosition(pair.Key, alpha) + tileCentre;
                pair.Value.transform.position = new Vector3(position.x, position.y, 0);
            }
        }

        public void Dispose() {
            foreach (var pair in id2sprite) {
                if (pair.Value != null) {
                    UnityObjects.Destroy(pair.Value.gameObject);
                }
            }
            id2sprite.Clear();
            placeholders.Dispose();
        }

        void RemoveDespawned(RenderContext ctx) {
            removed.Clear();
            foreach (var pair in id2sprite) {
                if (!ctx.EntityExists(pair.Key)) {
                    removed.Add(pair.Key);
                }
            }

            for (int i = 0; i < removed.Count; i++) {
                var id = removed[i];
                var sprite = id2sprite[id];
                if (sprite != null) {
                    UnityObjects.Destroy(sprite.gameObject);
                }
                id2sprite.Remove(id);
            }
        }

        SpriteRenderer CreateSprite(EntityID id) {
            var obj = new GameObject($"Entity {id}");
            obj.transform.SetParent(parent, false);

            var sprite = obj.AddComponent<SpriteRenderer>();
            sprite.sortingLayerName = SortingLayerNames.EntityOnGround;
            return sprite;
        }
    }
}
