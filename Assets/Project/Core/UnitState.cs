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
        public List<DamageOverTime> damageOverTimes = new List<DamageOverTime>();
        public List<FixedStatBuff> fixedBuffs = new List<FixedStatBuff>();

        public bool IsAlive => currentHP > 0;
        public bool IsMovementLocked => activeBuffs.Exists(b => b.statType == StatType.MovementLock);
        public int extraActionsAvailable = 0;
        public bool firstAttackUsed = false; // 3-2: 게임당 1회 한정 패시브 추적
        public bool HasExtraAction => extraActionsAvailable > 0;
        public void GrantExtraAction() { extraActionsAvailable++; }
        public void ConsumeExtraAction() { if (extraActionsAvailable > 0) extraActionsAvailable--; }
        public int enhanceLevel;

        private BoardState board;
        public void SetBoardReference(BoardState b) { board = b; }

        // Always 패시브 캐시 (dirty flag 방식)
        private Dictionary<StatType, int> _alwaysCache = new Dictionary<StatType, int>();
        private bool _isDirty = true;
        public void InvalidateCache() { _isDirty = true; }

        public UnitState(int id, UnitDefinition def, Vector2Int pos, int owner, int enhanceLevel = 0)
        {
            this.id = id;
            this.definition = def;
            this.enhanceLevel = enhanceLevel;
            this.currentHP = def.GetEnhancedHP(enhanceLevel);
            this.position = pos;
            this.ownerID = owner;
        }

        public void TakeDamage(int damage)
        {
            currentHP -= Mathf.Max(0, damage);
            if (currentHP < 0) currentHP = 0;
        }

        public int GetDamageReceivedModifier()
        {
            int modifier = 0;
            foreach (var buff in activeBuffs)
                if (buff.statType == StatType.DamageReceived)
                    modifier += buff.amount;
            modifier += GetAlwaysModifier(StatType.DamageReceived);
            return modifier;
        }

        // Always 패시브 보정값 반환 — dirty 시에만 전체 재계산
        private int GetAlwaysModifier(StatType statType)
        {
            if (board == null) return 0;
            if (_isDirty) RebuildAlwaysCache();
            return _alwaysCache.TryGetValue(statType, out int val) ? val : 0;
        }

        // 보드 전체를 순회해 모든 StatType의 Always 보정값을 한 번에 계산
        private void RebuildAlwaysCache()
        {
            _alwaysCache.Clear();
            foreach (var other in board.GetAllUnits())
            {
                if (!other.IsAlive) continue;
                if (other.definition.passiveSkills == null) continue;
                foreach (var skill in other.definition.passiveSkills)
                {
                    if (skill == null) continue;
                    if (skill.trigger != SkillTrigger.Always) continue;
                    if (skill.effectType != SkillEffectType.Buff &&
                        skill.effectType != SkillEffectType.Debuff) continue;
                    // 자기 자신 스킬은 Self 타겟팅만 허용 (non-Self는 다른 유닛 오라 용)
                    if (other == this && skill.auraRange != SkillAuraRange.Self) continue;
                    if (!other.IsHPInRange(skill.hpConditionMin / 100f, skill.hpConditionMax / 100f)) continue;
                    if (!IsInAuraRange(other, skill)) continue;

                    bool isBuff = skill.effectType == SkillEffectType.Buff;
                    bool sameTeam = ownerID == other.ownerID;
                    if (isBuff && !sameTeam) continue;
                    // self-targeting debuff(auraRange=Self)은 자기 자신에게 적용 허용 (3-3 제피로스)
                    if (!isBuff && sameTeam && skill.auraRange != SkillAuraRange.Self) continue;

                    // SPD 비교 조건 (3-3): 시전자 SPD가 모든 적보다 높아야 함
                    if (skill.requiresHighestSpeed && !board.HasHighestSpeedAgainstEnemies(other)) continue;

                    int delta = isBuff ? skill.power : -skill.power;
                    if (!_alwaysCache.ContainsKey(skill.buffStatType))
                        _alwaysCache[skill.buffStatType] = 0;
                    _alwaysCache[skill.buffStatType] += delta;
                }
            }
            _isDirty = false;
        }

        private bool IsInAuraRange(UnitState caster, SkillDefinition skill)
        {
            switch (skill.auraRange)
            {
                case SkillAuraRange.Self:
                    return this == caster;
                case SkillAuraRange.AllAllies:
                    return ownerID == caster.ownerID;
                case SkillAuraRange.SameRow:
                    return position.y == caster.position.y;
                case SkillAuraRange.Adjacent:
                    int dx = Mathf.Abs(position.x - caster.position.x);
                    int dy = Mathf.Abs(position.y - caster.position.y);
                    return dx <= 1 && dy <= 1 && (dx + dy > 0);
                case SkillAuraRange.LineOfSight:
                    return board != null && board.GetUnitsInLine(caster.position).Contains(this);
                case SkillAuraRange.Attacker:
                    return false; // Always 캐시에서는 미사용 (OnDamaged contextUnit으로만 처리)
                default:
                    return false;
            }
        }

        // DEF 적용 후 DamageReceived 버프까지 반영한 최종 피해량
        public int CalculateFinalDamage(int incomingDamage)
        {
            int afterDef = incomingDamage - GetDefense();
            int afterModifier = afterDef + GetDamageReceivedModifier();
            return Mathf.Max(0, afterModifier);
        }

        // HP 비율 (0.0 ~ 1.0)
        public float GetHPPercent()
        {
            int maxHP = definition.GetEnhancedHP(enhanceLevel);
            if (maxHP <= 0) return 0f;
            return (float)currentHP / maxHP;
        }

        // HP 비율이 [min, max] 범위 내인지 (0.0 ~ 1.0 기준)
        public bool IsHPInRange(float min, float max)
        {
            float pct = GetHPPercent();
            return pct >= min && pct <= max;
        }

        public void Heal(int amount)
        {
            currentHP += amount;
            int maxHP = definition.GetEnhancedHP(enhanceLevel);
            if (currentHP > maxHP)
                currentHP = maxHP;
        }

        public void ApplyBuff(ActiveBuff buff)
        {
            activeBuffs.Add(buff);
        }

        public void ApplyDamageOverTime(int damage, int turns)
        {
            damageOverTimes.Add(new DamageOverTime(damage, turns));
        }

        // 3-4: 스탯 고정값 적용 (같은 타입 기존 버프 교체)
        public void ApplyFixedBuff(StatType statType, int value, int turns)
        {
            fixedBuffs.RemoveAll(b => b.statType == statType);
            fixedBuffs.Add(new FixedStatBuff(statType, value, turns));
            InvalidateCache();
        }

        public int? GetFixedStat(StatType statType)
        {
            foreach (var buff in fixedBuffs)
                if (buff.statType == statType)
                    return buff.fixedValue;
            return null;
        }

        public void TickFixedBuffs()
        {
            for (int i = fixedBuffs.Count - 1; i >= 0; i--)
            {
                fixedBuffs[i].remainingTurns--;
                if (fixedBuffs[i].remainingTurns <= 0)
                    fixedBuffs.RemoveAt(i);
            }
            if (fixedBuffs.Count == 0) InvalidateCache();
        }

        // Apply DoT damage for this turn, decrement remaining turns, remove expired.
        // Returns total damage dealt (0 if no DoT active).
        public int TickDamageOverTime()
        {
            int totalDamage = 0;
            for (int i = damageOverTimes.Count - 1; i >= 0; i--)
            {
                var dot = damageOverTimes[i];
                TakeDamage(dot.damagePerTurn);
                totalDamage += dot.damagePerTurn;
                dot.remainingTurns--;
                if (dot.remainingTurns <= 0)
                    damageOverTimes.RemoveAt(i);
            }
            return totalDamage;
        }

        // Decrement buff duration each turn, remove expired buffs (permanent buffs skipped)
        public void TickBuffs()
        {
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                if (activeBuffs[i].IsPermanent) continue;
                activeBuffs[i].remainingTurns--;
                if (activeBuffs[i].remainingTurns <= 0)
                    activeBuffs.RemoveAt(i);
            }
            TickFixedBuffs();
        }

        // Return stat values with enhance level and buffs applied
        public int GetAttack()
        {
            int atk = definition.GetEnhancedAttack(enhanceLevel);
            foreach (var buff in activeBuffs)
                if (buff.statType == StatType.ATK)
                    atk += buff.amount;
            atk += GetAlwaysModifier(StatType.ATK);
            return Mathf.Max(0, atk);
        }

        public int GetSpeed()
        {
            // 고정값이 있으면 무조건 그 값 반환 (3-4)
            var fixedSpd = GetFixedStat(StatType.SPD);
            if (fixedSpd.HasValue) return Mathf.Max(0, fixedSpd.Value);

            int spd = definition.GetEnhancedSpeed(enhanceLevel);
            foreach (var buff in activeBuffs)
                if (buff.statType == StatType.SPD)
                    spd += buff.amount;
            spd += GetAlwaysModifier(StatType.SPD);
            return Mathf.Max(0, spd);
        }

        public int GetDefense()
        {
            int def = Mathf.FloorToInt(definition.GetEnhancedDefense(enhanceLevel));
            foreach (var buff in activeBuffs)
                if (buff.statType == StatType.DEF)
                    def += buff.amount;
            def += GetAlwaysModifier(StatType.DEF);
            return Mathf.Max(0, def);
        }
    }
}