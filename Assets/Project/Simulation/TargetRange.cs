using System.Collections.Generic;
using UnityEngine;
using Chess.Core;

namespace Chess.Simulation
{
    public class TargetRange : SkillTarget
    {
        private bool targetAlly;

        public TargetRange(bool targetAlly = false)
        {
            this.targetAlly = targetAlly;
        }

        public override List<UnitState> SelectTargets(UnitState caster, BoardState board, List<Vector2Int> validMoves)
        {
            var targets = new List<UnitState>();

            foreach (var pos in validMoves)
            {
                if (!board.TryGetUnit(pos, out var unit)) continue;
                if (!unit.IsAlive) continue;

                bool isAlly = unit.ownerID == caster.ownerID;
                if (targetAlly == isAlly)
                    targets.Add(unit);
            }

            return targets;
        }
    }
}