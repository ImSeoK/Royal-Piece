namespace Chess.Core
{
    public class FixedStatBuff
    {
        public StatType statType;
        public int fixedValue;
        public int remainingTurns;

        public FixedStatBuff(StatType statType, int value, int turns)
        {
            this.statType = statType;
            this.fixedValue = value;
            this.remainingTurns = turns;
        }
    }
}
