using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Chess.Core;

namespace Chess.Simulation
{
    public class SimpleAI
    {
        private BoardState board;
        private int playerID;

        public SimpleAI(BoardState board, int playerID)
        {
            this.board = board;
            this.playerID = playerID;
        }

        // AI 턴 실행
        public (UnitState unit, Vector2Int targetPos) DecideMove()
        {
            // 내 유닛들 가져오기
            var myUnits = board.GetAllUnits()
                .Where(u => u.ownerID == playerID && u.IsAlive)
                .ToList();

            if (myUnits.Count == 0)
                return (null, Vector2Int.zero);

            // 전략 1: 공격 가능한 유닛 우선
            foreach (var unit in myUnits)
            {
                var moves = MovementResolver.GetValidMoves(unit, board);

                foreach (var move in moves)
                {
                    // 적이 있는 위치면 공격
                    if (board.TryGetUnit(move, out var target))
                    {
                        if (target.ownerID != playerID)
                        {
                            Debug.Log($"[AI] 공격 선택: {unit.definition.unitName} → {target.definition.unitName}");
                            return (unit, move);
                        }
                    }
                }
            }

            // 전략 2: 공격 못하면 랜덤 이동
            // 이동 가능한 유닛만 필터링
            var movableUnits = myUnits
                .Where(u => MovementResolver.GetValidMoves(u, board).Count > 0)
                .ToList();

            if (movableUnits.Count == 0)
            {
                Debug.Log("[AI] 이동 가능한 유닛 없음");
                return (null, Vector2Int.zero);
            }

            // 랜덤 유닛 선택
            var randomUnit = movableUnits[Random.Range(0, movableUnits.Count)];
            var validMoves = MovementResolver.GetValidMoves(randomUnit, board);

            // 랜덤 이동 선택
            var randomMove = validMoves[Random.Range(0, validMoves.Count)];

            Debug.Log($"[AI] 랜덤 이동: {randomUnit.definition.unitName} → ({randomMove.x}, {randomMove.y})");
            return (randomUnit, randomMove);
        }
    }
}