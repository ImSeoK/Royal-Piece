using System.Collections.Generic;
using Chess.Core;

namespace Chess.Simulation
{
    // 모든 유닛의 버프/디버프/DoT를 제거 (카이로스 - 공허로의 회귀)
    public class ClearBuffsEffect : SkillEffect
    {
        public override void Execute(UnitState caster, List<UnitState> targets, BoardState board)
        {
            foreach (var target in targets)
            {
                if (!target.IsAlive) continue;
                target.activeBuffs.Clear();
                target.fixedBuffs.Clear();
                target.damageOverTimes.Clear();
            }
        }
    }
}
