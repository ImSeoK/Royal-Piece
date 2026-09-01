using UnityEngine;
using System.Collections.Generic;
using Chess.Core;

namespace Chess.Simulation
{
    // 대상을 시전자 방향으로 끌어당김 — 시전자와 1칸 거리까지 (아노말리 - 심연의 인력)
    public class PullEffect : SkillEffect
    {
        public override void Execute(UnitState caster, List<UnitState> targets, BoardState board)
        {
            if (caster == null) return;

            foreach (var target in targets)
            {
                if (!target.IsAlive) continue;
                if (SkillExecutor.IsPullImmune(target, board)) continue;
                PullTarget(caster, target, board);
            }
        }

        private void PullTarget(UnitState caster, UnitState target, BoardState board)
        {
            Vector2Int diff = caster.position - target.position;
            if (diff == Vector2Int.zero) return;

            Vector2Int direction = new Vector2Int(
                System.Math.Sign(diff.x),
                System.Math.Sign(diff.y)
            );

            // 목표 위치: 시전자로부터 1칸 떨어진 곳
            Vector2Int desiredPos = caster.position - direction;

            if (!board.IsInBounds(desiredPos)) return;

            if (board.IsEmpty(desiredPos))
            {
                board.MoveUnit(target, desiredPos);
                return;
            }

            // 목표 위치가 막혀 있으면 현재 위치에서 가능한 만큼 이동
            Vector2Int current = target.position;
            while (true)
            {
                Vector2Int next = current + direction;
                if (!board.IsInBounds(next)) break;
                if (!board.IsEmpty(next)) break;
                current = next;
                if (current == desiredPos) break;
            }

            if (current != target.position)
                board.MoveUnit(target, current);
        }
    }
}
