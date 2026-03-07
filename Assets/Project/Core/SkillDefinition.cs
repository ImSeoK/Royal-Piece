using UnityEngine;

namespace Chess.Core
{
    public enum SkillType
    {
        Active,   // 액티브 (Epic/Legendary 전용, 플레이어가 직접 시전)
        Passive,  // 패시브 (모든 등급, 자동 발동)
    }

    public enum SkillTrigger
    {
        None,           // 해당 없음 (액티브)
        Always,         // 상시 적용
        OnAttack,       // 공격 시
        OnDamaged,      // 피격 시
        OnMove,         // 이동 시
        OnAdjacent,     // 인접 시
        OnDeath,        // 사망 시
    }

    public enum SkillTargetType
    {
        Self,
        SingleEnemy,
        AreaEnemy,
        SingleAlly,
        AreaAlly,
    }

    public enum SkillEffectType
    {
        Damage,
        Heal,
        Buff,
        Debuff,
        Move,
    }

    [CreateAssetMenu(fileName = "NewSkill", menuName = "Chess/Skill Definition")]
    public class SkillDefinition : ScriptableObject
    {
        [Header("기본 정보")]
        public string skillName;
        public string description;
        public Sprite icon;

        [Header("스킬 분류")]
        public SkillType skillType;
        public SkillTrigger trigger;        // 패시브 발동 조건 (액티브면 None)

        [Header("효과")]
        public SkillTargetType targetType;
        public SkillEffectType effectType;
        public int power;                   // 피해/회복량
        public int range;                   // 사거리 (셀 기준)

        [Header("액티브 전용")]
        public int maxUseCount = 2;         // 게임당 사용 횟수 (Epic/Legendary 공통 2회)

        [Header("이펙트 (추후 연동)")]
        public GameObject castEffectPrefab;
        public GameObject hitEffectPrefab;
        public AnimationClip castAnimation;
    }
}