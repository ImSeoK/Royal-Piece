using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Chess.Core;

namespace Chess.Simulation
{
    public static class SkillExecutor
    {
        // Active skill - Range type, no caster, targets entire board
        public static void ExecuteActive(SkillDefinition skill, BoardState board)
        {
            if (skill == null) return;

            // 3-5: ClearBuffs는 보드 전체 유닛을 대상으로 함
            if (skill.effectType == SkillEffectType.ClearBuffs)
            {
                var allAlive = board.GetAllUnits().Where(u => u.IsAlive).ToList();
                new ClearBuffsEffect().Execute(null, allAlive, board);
                return;
            }

            SkillEffect effect = CreateEffect(skill);
            if (effect == null) return;

            List<UnitState> targets = SelectActiveTargets(skill, board);
            effect.Execute(null, targets, board);
        }

        // Active skill - Single type, player selects target directly
        // caster: 스킬을 시전한 유닛 (Pull 등 방향 계산에 필요, 없으면 null)
        public static void ExecuteActiveSingle(SkillDefinition skill, UnitState target, BoardState board, UnitState caster = null)
        {
            if (skill == null || target == null) return;

            SkillEffect effect = CreateEffect(skill);
            if (effect == null) return;

            effect.Execute(caster, new List<UnitState> { target }, board);
        }

        // 패시브 스킬 — auraRange 기반 타겟 선택 후 효과 실행 (모든 패시브 트리거 공통)
        // contextUnit: OnDamaged 시전자 등 트리거 컨텍스트 유닛 (Attacker 범위 타겟팅용)
        // allowDeadCaster: OnDeath 트리거처럼 사망 유닛이 스킬을 발동해야 하는 경우 true
        public static void ExecutePassive(SkillDefinition skill, UnitState caster, BoardState board, UnitState contextUnit = null, bool allowDeadCaster = false)
        {
            if (skill == null || caster == null) return;
            if (!allowDeadCaster && !caster.IsAlive) return;

            if (!caster.IsHPInRange(skill.hpConditionMin / 100f, skill.hpConditionMax / 100f)) return;

            List<UnitState> targets = GetAuraTargets(skill, caster, board, contextUnit);
            if (targets.Count == 0) return;

            if (skill.selectRandomTarget)
            {
                var pool = targets.Where(u => !u.definition.isKing && !u.definition.isPawn).ToList();
                if (pool.Count == 0) return;
                targets = new List<UnitState> { pool[UnityEngine.Random.Range(0, pool.Count)] };
            }

            SkillEffect effect = CreateEffect(skill);
            if (effect == null) return;

            effect.Execute(caster, targets, board);
        }

        // auraRange 기반 타겟 목록 반환
        // contextUnit: Attacker 등 범위 케이스에서 사용 (추후 SkillAuraRange.Attacker 추가 시 활성화)
        private static List<UnitState> GetAuraTargets(SkillDefinition skill, UnitState caster, BoardState board, UnitState contextUnit = null)
        {
            bool wantAlly = skill.effectType == SkillEffectType.Buff
                         || skill.effectType == SkillEffectType.Heal
                         || skill.effectType == SkillEffectType.GrantExtraAction;
            int targetOwner = wantAlly ? caster.ownerID : 1 - caster.ownerID;

            switch (skill.auraRange)
            {
                case SkillAuraRange.Self:
                    return new List<UnitState> { caster };
                case SkillAuraRange.AllAllies:
                    return board.GetAllUnits().Where(u => u.IsAlive && u.ownerID == targetOwner).ToList();
                case SkillAuraRange.SameRow:
                    return board.GetUnitsInRow(caster.position.y, targetOwner);
                case SkillAuraRange.Adjacent:
                    return board.GetAdjacentUnits(caster.position, targetOwner);
                case SkillAuraRange.LineOfSight:
                    return board.GetUnitsInLine(caster.position, targetOwner);
                case SkillAuraRange.Attacker:
                    // OnDamaged에서 나를 공격한 유닛 (contextUnit)을 대상으로 함
                    if (contextUnit != null && contextUnit.IsAlive)
                        return new List<UnitState> { contextUnit };
                    return new List<UnitState>();
                default:
                    return new List<UnitState>();
            }
        }

        // Active skill target selection - search entire board
        private static List<UnitState> SelectActiveTargets(SkillDefinition skill, BoardState board)
        {
            var allUnits = board.GetAllUnits().Where(u => u.IsAlive).ToList();

            switch (skill.targetType)
            {
                case SkillTargetType.RangeEnemy:
                    return allUnits.Where(u => u.ownerID == 1).ToList();

                case SkillTargetType.RangeAlly:
                case SkillTargetType.Self:
                    return allUnits.Where(u => u.ownerID == 0).ToList();

                default:
                    return new List<UnitState>();
            }
        }

        private static SkillEffect CreateEffect(SkillDefinition skill)
        {
            switch (skill.effectType)
            {
                case SkillEffectType.Damage:
                    return new DamageEffect(skill.power);

                case SkillEffectType.Heal:
                    return new HealEffect(skill.power);

                case SkillEffectType.Buff:
                    return new BuffEffect(skill.buffStatType, skill.power, skill.buffDuration);

                case SkillEffectType.Debuff:
                    return new BuffEffect(skill.buffStatType, -skill.power, skill.buffDuration);

                case SkillEffectType.Move:
                    return new MoveEffect(skill.power);

                case SkillEffectType.LockMovement:
                    return new BuffEffect(StatType.MovementLock, 0, skill.buffDuration);

                case SkillEffectType.DamageOverTime:
                    return new DamageOverTimeEffect(skill.power, skill.buffDuration);

                case SkillEffectType.GrantExtraAction:
                    return new GrantExtraActionEffect();

                case SkillEffectType.Pull:
                    return new PullEffect();

                case SkillEffectType.ClearBuffs:
                    return new ClearBuffsEffect();

                case SkillEffectType.FixedBuff:
                    return new FixedBuffEffect(skill.buffStatType, skill.power, skill.buffDuration);

                case SkillEffectType.ClearTargetBuffs:
                    return new ClearTargetBuffsEffect();

                default:
                    return null;
            }
        }
    }
}