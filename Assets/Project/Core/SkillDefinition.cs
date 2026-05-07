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
    }

    [CreateAssetMenu(fileName = "NewSkill", menuName = "Chess/Skill Definition")]
    public class SkillDefinition : ScriptableObject
    {
        [Header("�⺻ ����")]
        public string skillName;
        public string description;
        public Sprite icon;

        [Header("��ų �з�")]
        public SkillType skillType;
        public SkillTrigger trigger;

        [Header("ȿ��")]
        public SkillTargetType targetType;
        public SkillEffectType effectType;
        public int power;

        [Header("���� ����")]
        public StatType buffStatType = StatType.ATK;
        public int buffDuration = 2;

        [Header("��Ƽ�� ����")]
        public int maxUseCount = 2;

        [Header("����Ʈ (���� ����)")]
        public GameObject castEffectPrefab;
        public GameObject hitEffectPrefab;
        public AnimationClip castAnimation;
    }
}