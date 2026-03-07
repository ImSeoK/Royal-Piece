using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Chess.Core;

namespace Chess.Simulation
{
    public static class AttackResolver
    {
        // 공격 가능한 적 찾기 (사정거리 + 장애물 체크)
        public static List<UnitState> GetAttackTargets(UnitState attacker, BoardState board)
        {
            var targets = new List<UnitState>();
            var def = attacker.definition;

            // Pawn은 대각선 1칸만
            if (def.moveAttributes.HasFlag(MovementAttribute.Pawn))
            {
                targets.AddRange(GetPawnAttackTargets(attacker, board));
            }

            // Rook은 직선 (장애물 체크)
            if (def.moveAttributes.HasFlag(MovementAttribute.Rook))
            {
                targets.AddRange(GetLinearAttackTargets(attacker, board));
            }

            // Bishop은 대각선 (장애물 체크)
            if (def.moveAttributes.HasFlag(MovementAttribute.Bishop))
            {
                targets.AddRange(GetDiagonalAttackTargets(attacker, board));
            }

            // Knight는 L자
            if (def.moveAttributes.HasFlag(MovementAttribute.Knight))
            {
                targets.AddRange(GetKnightAttackTargets(attacker, board));
            }

            // King은 8방향 1칸
            if (def.moveAttributes.HasFlag(MovementAttribute.King))
            {
                targets.AddRange(GetKingAttackTargets(attacker, board));
            }

            // 중복 제거 (Queen 같은 경우)
            return targets.Distinct().ToList();
        }

        // Pawn 공격 (대각선 1칸)
        private static List<UnitState> GetPawnAttackTargets(UnitState unit, BoardState board)
        {
            var targets = new List<UnitState>();
            int forward = unit.ownerID == 0 ? 1 : -1;

            Vector2Int[] attackPositions = {
                unit.position + new Vector2Int(-1, forward),
                unit.position + new Vector2Int(1, forward)
            };

            foreach (var pos in attackPositions)
            {
                if (board.IsInBounds(pos) && board.TryGetUnit(pos, out var target))
                {
                    if (target.ownerID != unit.ownerID)
                    {
                        targets.Add(target);
                    }
                }
            }

            return targets;
        }

        // 직선 공격 (Rook) - 장애물 차단
        private static List<UnitState> GetLinearAttackTargets(UnitState unit, BoardState board)
        {
            var targets = new List<UnitState>();
            Vector2Int[] directions = {
                Vector2Int.up, Vector2Int.down,
                Vector2Int.left, Vector2Int.right
            };

            foreach (var dir in directions)
            {
                for (int i = 1; i < 8; i++)
                {
                    var targetPos = unit.position + dir * i;

                    if (!board.IsInBounds(targetPos))
                        break;

                    if (board.TryGetUnit(targetPos, out var targetUnit))
                    {
                        // 적이면 공격 대상
                        if (targetUnit.ownerID != unit.ownerID)
                        {
                            targets.Add(targetUnit);
                        }
                        // 유닛 있으면 더 이상 못 감 (장애물 차단)
                        break;
                    }
                }
            }

            return targets;
        }

        // 대각선 공격 (Bishop) - 장애물 차단
        private static List<UnitState> GetDiagonalAttackTargets(UnitState unit, BoardState board)
        {
            var targets = new List<UnitState>();
            Vector2Int[] directions = {
                new Vector2Int(1, 1), new Vector2Int(1, -1),
                new Vector2Int(-1, 1), new Vector2Int(-1, -1)
            };

            foreach (var dir in directions)
            {
                for (int i = 1; i < 8; i++)
                {
                    var targetPos = unit.position + dir * i;

                    if (!board.IsInBounds(targetPos))
                        break;

                    if (board.TryGetUnit(targetPos, out var targetUnit))
                    {
                        if (targetUnit.ownerID != unit.ownerID)
                        {
                            targets.Add(targetUnit);
                        }
                        break;
                    }
                }
            }

            return targets;
        }

        // Knight 공격 (L자)
        private static List<UnitState> GetKnightAttackTargets(UnitState unit, BoardState board)
        {
            var targets = new List<UnitState>();
            Vector2Int[] offsets = {
                new Vector2Int(2, 1), new Vector2Int(2, -1),
                new Vector2Int(-2, 1), new Vector2Int(-2, -1),
                new Vector2Int(1, 2), new Vector2Int(1, -2),
                new Vector2Int(-1, 2), new Vector2Int(-1, -2)
            };

            foreach (var offset in offsets)
            {
                var targetPos = unit.position + offset;

                if (board.IsInBounds(targetPos) && board.TryGetUnit(targetPos, out var target))
                {
                    if (target.ownerID != unit.ownerID)
                    {
                        targets.Add(target);
                    }
                }
            }

            return targets;
        }

        // King 공격 (8방향 1칸)
        private static List<UnitState> GetKingAttackTargets(UnitState unit, BoardState board)
        {
            var targets = new List<UnitState>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;

                    var targetPos = unit.position + new Vector2Int(x, y);

                    if (board.IsInBounds(targetPos) && board.TryGetUnit(targetPos, out var target))
                    {
                        if (target.ownerID != unit.ownerID)
                        {
                            targets.Add(target);
                        }
                    }
                }
            }

            return targets;
        }
    }
}