using System;

namespace Chess.Core
{
    [Serializable]
    public class DamageOverTime
    {
        public int damagePerTurn;
        public int remainingTurns;

        public DamageOverTime(int damage, int turns)
        {
            damagePerTurn = damage;
            remainingTurns = turns;
        }
    }
}
