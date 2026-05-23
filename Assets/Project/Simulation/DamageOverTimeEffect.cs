using System.Collections.Generic;
using Chess.Core;

namespace Chess.Simulation
{
    public class DamageOverTimeEffect : SkillEffect
    {
        private int damagePerTurn;
        private int duration;

        public DamageOverTimeEffect(int damage, int duration)
        {
            this.damagePerTurn = damage;
            this.duration = duration;
        }

        public override void Execute(UnitState caster, List<UnitState> targets, BoardState board)
        {
            foreach (var target in targets)
            {
                if (target.IsAlive)
                    target.ApplyDamageOverTime(damagePerTurn, duration);
            }
        }
    }
}
