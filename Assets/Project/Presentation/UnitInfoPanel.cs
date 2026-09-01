using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Chess.Core;

namespace Chess.Presentation
{
    public class UnitInfoPanel : MonoBehaviour
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
        public TextMeshProUGUI defText;

        void Awake()
        {
            if (labelText != null) labelText.text = "아군";
            Clear();
        }

        public void Show(UnitState unit)
        {
            if (unit == null) { Clear(); return; }

            if (nameText != null) nameText.text = BuildNameText(unit);
            if (attrText != null) attrText.text = GetAttrText(unit.definition);
            int maxHP = unit.definition.GetEnhancedHP(unit.enhanceLevel);
            if (hpText != null) hpText.text = $"{unit.currentHP}/{maxHP}";
            if (atkText != null) atkText.text = FormatStat("공격", unit.GetAttack(), StatType.ATK, unit);
            if (spdText != null) spdText.text = FormatStat("속도", unit.GetSpeed(), StatType.SPD, unit);
            if (defText != null) defText.text = FormatStat("방어력", unit.GetDefense(), StatType.DEF, unit);

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
                float ratio = (float)unit.currentHP / unit.definition.GetEnhancedHP(unit.enhanceLevel);
                hpBarFill.anchorMin = new Vector2(0, 0);
                hpBarFill.anchorMax = new Vector2(ratio, 1);
                hpBarFill.offsetMin = Vector2.zero;
                hpBarFill.offsetMax = Vector2.zero;
            }
        }

        string BuildNameText(UnitState unit)
        {
            string name = unit.definition.unitName;
            var parts = new List<string>();

            int dr = GetBuffTotal(StatType.DamageReceived, unit);
            if (dr != 0)
            {
                string color = dr < 0 ? "#5AB8FF" : "#FF6060";
                string sign  = dr < 0 ? "" : "+";
                parts.Add($"<color={color}>피해{sign}{dr}</color>");
            }

            foreach (var buff in unit.activeBuffs)
            {
                if (!UIConfig.IsStatusEffect(buff.statType)) continue;
                string turns = buff.IsPermanent ? "" : $" {buff.remainingTurns}턴";
                parts.Add($"<color=#FF6060>{UIConfig.GetStatLabel(buff.statType)}{turns}</color>");
            }

            foreach (var dot in unit.damageOverTimes)
                parts.Add($"<color=#B04FFF>독 {dot.remainingTurns}턴</color>");

            if (parts.Count == 0) return name;
            return $"{name}  {string.Join(" ", parts)}";
        }

        string FormatStat(string label, int total, StatType statType, UnitState unit)
        {
            int buffAmount = GetBuffTotal(statType, unit);
            if (buffAmount == 0) return $"{label} {total}";
            string color = buffAmount > 0 ? "#4FFF4F" : "#FF6060";
            return $"<color={color}>{label} {total}</color>";
        }

        int GetBuffTotal(StatType statType, UnitState unit)
        {
            int total = 0;
            foreach (var buff in unit.activeBuffs)
                if (buff.statType == statType) total += buff.amount;
            return total;
        }

        public void Hide() => Clear();

        void Clear()
        {
            if (nameText != null) nameText.text = "-";
            if (attrText != null) attrText.text = "";
            if (hpText != null) hpText.text = "";
            if (atkText != null) atkText.text = "";
            if (spdText != null) spdText.text = "";
            if (defText != null) defText.text = "";
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
            if (unit.isKing) return "킹";
            if (unit.isPawn) return "폰";

            bool hasRook   = (unit.moveAttributes & MovementAttribute.Rook)   != 0;
            bool hasBishop = (unit.moveAttributes & MovementAttribute.Bishop)  != 0;
            bool hasKnight = (unit.moveAttributes & MovementAttribute.Knight)  != 0;

            if (hasRook && hasBishop) return "퀸";
            if (hasRook)   return "룩";
            if (hasBishop) return "비숍";
            if (hasKnight) return "나이트";
            return "";
        }
    }
}
