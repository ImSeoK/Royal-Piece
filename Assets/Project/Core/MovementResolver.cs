using System.Collections.Generic;
using UnityEngine;
using Chess.Core;

namespace Chess.Simulation
{
    public static class MovementResolver
    {
        // 이동 가능한 위치 계산
        public static List<Vector2Int> GetValidMoves(UnitState unit, BoardState board)
        {
            var moves = new List<Vector2Int>();

            // Rook 속성
            if (unit.definition.moveAttributes.HasFlag(MovementAttribute.Rook))
            {
                moves.AddRange(GetLinearMoves(unit.position, board));
            }

            // Bishop 속성
            if (unit.definition.moveAttributes.HasFlag(MovementAttribute.Bishop))
            {
                moves.AddRange(GetDiagonalMoves(unit.position, board));
            }

            // Knight 속성
            if (unit.definition.moveAttributes.HasFlag(MovementAttribute.Knight))
            {
                moves.AddRange(GetKnightMoves(unit.position, board));
            }

            // Enchanter 속성
            if (unit.definition.moveAttributes.HasFlag(MovementAttribute.Enchanter))
            {
                moves.AddRange(GetEnchanterMoves(unit.position, board));
            }

            // King 속성
            if (unit.definition.moveAttributes.HasFlag(MovementAttribute.King))
            {
                moves.AddRange(GetKingMoves(unit.position, board));
            }

            // Pawn 속성
            if (unit.definition.moveAttributes.HasFlag(MovementAttribute.Pawn))
            {
                moves.AddRange(GetPawnMoves(unit, board));
            }

            return moves;
        }

        // 직선 이동 (Rook) - 유닛 있으면 막힘
        private static List<Vector2Int> GetLinearMoves(Vector2Int pos, BoardState board)
        {
            var moves = new List<Vector2Int>();

            Vector2Int[] directions = {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            foreach (var dir in directions)
            {
                for (int i = 1; i < 8; i++)
                {
                    var targetPos = pos + dir * i;

                    if (!board.IsInBounds(targetPos))
                        break;

                    // 유닛 있으면 막힘 (아군이든 적이든)
                    if (board.TryGetUnit(targetPos, out var targetUnit))
                    {
                        break;
                    }

                    moves.Add(targetPos);
                }
            }

            return moves;
        }

        // 대각선 이동 (Bishop) - 유닛 있으면 막힘
        private static List<Vector2Int> GetDiagonalMoves(Vector2Int pos, BoardState board)
        {
            var moves = new List<Vector2Int>();

            Vector2Int[] directions = {
                new Vector2Int(1, 1),
                new Vector2Int(1, -1),
                new Vector2Int(-1, 1),
                new Vector2Int(-1, -1)
            };

            foreach (var dir in directions)
            {
                for (int i = 1; i < 8; i++)
                {
                    var targetPos = pos + dir * i;

                    if (!board.IsInBounds(targetPos))
                        break;

                    // 유닛 있으면 막힘
                    if (board.TryGetUnit(targetPos, out var targetUnit))
                    {
                        break;
                    }

                    moves.Add(targetPos);
                }
            }

            return moves;
        }

        // Knight 이동 - 빈칸만
        private static List<Vector2Int> GetKnightMoves(Vector2Int pos, BoardState board)
        {
            var moves = new List<Vector2Int>();

            Vector2Int[] offsets = {
                new Vector2Int(2, 1), new Vector2Int(2, -1),
                new Vector2Int(-2, 1), new Vector2Int(-2, -1),
                new Vector2Int(1, 2), new Vector2Int(1, -2),
                new Vector2Int(-1, 2), new Vector2Int(-1, -2)
            };

            foreach (var offset in offsets)
            {
                var targetPos = pos + offset;

                // 빈칸만 이동 가능
                if (board.IsInBounds(targetPos) && board.IsEmpty(targetPos))
                {
                    moves.Add(targetPos);
                }
            }

            return moves;
        }

        // Enchanter 이동
        private static List<Vector2Int> GetEnchanterMoves(Vector2Int pos, BoardState board)
        {
            var moves = new List<Vector2Int>();

            // Distance-2 jump on same-color tiles (8 candidate tiles)
            // 4 diagonal + 4 straight
            Vector2Int[] offsets = {
        new Vector2Int(2, 2), new Vector2Int(2, -2),
        new Vector2Int(-2, 2), new Vector2Int(-2, -2),
        new Vector2Int(2, 0), new Vector2Int(-2, 0),
        new Vector2Int(0, 2), new Vector2Int(0, -2)
    };

            foreach (var offset in offsets)
            {
                var targetPos = pos + offset;

                // Out of bounds check
                if (!board.IsInBounds(targetPos)) continue;

                // Empty tile check (jumping ignores middle units, but lands on empty only)
                if (!board.IsEmpty(targetPos)) continue;

                moves.Add(targetPos);
            }

            return moves;
        }

        // King 이동 - 빈칸만
        private static List<Vector2Int> GetKingMoves(Vector2Int pos, BoardState board)
        {
            var moves = new List<Vector2Int>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;

                    var targetPos = pos + new Vector2Int(x, y);

                    // 빈칸만 이동 가능
                    if (board.IsInBounds(targetPos) && board.IsEmpty(targetPos))
                    {
                        moves.Add(targetPos);
                    }
                }
            }

            return moves;
        }

        // Pawn 이동 - 앞으로만 (대각선 이동 X, 공격만 가능)
        private static List<Vector2Int> GetPawnMoves(UnitState unit, BoardState board)
        {
            var moves = new List<Vector2Int>();

            int forward = unit.ownerID == 0 ? 1 : -1;

            // 1칸 전진 (비어있을 때만)
            var frontPos = unit.position + new Vector2Int(0, forward);
            if (board.IsInBounds(frontPos) && board.IsEmpty(frontPos))
            {
                moves.Add(frontPos);
            }

            return moves;
        }
    }
}