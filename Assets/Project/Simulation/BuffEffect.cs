using UnityEngine;
using System.Collections.Generic;
using Chess.Core;

namespace Chess.Simulation
{
    public class BuffEffect : SkillEffect
    {
        private StatType statType;
        private int amount;
        private int duration;

        public BuffEffect(StatType statType, int amount, int duration)
        {
            this.statType = statType;
            this.amount = amount;
            this.duration = duration;
        }

        public override void Execute(UnitState caster, List<UnitState> targets, BoardState board)
        {
            foreach (var target in targets)
            {
                if (target.IsAlive)
                    target.ApplyBuff(new ActiveBuff(statType, amount, duration));
            }
        }
    }
}