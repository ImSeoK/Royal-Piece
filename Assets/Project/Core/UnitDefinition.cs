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
        public int defense;

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
        public float hpEnhance;    // 레벨당 HP 증가량
        public float atkEnhance;   // 레벨당 공격력 증가량
        public float spdEnhance;   // 레벨당 속도 증가량
        public float defEnhance;   // 레벨당 방어력 증가량

        [Header("인게임 표시")]
        public float displayScale = 0.85f;  // 타일 크기 대비 기물 표시 비율 (1.0 = 타일과 동일)

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
        public int GetEnhancedHP(int level)      => Mathf.FloorToInt(maxHP + hpEnhance * level);
        public int GetEnhancedAttack(int level)  => Mathf.FloorToInt(attackPower + atkEnhance * level);
        public int GetEnhancedSpeed(int level)   => Mathf.FloorToInt(speed + spdEnhance * level);
        public float GetEnhancedDefense(int level) => defense + defEnhance * level;

        // +10 ���� ��ȭ ��������Ʈ
        public Sprite GetIcon(int level) => (level >= 10 && enhancedIcon != null) ? enhancedIcon : icon;
    }
}