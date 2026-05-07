using UnityEngine;
using System.Collections.Generic;
using Chess.Core;

namespace Chess.Simulation
{
    public class MoveEffect : SkillEffect
    {
        private int distance;

        public MoveEffect(int distance)
        {
            this.distance = distance;
        }

        public override void Execute(UnitState caster, List<UnitState> targets, BoardState board)
        {
            if (caster == null) return; // 밀어내기 방향 계산에 캐스터 필요

            foreach (var target in targets)
            {
                if (!target.IsAlive) continue;

                // ĳ���� �ݴ� �������� �о
                Vector2Int direction = target.position - caster.position;
                if (direction.x != 0) direction.x = direction.x > 0 ? 1 : -1;
                if (direction.y != 0) direction.y = direction.y > 0 ? 1 : -1;

                Vector2Int newPos = target.position + direction * distance;

                if (board.IsInBounds(newPos) && board.IsEmpty(newPos))
                    board.MoveUnit(target, newPos);
            }
        }
    }
}