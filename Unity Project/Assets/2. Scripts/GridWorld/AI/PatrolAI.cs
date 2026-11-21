using System;
using System.Collections.Generic;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Walks a route of points that each share a row or column with the next. At each point it turns
    /// to the point's facing, if it has one, and waits. A blocked step retries on the next tick. An
    /// entity out of line with its next point stands still and logs a warning.
    /// </summary>
    public class PatrolAI : EntityAI {
        PatrolPoint[] points;
        PatrolMode mode;
        int index;
        int travel = 1;
        bool arrived;
        int waitLeft;
        bool warned;

        /// <param name="points">In world tiles.</param>
        public PatrolAI(PatrolPoint[] points, PatrolMode mode) {
            if (points == null) {
                throw new ArgumentNullException(nameof(points));
            }

            var tiles = new Vector2Int[points.Length];
            for (int i = 0; i < points.Length; i++) {
                tiles[i] = points[i].tile;
            }
            var error = FindRouteError(tiles, mode);
            if (error != null) {
                throw new ArgumentException(error);
            }

            this.points = (PatrolPoint[])points.Clone();
            this.mode = mode;
        }

        /// <summary>Why a route isn't valid, or null if it is.</summary>
        public static string FindRouteError(IReadOnlyList<Vector2Int> tiles, PatrolMode mode) {
            if (tiles.Count < 2) {
                return "A patrol needs at least two points.";
            }

            int legs = mode == PatrolMode.Loop ? tiles.Count : tiles.Count - 1;
            for (int i = 0; i < legs; i++) {
                int next = (i + 1) % tiles.Count;
                var from = tiles[i];
                var to = tiles[next];
                if (from.x != to.x && from.y != to.y) {
                    return $"Patrol points {i} {from} and {next} {to} don't share a row or column.";
                }
            }
            return null;
        }

        public override void Decide(AIContext ctx, EntityID self) {
            var tile = ctx.GetTile(self);
            var point = points[index];

            if (tile == point.tile) {
                if (!arrived) {
                    arrived = true;
                    waitLeft = point.waitTicks;
                    if (point.facing != null && ctx.GetFacing(self) != point.facing) {
                        ctx.RequestMove(self, point.facing, turnOnly: true);
                        return;
                    }
                }
                if (waitLeft > 0) {
                    waitLeft--;
                    return;
                }

                Advance();
                arrived = false;
                point = points[index];
                // Two points on one tile turn in place, handled as an arrival next tick.
                if (tile == point.tile) {
                    return;
                }
            }

            var delta = point.tile - tile;
            if (delta.x != 0 && delta.y != 0) {
                if (!warned) {
                    warned = true;
                    Debug.LogWarning($"Grid World: patrolling {self} at {tile} is out of line with its next point {point.tile}, so it's standing still.");
                }
                return;
            }

            warned = false;
            var direction = Direction4.FromVector2Int(new Vector2Int(Math.Sign(delta.x), Math.Sign(delta.y)));
            ctx.RequestMove(self, direction);
        }

        void Advance() {
            if (mode == PatrolMode.Loop) {
                index = (index + 1) % points.Length;
                return;
            }

            if (index + travel < 0 || index + travel >= points.Length) {
                travel = -travel;
            }
            index += travel;
        }
    }
}
