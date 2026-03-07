using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Chess.Core;

namespace Chess.Presentation
{
    public class GachaResultCard : MonoBehaviour
    {
        [Header("UI 요소")]
        public Image unitImage;
        public TextMeshProUGUI unitNameText;
        public TextMeshProUGUI rarityText;
        public Image cardBackground;
        public Image unitIconBG;

        [Header("등급별 스프라이트")]
        public Sprite bgCommon;
        public Sprite bgRare;
        public Sprite bgEpic;
        public Sprite bgLegendary;

        public Sprite iconBGCommon;
        public Sprite iconBGRare;
        public Sprite iconBGEpic;
        public Sprite iconBGLegendary;

        public void Initialize(UnitDefinition unit)
        {
            if (unit == null) return;

            // 아이콘
            if (unitImage != null && unit.icon != null)
            {
                unitImage.sprite = unit.icon;
                unitImage.color = Color.white;
            }

            // 이름
            if (unitNameText != null) unitNameText.text = unit.unitName;

            // 등급 텍스트
            if (rarityText != null)
            {
                rarityText.text = GetRarityText(unit.rarity);
                rarityText.color = GetRarityColor(unit.rarity);
            }

            // 카드 배경 스프라이트
            if (cardBackground != null)
            {
                Sprite bg = GetBGSprite(unit.rarity);
                if (bg != null) cardBackground.sprite = bg;
                else cardBackground.color = GetRarityBackgroundColor(unit.rarity);
            }

            // 아이콘 BG 스프라이트
            if (unitIconBG != null)
            {
                Sprite ibg = GetIconBGSprite(unit.rarity);
                if (ibg != null) unitIconBG.sprite = ibg;
            }
        }

        Sprite GetBGSprite(UnitRarity r)
        {
            switch (r)
            {
                case UnitRarity.Rare: return bgRare;
                case UnitRarity.Epic: return bgEpic;
                case UnitRarity.Legendary: return bgLegendary;
                default: return bgCommon;
            }
        }

        Sprite GetIconBGSprite(UnitRarity r)
        {
            switch (r)
            {
                case UnitRarity.Rare: return iconBGRare;
                case UnitRarity.Epic: return iconBGEpic;
                case UnitRarity.Legendary: return iconBGLegendary;
                default: return iconBGCommon;
            }
        }

        string GetRarityText(UnitRarity r)
        {
            switch (r)
            {
                case UnitRarity.Common: return "커먼";
                case UnitRarity.Rare: return "레어";
                case UnitRarity.Epic: return "에픽";
                case UnitRarity.Legendary: return "레전더리";
                default: return "";
            }
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

        Color GetRarityBackgroundColor(UnitRarity r)
        {
            switch (r)
            {
                case UnitRarity.Common: return new Color(0.25f, 0.25f, 0.25f);
                case UnitRarity.Rare: return new Color(0.15f, 0.20f, 0.35f);
                case UnitRarity.Epic: return new Color(0.25f, 0.15f, 0.35f);
                case UnitRarity.Legendary: return new Color(0.35f, 0.30f, 0.15f);
                default: return new Color(0.20f, 0.20f, 0.24f);
            }
        }
    }
}