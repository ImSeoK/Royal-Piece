using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Chess.Core;

namespace Chess.Presentation
{
    public class PickerCard : MonoBehaviour
    {
        public Image unitIcon;
        public Image levelBadgeImage;
        public TextMeshProUGUI levelBadgeText;

        [Header("���� ��������Ʈ")]
        public Sprite badgeCommon;
        public Sprite badgeRare;
        public Sprite badgeEpic;
        public Sprite badgeLegendary;

        private System.Action<PickerCard> onClickCallback;
        public OwnedUnitInstance ownedInstance { get; private set; }

        void Awake()
        {
            Button btn = GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => onClickCallback?.Invoke(this));
        }

        public void Initialize(UnitDefinition def, OwnedUnitInstance inst, System.Action<PickerCard> onClick)
        {
            ownedInstance = inst;
            onClickCallback = onClick;

            // ������
            if (unitIcon != null)
            {
                unitIcon.sprite = def.GetIcon(inst.enhanceLevel);
                unitIcon.color = Color.white;
            }

            // ���� ����
            // 레벨 뱃지 항상 표시
            if (levelBadgeImage != null)
            {
                levelBadgeImage.gameObject.SetActive(true);
                levelBadgeImage.sprite = GetBadgeSpriteByLevel(inst.enhanceLevel);
            }
            if (levelBadgeText != null)
            {
                levelBadgeText.text = $"+{inst.enhanceLevel}";
                levelBadgeText.fontSize = inst.enhanceLevel >= 10 ? 28 : 36;
            }
        }

        Sprite GetBadgeSpriteByLevel(int level)
        {
            if (level <= 3) return badgeCommon;
            if (level <= 6) return badgeRare;
            if (level <= 8) return badgeEpic;
            return badgeLegendary;
        }
    }
}