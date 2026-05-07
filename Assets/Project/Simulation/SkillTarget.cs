using System.Collections.Generic;
using UnityEngine;
using Chess.Core;

namespace Chess.Simulation
{
    public abstract class SkillTarget
    {
        // range 숫자 대신 해당 유닛의 이동 가능한 칸 목록을 받음
        public abstract List<UnitState> SelectTargets(UnitState caster, BoardState board, List<Vector2Int> validMoves);
    }
}