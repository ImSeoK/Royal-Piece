using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Chess.Core;

namespace Chess.Presentation
{
    public class UnitDetailPopup : MonoBehaviour
    {
        [Header("�˾�")]
        public GameObject popupRoot;
        public Button closeButton;

        [Header("Ÿ�� �г�")]
        public Image targetPanelImage;
        public Image unitIconBG;
        public Image unitIcon;
        public Image levelBadgeImage;
        public TextMeshProUGUI levelBadgeText;
        public TextMeshProUGUI nameText;
        public Image rarityChipImage;          // ��޺� ��������Ʈ ��ü
        public TextMeshProUGUI rarityText;
        public TextMeshProUGUI attrText;

        [Header("����")]
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI atkText;
        public TextMeshProUGUI spdText;
        public TextMeshProUGUI defText;
        public RectTransform hpBarFill;
        public RectTransform atkBarFill;
        public RectTransform spdBarFill;
        public RectTransform defBarFill;

        [Header("��޺� ��������Ʈ")]
        public Sprite panelCommon;
        public Sprite panelRare;
        public Sprite panelEpic;
        public Sprite panelLegendary;
        public Sprite iconBGCommon;
        public Sprite iconBGRare;
        public Sprite iconBGEpic;
        public Sprite iconBGLegendary;
        public Sprite chipCommon;
        public Sprite chipRare;
        public Sprite chipEpic;
        public Sprite chipLegendary;
        public Sprite badgeCommon;
        public Sprite badgeRare;
        public Sprite badgeEpic;
        public Sprite badgeLegendary;

        [Header("��ų �׷�")]
        public Image activeSkillIcon;
        public TextMeshProUGUI activeSkillNameText;
        public TextMeshProUGUI activeSkillDescText;
        public Transform passiveSkillContainer;
        public GameObject skillItemPrefab;

        [Header("��ȭ ��ư")]
        public Button enhanceButton;

        private OwnedUnitInstance currentInstance;
        private System.Action<OwnedUnitInstance> onEnhanceCallback;
        public System.Action<OwnedUnitInstance> LastEnhanceCallback => onEnhanceCallback;

        void Awake()
        {
            if (popupRoot != null) popupRoot.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (enhanceButton != null) enhanceButton.onClick.AddListener(OnEnhanceClicked);
        }

        public void Open(OwnedUnitInstance inst, System.Action<OwnedUnitInstance> onEnhance = null)
        {
            currentInstance = inst;
            onEnhanceCallback = onEnhance;

            UnitDefinition def = PlayerInventory.Instance.GetDefinition(inst.unitName);
            if (def == null) return;

            if (popupRoot != null) popupRoot.SetActive(true);

            // ��޺� ��������Ʈ ��ü
            if (targetPanelImage != null) targetPanelImage.sprite = GetPanelSprite(def.rarity);
            if (unitIconBG != null) unitIconBG.sprite = GetIconBGSprite(def.rarity);
            if (rarityChipImage != null) rarityChipImage.sprite = GetChipSprite(def.rarity);
            if (rarityText != null) rarityText.color = GetRarityColor(def.rarity);

            // ������
            if (unitIcon != null) { unitIcon.sprite = def.GetIcon(inst.enhanceLevel); unitIcon.color = Color.white; }

            // ���� ���� (�׻� ǥ��)
            if (levelBadgeImage != null) { levelBadgeImage.gameObject.SetActive(true); levelBadgeImage.sprite = GetBadgeSpriteByLevel(inst.enhanceLevel); }
            if (levelBadgeText != null) { levelBadgeText.text = $"+{inst.enhanceLevel}"; levelBadgeText.color = GetLevelColor(inst.enhanceLevel); }

            // ����
            if (nameText != null) nameText.text = def.unitName;
            if (rarityText != null) rarityText.text = GetRarityText(def.rarity);
            if (attrText != null) attrText.text = GetAttrText(def);

            // ����
            int lv = inst.enhanceLevel;
            if (hpText != null) hpText.text = $"{def.GetEnhancedHP(lv)}";
            if (atkText != null) atkText.text = $"{def.GetEnhancedAttack(lv)}";
            if (spdText != null) spdText.text = $"{def.GetEnhancedSpeed(lv)}";
            if (defText != null) defText.text = $"{def.GetEnhancedDefense(lv)}";

            SetBar(hpBarFill, (float)def.GetEnhancedHP(lv) / def.GetEnhancedHP(10));
            SetBar(atkBarFill, (float)def.GetEnhancedAttack(lv) / def.GetEnhancedAttack(10));
            SetBar(spdBarFill, (float)def.GetEnhancedSpeed(lv) / def.GetEnhancedSpeed(10));
            if (def.GetEnhancedDefense(10) > 0)
                SetBar(defBarFill, (float)def.GetEnhancedDefense(lv) / def.GetEnhancedDefense(10));
            else
                SetBar(defBarFill, 0f);

            if (enhanceButton != null) enhanceButton.interactable = inst.enhanceLevel < 10;

            SetupSkills(def);
        }

        void SetupSkills(UnitDefinition def)
        {
            // ��Ƽ�� ��ų
            if (def.activeSkill != null)
            {
                if (activeSkillIcon != null) { activeSkillIcon.sprite = def.activeSkill.icon; activeSkillIcon.color = Color.white; }
                if (activeSkillNameText != null) activeSkillNameText.text = def.activeSkill.skillName;
                if (activeSkillDescText != null) activeSkillDescText.text = def.activeSkill.description;
            }
            else
            {
                if (activeSkillIcon != null) activeSkillIcon.color = Color.clear;
                if (activeSkillNameText != null) activeSkillNameText.text = "액티브 스킬 없음";
                if (activeSkillDescText != null) activeSkillDescText.text = "";
            }

            // �нú� ��ų ���� ����
            if (passiveSkillContainer != null)
            {
                foreach (Transform child in passiveSkillContainer) Destroy(child.gameObject);

                if (def.passiveSkills == null || def.passiveSkills.Count == 0)
                {
                    if (skillItemPrefab != null)
                    {
                        GameObject empty = Instantiate(skillItemPrefab, passiveSkillContainer);
                        TextMeshProUGUI[] texts = empty.GetComponentsInChildren<TextMeshProUGUI>();
                        if (texts.Length > 0) texts[0].text = "패시브 스킬 없음";
                        if (texts.Length > 1) texts[1].text = "";
                    }
                    return;
                }

                foreach (var skill in def.passiveSkills)
                {
                    if (skill == null) continue;
                    GameObject item = Instantiate(skillItemPrefab, passiveSkillContainer);
                    Image icon = item.GetComponentInChildren<Image>();
                    TextMeshProUGUI[] texts = item.GetComponentsInChildren<TextMeshProUGUI>();
                    if (icon != null && skill.icon != null) { icon.sprite = skill.icon; icon.color = Color.white; }
                    if (texts.Length > 0) texts[0].text = skill.skillName;
                    if (texts.Length > 1) texts[1].text = skill.description;
                }
            }
        }

        public void Close()
        {
            if (popupRoot != null) popupRoot.SetActive(false);
        }

        void OnEnhanceClicked()
        {
            Close();
            onEnhanceCallback?.Invoke(currentInstance);
        }

        void SetBar(RectTransform fill, float ratio)
        {
            if (fill == null) return;
            fill.anchorMin = new Vector2(0, 0);
            fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }

        Color GetLevelColor(int level)
        {
            if (level <= 3) return new Color(0.54f, 0.61f, 0.71f); // Common
            if (level <= 6) return new Color(0.29f, 0.62f, 1.00f); // Rare
            if (level <= 8) return new Color(0.61f, 0.36f, 0.90f); // Epic
            return new Color(0.96f, 0.77f, 0.19f);                  // Legendary
        }
        Color GetRarityColor(UnitRarity r)
        {
            switch (r)
            {
                case UnitRarity.Common: return new Color(0.54f, 0.61f, 0.71f);
                case UnitRarity.Rare: return new Color(0.29f, 0.62f, 1.00f);
                case UnitRarity.Epic: return new Color(0.61f, 0.36f, 0.90f);
                case UnitRarity.Legendary: return new Color(0.96f, 0.77f, 0.19f);
                default: return Color.white;
            }
        }
        Sprite GetChipSprite(UnitRarity r) { switch (r) { case UnitRarity.Rare: return chipRare; case UnitRarity.Epic: return chipEpic; case UnitRarity.Legendary: return chipLegendary; default: return chipCommon; } }
        Sprite GetPanelSprite(UnitRarity r) { switch (r) { case UnitRarity.Rare: return panelRare; case UnitRarity.Epic: return panelEpic; case UnitRarity.Legendary: return panelLegendary; default: return panelCommon; } }
        Sprite GetIconBGSprite(UnitRarity r) { switch (r) { case UnitRarity.Rare: return iconBGRare; case UnitRarity.Epic: return iconBGEpic; case UnitRarity.Legendary: return iconBGLegendary; default: return iconBGCommon; } }

        Sprite GetBadgeSpriteByLevel(int level)
        {
            if (level <= 3) return badgeCommon;
            if (level <= 6) return badgeRare;
            if (level <= 8) return badgeEpic;
            return badgeLegendary;
        }

        string GetRarityText(UnitRarity r)
        {
            switch (r) { case UnitRarity.Common: return "커먼"; case UnitRarity.Rare: return "레어"; case UnitRarity.Epic: return "에픽"; case UnitRarity.Legendary: return "레전더리"; default: return ""; }
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