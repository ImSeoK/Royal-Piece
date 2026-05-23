using System.Collections.Generic;
using Chess.Core;

namespace Chess.Simulation
{
    public class GrantExtraActionEffect : SkillEffect
    {
        public override void Execute(UnitState caster, List<UnitState> targets, BoardState board)
        {
            foreach (var target in targets)
            {
                if (target.IsAlive)
                    target.GrantExtraAction();
            }
        }
    }
}
