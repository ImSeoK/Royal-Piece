using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Chess.Core;

namespace Chess.Balance
{
    // 밸런스 시뮬레이션 전용 AI.
    // SimpleAI(게임용)는 건드리지 않는다.
    //
    // 우선순위
    //  1. 이동 후 적 King을 공격할 수 있는 수 → 즉시 실행
    //  2. 이동 후 공격 가능한 적 수가 가장 많은 수 (그리디)
    //  3. 이동 가능한 유닛 중 랜덤 이동 (fallback)
    public class BalanceAI
    {
        private readonly BoardState board;
        private readonly int playerID;

        public BalanceAI(BoardState board, int playerID)
        {
            this.board    = board;
            this.playerID = playerID;
        }

        public (UnitState unit, Vector2Int targetPos) DecideMove()
        {
            var myUnits = board.GetAllUnits()
                .Where(u => u.ownerID == playerID && u.IsAlive)
                .ToList();

            if (myUnits.Count == 0)
                return (null, Vector2Int.zero);

            // ----- 우선순위 1: 킹 직접 공격 가능한 수 -----
            foreach (var unit in myUnits)
            {
                foreach (var move in Chess.Simulation.MovementResolver.GetValidMoves(unit, board))
                {
                    if (CanAttackKingAfterMove(unit, move))
                        return (unit, move);
                }
            }

            // ----- 우선순위 2: 적을 가장 많이 공격할 수 있는 수 -----
            int bestCount   = 0;
            UnitState bestUnit   = null;
            Vector2Int bestMove  = Vector2Int.zero;

            foreach (var unit in myUnits)
            {
                foreach (var move in Chess.Simulation.MovementResolver.GetValidMoves(unit, board))
                {
                    int count = CountAttackableEnemiesAfterMove(unit, move);
                    if (count > bestCount)
                    {
                        bestCount = count;
                        bestUnit  = unit;
                        bestMove  = move;
                    }
                }
            }

            if (bestCount > 0)
                return (bestUnit, bestMove);

            // ----- 우선순위 3: 랜덤 이동 (완전 막힌 상황 fallback) -----
            var movable = myUnits
                .Where(u => Chess.Simulation.MovementResolver.GetValidMoves(u, board).Count > 0)
                .ToList();

            if (movable.Count == 0)
                return (null, Vector2Int.zero);

            var rUnit  = movable[Random.Range(0, movable.Count)];
            var rMoves = Chess.Simulation.MovementResolver.GetValidMoves(rUnit, board);
            return (rUnit, rMoves[Random.Range(0, rMoves.Count)]);
        }

        // 임시 이동 → 킹 공격 가능 여부 확인 → 이동 복원
        private bool CanAttackKingAfterMove(UnitState unit, Vector2Int move)
        {
            var originalPos = unit.position;
            board.MoveUnit(unit, move);

            bool result = Chess.Simulation.AttackResolver.GetAttackTargets(unit, board)
                .Any(t => t.definition.isKing);

            board.MoveUnit(unit, originalPos);
            return result;
        }

        // 임시 이동 → 공격 가능한 적 수 반환 → 이동 복원
        private int CountAttackableEnemiesAfterMove(UnitState unit, Vector2Int move)
        {
            var originalPos = unit.position;
            board.MoveUnit(unit, move);

            int count = Chess.Simulation.AttackResolver.GetAttackTargets(unit, board).Count;

            board.MoveUnit(unit, originalPos);
            return count;
        }
    }
}
