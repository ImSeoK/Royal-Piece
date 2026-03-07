using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Chess.Core;

namespace Chess.Presentation
{
    public class EnemyUnitInfoPanel : MonoBehaviour
    {
        [Header("UI")]
        public Image unitIcon;
        public TextMeshProUGUI labelText;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI attrText;
        public RectTransform hpBarFill;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI atkText;
        public TextMeshProUGUI spdText;

        void Awake()
        {
            if (labelText != null) labelText.text = "Àû ¸»";
            Clear();
        }

        public void Show(UnitState unit)
        {
            if (unit == null) { Clear(); return; }

            if (nameText != null) nameText.text = unit.definition.unitName;
            if (attrText != null) attrText.text = GetAttrText(unit.definition);
            if (hpText != null) hpText.text = $"{unit.currentHP}/{unit.definition.maxHP}";
            if (atkText != null) atkText.text = $"°ø°Ý {unit.definition.attackPower}";
            if (spdText != null) spdText.text = $"¼Óµµ {unit.definition.speed}";

            if (unitIcon != null)
            {
                if (unit.definition.icon != null)
                {
                    unitIcon.sprite = unit.definition.icon;
                    unitIcon.color = Color.white;
                }
                else
                    unitIcon.color = Color.clear;
            }

            if (hpBarFill != null)
            {
                float ratio = (float)unit.currentHP / unit.definition.maxHP;
                hpBarFill.anchorMin = new Vector2(0, 0);
                hpBarFill.anchorMax = new Vector2(ratio, 1);
                hpBarFill.offsetMin = Vector2.zero;
                hpBarFill.offsetMax = Vector2.zero;
            }
        }

        public void Hide() => Clear();

        void Clear()
        {
            if (nameText != null) nameText.text = "-";
            if (attrText != null) attrText.text = "";
            if (hpText != null) hpText.text = "";
            if (atkText != null) atkText.text = "";
            if (spdText != null) spdText.text = "";
            if (unitIcon != null) unitIcon.color = Color.clear;

            if (hpBarFill != null)
            {
                hpBarFill.anchorMax = new Vector2(0, 1);
                hpBarFill.offsetMin = Vector2.zero;
                hpBarFill.offsetMax = Vector2.zero;
            }
        }

        string GetAttrText(UnitDefinition unit)
        {
            if (unit.isKing) return "Å·";
            if (unit.isPawn) return "Æù";

            bool hasRook = (unit.moveAttributes & MovementAttribute.Rook) != 0;
            bool hasBishop = (unit.moveAttributes & MovementAttribute.Bishop) != 0;
            bool hasKnight = (unit.moveAttributes & MovementAttribute.Knight) != 0;

            if (hasRook && hasBishop) return "Äý";
            if (hasRook) return "·è";
            if (hasBishop) return "ºñ¼ó";
            if (hasKnight) return "³ªÀÌÆ®";
            return "";
        }
    }
}