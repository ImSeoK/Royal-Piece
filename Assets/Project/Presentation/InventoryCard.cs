using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Chess.Core;

namespace Chess.Presentation
{
    public class InventoryCard : MonoBehaviour
    {
        [Header("UI")]
        public Image cardBackground;
        public Image unitImage;
        public TextMeshProUGUI unitNameText;
        public TextMeshProUGUI attrText;
        public TextMeshProUGUI enhanceBadgeText;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI atkText;
        public TextMeshProUGUI spdText;
        public TextMeshProUGUI defText;

        [Header("��޺� ī�� ��������Ʈ")]
        public Sprite cardCommon;
        public Sprite cardRare;
        public Sprite cardEpic;
        public Sprite cardLegendary;

        [Header("��޺� ���� ��������Ʈ")]
        public Sprite badgeCommon;
        public Sprite badgeRare;
        public Sprite badgeEpic;
        public Sprite badgeLegendary;

        public UnitDefinition unitDefinition { get; private set; }
        public OwnedUnitInstance ownedInstance { get; private set; }

        private System.Action<InventoryCard> onClickCallback;

        void Awake()
        {
            Button btn = GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => onClickCallback?.Invoke(this));
        }

        // OwnedUnitInstance ���� ���� (�κ��丮/��Ŀ���� ���)
        public void Initialize(UnitDefinition unit, OwnedUnitInstance inst, System.Action<InventoryCard> onClick = null)
        {
            ownedInstance = inst;
            Initialize(unit, onClick);

            // ��ȭ ���� ǥ�� (�׻� ǥ��)
            if (enhanceBadgeText != null && inst != null)
            {
                enhanceBadgeText.gameObject.SetActive(true);
                enhanceBadgeText.text = $"+{inst.enhanceLevel}";
                enhanceBadgeText.color = GetLevelColor(inst.enhanceLevel);

                Image badgeImage = enhanceBadgeText.GetComponentInParent<Image>();
                if (badgeImage != null)
                    badgeImage.sprite = GetBadgeSpriteByLevel(inst.enhanceLevel);
            }

            // ��ȭ�� ���� �ݿ�
            if (inst != null)
            {
                if (hpText != null) hpText.text = $"HP  {unit.GetEnhancedHP(inst.enhanceLevel)}";
                if (atkText != null) atkText.text = $"공격  {unit.GetEnhancedAttack(inst.enhanceLevel)}";
                if (spdText != null) spdText.text = $"속도  {unit.GetEnhancedSpeed(inst.enhanceLevel)}";
                if (defText != null) defText.text = $"방어력  {unit.GetEnhancedDefense(inst.enhanceLevel):0.##}";
            }
        }

        // UnitDefinition�� �޴� ���� (������ ȣȯ)
        public void Initialize(UnitDefinition unit, System.Action<InventoryCard> onClick = null)
        {
            if (unit == null) return;
            onClickCallback = onClick;
            unitDefinition = unit;

            if (unitNameText != null) unitNameText.text = unit.unitName;
            if (attrText != null) attrText.text = GetAttrText(unit);
            if (hpText != null) hpText.text = $"HP  {unit.maxHP}";
            if (atkText != null) atkText.text = $"공격  {unit.attackPower}";
            if (spdText != null) spdText.text = $"속도  {unit.speed}";
            if (defText != null) defText.text = $"방어력  {unit.defense}";

            if (unitImage != null)
            {
                if (unit.icon != null) { unitImage.sprite = unit.icon; unitImage.color = Color.white; }
                else unitImage.color = Color.clear;
            }

            if (cardBackground == null) cardBackground = GetComponent<Image>();
            if (cardBackground != null) { cardBackground.sprite = GetRaritySprite(unit.rarity); cardBackground.color = Color.white; }

            // ���� �⺻ ��Ȱ��
            if (enhanceBadgeText != null) enhanceBadgeText.gameObject.SetActive(false);
        }

        public void SetHighlight(bool highlight)
        {
            if (cardBackground != null)
                cardBackground.color = highlight ? new Color(0.3f, 0.8f, 0.3f) : Color.white;
        }

        Sprite GetRaritySprite(UnitRarity rarity)
        {
            switch (rarity)
            {
                case UnitRarity.Common: return cardCommon;
                case UnitRarity.Rare: return cardRare;
                case UnitRarity.Epic: return cardEpic;
                case UnitRarity.Legendary: return cardLegendary;
                default: return cardCommon;
            }
        }

        Color GetLevelColor(int level)
        {
            if (level <= 3) return new Color(0.54f, 0.61f, 0.71f);
            if (level <= 6) return new Color(0.29f, 0.62f, 1.00f);
            if (level <= 8) return new Color(0.61f, 0.36f, 0.90f);
            return new Color(0.96f, 0.77f, 0.19f);
        }
        Sprite GetBadgeSpriteByLevel(int level)
        {
            if (level <= 3) return badgeCommon;
            if (level <= 6) return badgeRare;
            if (level <= 8) return badgeEpic;
            return badgeLegendary;
        }

        string GetAttrText(UnitDefinition unit)
        {
            if (unit.isKing) return "킹";
            if (unit.isPawn) return "폰";
            bool hasRook = (unit.moveAttributes & MovementAttribute.Rook) != 0;
            bool hasBishop = (unit.moveAttributes & MovementAttribute.Bishop) != 0;
            bool hasKnight = (unit.moveAttributes & MovementAttribute.Knight) != 0;
            if (hasRook && hasBishop) return "퀸";
            if (hasRook) return "룩";
            if (hasBishop) return "비숍";
            if (hasKnight) return "나이트";
            return "";
        }
    }
}