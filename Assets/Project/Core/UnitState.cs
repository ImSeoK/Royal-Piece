using System.Collections.Generic;
using UnityEngine;

namespace Chess.Core
{
    public class UnitState
    {
        public int id;
        public int currentHP;
        public Vector2Int position;
        public int ownerID;
        public UnitDefinition definition;
        public List<ActiveBuff> activeBuffs = new List<ActiveBuff>();

        public bool IsAlive => currentHP > 0;

        public UnitState(int id, UnitDefinition def, Vector2Int pos, int owner)
        {
            this.id = id;
            this.definition = def;
            this.currentHP = def.maxHP;
            this.position = pos;
            this.ownerID = owner;
        }

        public void TakeDamage(int damage)
        {
            int reduced = Mathf.Max(0, damage - GetDefense());
            currentHP -= reduced;
            if (currentHP < 0) currentHP = 0;
        }

        public void Heal(int amount)
        {
            currentHP += amount;
            if (currentHP > definition.maxHP)
                currentHP = definition.maxHP;
        }

        public void ApplyBuff(ActiveBuff buff)
        {
            activeBuffs.Add(buff);
        }

        // Decrement buff duration each turn, remove expired buffs
        public void TickBuffs()
        {
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                activeBuffs[i].remainingTurns--;
                if (activeBuffs[i].remainingTurns <= 0)
                    activeBuffs.RemoveAt(i);
            }
        }

        // Return stat values with buffs applied
        public int GetAttack()
        {
            int atk = definition.attackPower;
            foreach (var buff in activeBuffs)
                if (buff.statType == StatType.ATK)
                    atk += buff.amount;
            return Mathf.Max(0, atk);
        }

        public int GetSpeed()
        {
            int spd = definition.speed;
            foreach (var buff in activeBuffs)
                if (buff.statType == StatType.SPD)
                    spd += buff.amount;
            return Mathf.Max(0, spd);
        }

        public int GetDefense()
        {
            int def = definition.defense;
            foreach (var buff in activeBuffs)
                if (buff.statType == StatType.DEF)
                    def += buff.amount;
            return Mathf.Max(0, def);
        }
    }
}