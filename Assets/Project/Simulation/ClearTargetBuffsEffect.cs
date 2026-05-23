using System.Collections.Generic;
using Chess.Core;

namespace Chess.Simulation
{
    // 단일 대상의 버프/디버프를 제거 (벨 - 망각의 실)
    public class ClearTargetBuffsEffect : SkillEffect
    {
        public override void Execute(UnitState caster, List<UnitState> targets, BoardState board)
        {
            foreach (var target in targets)
            {
                if (!target.IsAlive) continue;
                target.activeBuffs.Clear();
                target.fixedBuffs.Clear();
            }
        }
    }
}
