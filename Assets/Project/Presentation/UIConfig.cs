using UnityEngine;
using Chess.Core;

namespace Chess.Presentation
{
    public static class UIConfig
    {
        // 팝업 폰트 크기
        public const int DamagePopupSize = 80;
        public const int BuffPopupSize   = 40;

        // 팝업 애니메이션
        public const float PopupLifetime     = 0.8f;
        public const float PopupMoveDistance = 0.5f;
        public static readonly AnimationCurve PopupAlphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        // 팝업 색상
        public static readonly Color DamageColor = new Color(1f,   0.2f, 0.2f);  // 빨강 (피해)
        public static readonly Color BuffColor   = new Color(0.3f, 1f,   0.3f);  // 초록
        public static readonly Color DebuffColor = new Color(1f,   0.3f, 0.3f);  // 빨강
        public static readonly Color ShieldColor = new Color(0.3f, 0.7f, 1f);    // 파랑 (피해감소)
        public static readonly Color HealColor   = new Color(0.4f, 1f,   0.5f);  // 밝은 초록
        public static readonly Color StatusColor = new Color(1f,   0.6f, 0.2f);  // 주황 (봉쇄 등)
        public static readonly Color DotColor    = new Color(0.7f, 0.3f, 1f);    // 보라 (독)

        // 상태이상 여부 — 수치 버프/디버프가 아닌 상태 효과
        // 새 상태이상 추가 시 여기만 수정
        public static bool IsStatusEffect(StatType type)
        {
            return type == StatType.MovementLock;
        }

        public static string GetStatLabel(StatType stat)
        {
            switch (stat)
            {
                case StatType.ATK:            return "ATK";
                case StatType.DEF:            return "DEF";
                case StatType.SPD:            return "SPD";
                case StatType.HP:             return "HP";
                case StatType.DamageReceived: return "피해";
                case StatType.MovementLock:   return "봉쇄";
                default:                      return stat.ToString();
            }
        }

        // 스킬로부터 팝업 텍스트와 색상을 반환. 팝업이 필요 없으면 false 반환.
        // Damage/Pull/Move 등 HP delta로 처리되는 effectType은 false 반환
        public static bool TryGetBuffPopup(SkillDefinition skill, out string text, out Color color)
        {
            switch (skill.effectType)
            {
                case SkillEffectType.Buff:
                    text  = skill.buffStatType == StatType.DamageReceived
                        ? "-" + skill.power + " 피해"
                        : "+" + skill.power + " " + GetStatLabel(skill.buffStatType);
                    color = skill.buffStatType == StatType.DamageReceived ? ShieldColor : BuffColor;
                    return true;

                case SkillEffectType.Debuff:
                    text  = skill.buffStatType == StatType.DamageReceived
                        ? "+" + skill.power + " 피해"
                        : "-" + skill.power + " " + GetStatLabel(skill.buffStatType);
                    color = DebuffColor;
                    return true;

                case SkillEffectType.FixedBuff:
                    text  = "+" + skill.power + " " + GetStatLabel(skill.buffStatType);
                    color = BuffColor;
                    return true;

                case SkillEffectType.Heal:
                    text  = "+" + skill.power + " HP";
                    color = HealColor;
                    return true;

                case SkillEffectType.LockMovement:
                    text  = "봉쇄";
                    color = StatusColor;
                    return true;

                case SkillEffectType.DamageOverTime:
                    text  = "독 " + skill.power;
                    color = DotColor;
                    return true;

                case SkillEffectType.GrantExtraAction:
                    text  = "추가행동";
                    color = BuffColor;
                    return true;

                case SkillEffectType.ClearBuffs:
                case SkillEffectType.ClearTargetBuffs:
                    text  = "해제";
                    color = StatusColor;
                    return true;

                default:
                    text  = "";
                    color = Color.white;
                    return false;
            }
        }
    }
}
