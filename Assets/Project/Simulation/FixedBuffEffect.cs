using System.Collections.Generic;
using Chess.Core;

namespace Chess.Simulation
{
    // 대상 스탯을 절대값으로 고정 (미요 - 매혹의 춤)
    public class FixedBuffEffect : SkillEffect
    {
        private StatType statType;
        private int fixedValue;
        private int duration;

        public FixedBuffEffect(StatType statType, int value, int duration)
        {
            this.statType = statType;
            this.fixedValue = value;
            this.duration = duration;
        }

        public override void Execute(UnitState caster, List<UnitState> targets, BoardState board)
        {
            foreach (var target in targets)
            {
                if (target.IsAlive)
                    target.ApplyFixedBuff(statType, fixedValue, duration);
            }
        }
    }
}
