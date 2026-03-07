using System.Collections.Generic;
using UnityEngine;
using Chess.Core;

namespace Chess.Simulation
{
    public class BoardState
    {
        private Dictionary<Vector2Int, UnitState> units = new Dictionary<Vector2Int, UnitState>();

        // 유닛 배치
        public void PlaceUnit(UnitState unit, Vector2Int position)
        {
            units[position] = unit;
            unit.position = position;
        }

        // 유닛 제거
        public void RemoveUnit(Vector2Int position)
        {
            units.Remove(position);
        }

        // 유닛 이동
        public void MoveUnit(UnitState unit, Vector2Int newPosition)
        {
            // 기존 위치에서 제거
            units.Remove(unit.position);

            // 새 위치에 배치
            PlaceUnit(unit, newPosition);
        }

        // 특정 위치에 유닛 있는지 확인
        public bool TryGetUnit(Vector2Int position, out UnitState unit)
        {
            return units.TryGetValue(position, out unit);
        }

        // 해당 위치가 비어있는지
        public bool IsEmpty(Vector2Int position)
        {
            return !units.ContainsKey(position);
        }

        // 보드 범위 안인지
        public bool IsInBounds(Vector2Int position)
        {
            return position.x >= 0 && position.x < 8
                && position.y >= 0 && position.y < 8;
        }

        // 모든 유닛 가져오기
        public IEnumerable<UnitState> GetAllUnits()
        {
            return units.Values;
        }
    }
}