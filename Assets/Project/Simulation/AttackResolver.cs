using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Chess.Core;

namespace Chess.Simulation
{
    public static class AttackResolver
    {
        // ���� ������ �� ã�� (�����Ÿ� + ��ֹ� üũ)
        public static List<UnitState> GetAttackTargets(UnitState attacker, BoardState board)
        {
            var targets = new List<UnitState>();
            var def = attacker.definition;

            // Pawn�� �밢�� 1ĭ��
            if (def.moveAttributes.HasFlag(MovementAttribute.Pawn))
            {
                targets.AddRange(GetPawnAttackTargets(attacker, board));
            }

            // Rook�� ���� (��ֹ� üũ)
            if (def.moveAttributes.HasFlag(MovementAttribute.Rook))
            {
                targets.AddRange(GetLinearAttackTargets(attacker, board));
            }

            // Bishop�� �밢�� (��ֹ� üũ)
            if (def.moveAttributes.HasFlag(MovementAttribute.Bishop))
            {
                targets.AddRange(GetDiagonalAttackTargets(attacker, board));
            }

            // Knight�� L��
            if (def.moveAttributes.HasFlag(MovementAttribute.Knight))
            {
                targets.AddRange(GetKnightAttackTargets(attacker, board));
            }

            // King�� 8���� 1ĭ
            if (def.moveAttributes.HasFlag(MovementAttribute.King))
            {
                targets.AddRange(GetKingAttackTargets(attacker, board));
            }

            // Enchanter - 동일 오프셋, 중간 기물 무시
            if (def.moveAttributes.HasFlag(MovementAttribute.Enchanter))
            {
                targets.AddRange(GetEnchanterAttackTargets(attacker, board));
            }

            // �ߺ� ���� (Queen ���� ���)
            return targets.Distinct().ToList();
        }

        // Pawn ���� (�밢�� 1ĭ)
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

        // ���� ���� (Rook) - ��ֹ� ����
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
                        // ���̸� ���� ���
                        if (targetUnit.ownerID != unit.ownerID)
                        {
                            targets.Add(targetUnit);
                        }
                        // ���� ������ �� �̻� �� �� (��ֹ� ����)
                        break;
                    }
                }
            }

            return targets;
        }

        // �밢�� ���� (Bishop) - ��ֹ� ����
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

        // Knight ���� (L��)
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

        // Enchanter 공격 (동일 오프셋, 중간 기물 무시)
        private static List<UnitState> GetEnchanterAttackTargets(UnitState unit, BoardState board)
        {
            var targets = new List<UnitState>();
            Vector2Int[] offsets = {
                new Vector2Int(2, 2),  new Vector2Int(2, -2),
                new Vector2Int(-2, 2), new Vector2Int(-2, -2),
                new Vector2Int(2, 0),  new Vector2Int(-2, 0),
                new Vector2Int(0, 2),  new Vector2Int(0, -2)
            };

            foreach (var offset in offsets)
            {
                var targetPos = unit.position + offset;
                if (board.IsInBounds(targetPos) && board.TryGetUnit(targetPos, out var target))
                    if (target.ownerID != unit.ownerID)
                        targets.Add(target);
            }

            return targets;
        }

        // King ���� (8���� 1ĭ)
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