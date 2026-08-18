using UnityEngine;

namespace Chess.Core
{
    public enum SkillType
    {
        Active,
        Passive,
    }

    public enum SkillTrigger
    {
        None,
        OnAttack,
        OnDamaged,
        OnMove,
        OnAdjacent,
        OnDeath,
        Always,       // 상시 효과 (능력치 계산 시 자동 반영)
        OnTurnStart,  // 매 턴 시작 시 발동
        OnTurnEnd,    // 매 턴 종료 시 발동
    }

    // Always/턴 기반 패시브의 영향 범위
    public enum SkillAuraRange
    {
        Self,         // 자기 자신에게만
        AllAllies,    // 모든 아군
        SameRow,      // 같은 가로줄 (y 동일)
        Adjacent,     // 인접 8방향 1칸
        LineOfSight,  // 직선 8방향 (첫 번째 유닛까지)
        Attacker,     // 나를 공격한 유닛 (OnDamaged contextUnit)
        AttackRange,  // 이 유닛의 실제 공격 가능 범위
    }

    public enum SkillTargetType
    {
        Self,
        SingleEnemy,
        SingleAlly,
        RangeEnemy,
        RangeAlly,
    }

    public enum SkillEffectType
    {
        Damage,
        Heal,
        Buff,
        Debuff,
        Move,
        LockMovement,
        DamageOverTime,
        GrantExtraAction,
        Pull,          // 3-1: 강제 끌어당김
        ClearBuffs,    // 3-5: 광역 버프 제거
        FixedBuff,     // 3-4: 스탯 고정값 설정
        ClearTargetBuffs, // 3-6: 단일 대상 버프 제거
        FixedDamage,   // 방어력 무시 고정 피해
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
        public SkillTrigger trigger;

        [Header("효과")]
        public SkillTargetType targetType;
        public SkillEffectType effectType;
        public int power;

        [Header("버프 설정")]
        public StatType buffStatType = StatType.ATK;
        public int buffDuration = 2;

        [Header("Always/턴 트리거 설정")]
        public SkillAuraRange auraRange;
        [Range(0f, 100f)] public float hpConditionMin = 0f;
        [Range(0f, 100f)] public float hpConditionMax = 100f;
        public bool selectRandomTarget = false;

        [Header("액티브 전용")]
        public int maxUseCount = 2;

        [Header("게임당 1회 한정 (3-2)")]
        public bool isOneTimeUse = false;
        [Range(1f, 5f)] public float damageMultiplier = 1.0f;

        [Header("SPD 비교 조건 (3-3)")]
        public bool requiresHighestSpeed = false;

        [Header("보조 효과 (선택)")]
        public bool hasSecondaryEffect = false;
        public SkillEffectType secondaryEffectType;
        public int secondaryPower;
        public StatType secondaryBuffStatType = StatType.ATK;
        public int secondaryBuffDuration = 1;

        [Header("이펙트")]
        public GameObject castEffectPrefab;
        public GameObject hitEffectPrefab;
        public AnimationClip castAnimation;

        [Header("사운드")]
        public string castSoundId;   // 시전 시 재생할 AudioManager 사운드 ID
        public string hitSoundId;    // 피격 시 재생할 AudioManager 사운드 ID
    }
}