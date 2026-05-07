using UnityEngine;
using System;

namespace Chess.Core
{
    [Serializable]
    public class ActiveBuff
    {
        public StatType statType;
        public int amount;
        public int remainingTurns;

        public ActiveBuff(StatType statType, int amount, int remainingTurns)
        {
            this.statType = statType;
            this.amount = amount;
            this.remainingTurns = remainingTurns;
        }
    }
}