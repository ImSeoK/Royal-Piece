using UnityEngine;
using System;

namespace Chess.Core
{
    [Serializable]
    public class ActiveBuff
    {
        public StatType statType;
        public int amount;
        public int remainingTurns; // -1 = permanent

        public bool IsPermanent => remainingTurns < 0;

        public ActiveBuff(StatType statType, int amount, int remainingTurns)
        {
            this.statType = statType;
            this.amount = amount;
            this.remainingTurns = remainingTurns;
        }
    }
}