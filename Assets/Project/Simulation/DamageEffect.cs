using UnityEngine;
using System.Collections.Generic;
using Chess.Core;

namespace Chess.Simulation
{
    public class DamageEffect : SkillEffect
    {
        private int power;

        public DamageEffect(int power)
        {
            this.power = power;
        }

        public override void Execute(UnitState caster, List<UnitState> targets, BoardState board)
        {
            foreach (var target in targets)
            {
                if (target.IsAlive)
                    target.TakeDamage(power);
            }
        }
    }
}