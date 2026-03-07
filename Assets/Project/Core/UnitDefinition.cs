using UnityEngine;
using System.Collections.Generic;

namespace Chess.Core
{
    public enum UnitRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    [CreateAssetMenu(fileName = "NewUnit", menuName = "Chess/Unit Definition")]
    public class UnitDefinition : ScriptableObject
    {
        [Header("�⺻ ����")]
        public string unitName;
        public Sprite icon;
        public Sprite enhancedIcon;     

        [Header("�⺻ ����")]
        public int maxHP;
        public int attackPower;
        public int speed;

        [Header("�̵� �Ӽ�")]
        public MovementAttribute moveAttributes;

        [Header("Ư�� �Ӽ�")]
        public bool isKing;
        public bool isPawn;

        [Header("��í ���")]
        public UnitRarity rarity = UnitRarity.Common;

        [Header("��ų")]
        public SkillDefinition activeSkill;
        public List<SkillDefinition> passiveSkills = new List<SkillDefinition>();

        [Header("강화 수치")]
        public int hpEnhance ;   // 레벨당 HP 증가량
        public int atkEnhance ;    // 레벨당 공격력 증가량
        public int spdEnhance ;    // 레벨당 속도 증가량

        [Header("강화 설정")]
        public int enhanceGoldCost = 500;       
        public int enhanceMaterialCount         
        {
            get
            {
                switch (rarity)
                {
                    case UnitRarity.Common: return 3;
                    case UnitRarity.Rare: return 2;
                    case UnitRarity.Epic: return 2;
                    case UnitRarity.Legendary: return 1;
                    default: return 3;
                }
            }
        }

        // ��ȭ ���� ���� ���� ��� (5% per level)
        public int GetEnhancedHP(int level) => maxHP + hpEnhance * level;
        public int GetEnhancedAttack(int level) => attackPower + atkEnhance * level;
        public int GetEnhancedSpeed(int level) => speed + spdEnhance * level;

        // +10 ���� ��ȭ ��������Ʈ
        public Sprite GetIcon(int level) => (level >= 10 && enhancedIcon != null) ? enhancedIcon : icon;
    }
}