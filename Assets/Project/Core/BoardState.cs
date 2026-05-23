using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Chess.Core
{
    public class BoardState
    {
        private Dictionary<Vector2Int, UnitState> units = new Dictionary<Vector2Int, UnitState>();

        // 유닛 배치
        public void PlaceUnit(UnitState unit, Vector2Int position)
        {
            units[position] = unit;
            unit.position = position;
            InvalidateAllUnitCaches();
        }

        // 유닛 제거
        public void RemoveUnit(Vector2Int position)
        {
            units.Remove(position);
            InvalidateAllUnitCaches();
        }

        // 유닛 이동
        public void MoveUnit(UnitState unit, Vector2Int newPosition)
        {
            units.Remove(unit.position);
            PlaceUnit(unit, newPosition); // PlaceUnit이 InvalidateAllUnitCaches 호출
        }

        // 보드 배치 변경 시 모든 유닛의 Always 캐시를 무효화
        private void InvalidateAllUnitCaches()
        {
            foreach (var unit in units.Values)
                unit.InvalidateCache();
        }

        // 특정 위치에 유닛이 있는지 확인
        public bool TryGetUnit(Vector2Int position, out UnitState unit)
        {
            return units.TryGetValue(position, out unit);
        }

        // 해당 위치가 비어있는지
        public bool IsEmpty(Vector2Int position)
        {
            return !units.ContainsKey(position);
        }

        // 보드 범위 내인지
        public bool IsInBounds(Vector2Int position)
        {
            return position.x >= 0 && position.x < 8
                && position.y >= 0 && position.y < 8;
        }

        // 모든 유닛 열거
        public IEnumerable<UnitState> GetAllUnits()
        {
            return units.Values;
        }

        // ── 위치 기반 탐지 헬퍼 (1-2) ─────────────────────────────────────

        // 같은 가로줄(y좌표)의 살아있는 유닛 반환
        // ownerID 지정 시 해당 플레이어 유닛만 반환
        public List<UnitState> GetUnitsInRow(int y, int? ownerID = null)
        {
            var result = new List<UnitState>();
            foreach (var unit in units.Values)
            {
                if (!unit.IsAlive) continue;
                if (unit.position.y != y) continue;
                if (ownerID.HasValue && unit.ownerID != ownerID.Value) continue;
                result.Add(unit);
            }
            return result;
        }

        // 같은 세로줄(x좌표)의 살아있는 유닛 반환
        public List<UnitState> GetUnitsInColumn(int x, int? ownerID = null)
        {
            var result = new List<UnitState>();
            foreach (var unit in units.Values)
            {
                if (!unit.IsAlive) continue;
                if (unit.position.x != x) continue;
                if (ownerID.HasValue && unit.ownerID != ownerID.Value) continue;
                result.Add(unit);
            }
            return result;
        }

        // 인접 8방향 1칸 안의 살아있는 유닛 반환 (자기 자신 제외)
        public List<UnitState> GetAdjacentUnits(Vector2Int pos, int? ownerID = null)
        {
            var result = new List<UnitState>();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var target = new Vector2Int(pos.x + dx, pos.y + dy);
                    if (!IsInBounds(target)) continue;
                    if (!TryGetUnit(target, out var unit)) continue;
                    if (!unit.IsAlive) continue;
                    if (ownerID.HasValue && unit.ownerID != ownerID.Value) continue;
                    result.Add(unit);
                }
            }
            return result;
        }

        // 3-3: caster의 SPD가 모든 적보다 높은지 확인
        public bool HasHighestSpeedAgainstEnemies(UnitState caster)
        {
            int casterSpeed = caster.GetSpeed();
            foreach (var unit in units.Values)
            {
                if (!unit.IsAlive) continue;
                if (unit.ownerID == caster.ownerID) continue;
                if (unit.GetSpeed() >= casterSpeed) return false;
            }
            return true;
        }

        // 직선 8방향(가로/세로/대각)에서 방향별 첫 번째 살아있는 유닛 반환
        // 벽이나 다른 유닛에 막히면 탐색 중단 (시야 차단)
        public List<UnitState> GetUnitsInLine(Vector2Int pos, int? ownerID = null)
        {
            var result = new List<UnitState>();
            Vector2Int[] directions = {
                new Vector2Int(1, 0),  new Vector2Int(-1, 0),
                new Vector2Int(0, 1),  new Vector2Int(0, -1),
                new Vector2Int(1, 1),  new Vector2Int(-1, 1),
                new Vector2Int(1, -1), new Vector2Int(-1, -1)
            };

            foreach (var dir in directions)
            {
                var current = pos + dir;
                while (IsInBounds(current))
                {
                    if (TryGetUnit(current, out var unit) && unit.IsAlive)
                    {
                        if (!ownerID.HasValue || unit.ownerID == ownerID.Value)
                            result.Add(unit);
                        break; // 첫 번째 유닛에서 탐색 중단
                    }
                    current += dir;
                }
            }
            return result;
        }
    }
}
