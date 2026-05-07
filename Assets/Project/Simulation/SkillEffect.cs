using UnityEngine;
using System.Collections.Generic;
using Chess.Core;

namespace Chess.Simulation
{
    public abstract class SkillEffect
    {
        public abstract void Execute(UnitState caster, List<UnitState> targets, BoardState board);
    }
}