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

            SkillEffect effect = CreateEffect(skill);
            if (effect == null) return;

            List<UnitState> targets = SelectActiveTargets(skill, board);
            effect.Execute(null, targets, board);
        }

        // Active skill - Single type, player selects target directly
        public static void ExecuteActiveSingle(SkillDefinition skill, UnitState target, BoardState board)
        {
            if (skill == null || target == null) return;

            SkillEffect effect = CreateEffect(skill);
            if (effect == null) return;

            effect.Execute(null, new List<UnitState> { target }, board);
        }

        // Passive skill - caster is the unit, valid moves define range
        public static void ExecutePassive(SkillDefinition skill, UnitState caster, BoardState board)
        {
            if (skill == null || caster == null || !caster.IsAlive) return;

            List<Vector2Int> validMoves = MovementResolver.GetValidMoves(caster, board);

            SkillTarget targetSelector = CreateTarget(skill);
            SkillEffect effect = CreateEffect(skill);

            if (targetSelector == null || effect == null) return;

            List<UnitState> targets = targetSelector.SelectTargets(caster, board, validMoves);
            effect.Execute(caster, targets, board);
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

        private static SkillTarget CreateTarget(SkillDefinition skill)
        {
            switch (skill.targetType)
            {
                case SkillTargetType.SingleEnemy:
                    return new TargetSingle(targetAlly: false);

                case SkillTargetType.SingleAlly:
                case SkillTargetType.Self:
                    return new TargetSingle(targetAlly: true);

                case SkillTargetType.RangeEnemy:
                    return new TargetRange(targetAlly: false);

                case SkillTargetType.RangeAlly:
                    return new TargetRange(targetAlly: true);

                default:
                    return null;
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

                default:
                    return null;
            }
        }
    }
}