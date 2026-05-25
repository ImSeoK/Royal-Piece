using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Chess.Core;

namespace Chess.Presentation
{
    public static class UnitSortUtility
    {
        public static int GetRarityOrder(UnitRarity rarity)
        {
            switch (rarity)
            {
                case UnitRarity.Legendary: return 3;
                case UnitRarity.Epic:      return 2;
                case UnitRarity.Rare:      return 1;
                default:                   return 0;
            }
        }

        public static int GetChosung(string name)
        {
            if (string.IsNullOrEmpty(name)) return int.MaxValue;
            char c = name[0];
            if (c >= 0xAC00 && c <= 0xD7A3) return (c - 0xAC00) / (21 * 28);
            return c;
        }

        /// <summary>
        /// OwnedUnitInstance 리스트 정렬 (인벤토리용)
        /// 1순위: 등급 내림차순 / 2순위: 강화 레벨 내림차순 / 3순위: 이름 초성 오름차순
        /// </summary>
        public static List<OwnedUnitInstance> SortInstances(IEnumerable<OwnedUnitInstance> instances)
        {
            return instances
                .OrderByDescending(inst =>
                {
                    UnitDefinition u = PlayerInventory.Instance.GetDefinition(inst.unitName);
                    return u != null ? GetRarityOrder(u.rarity) : 0;
                })
                .ThenByDescending(inst => inst.enhanceLevel)
                .ThenBy(inst => inst.unitName, System.StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// 피커 카드 GameObject 리스트 정렬 후 Sibling Index 적용 (덱빌더용)
        /// 1순위: 등급 내림차순 / 2순위: 강화 레벨 내림차순 / 3순위: 이름 초성 오름차순
        /// </summary>
        public static void SortPickerCards(List<GameObject> pickerCards)
        {
            pickerCards.Sort((a, b) =>
            {
                InventoryCard ca = a != null ? a.GetComponent<InventoryCard>() : null;
                InventoryCard cb = b != null ? b.GetComponent<InventoryCard>() : null;
                if (ca == null || ca.unitDefinition == null) return 1;
                if (cb == null || cb.unitDefinition == null) return -1;

                int rarityCompare = GetRarityOrder(cb.unitDefinition.rarity)
                    .CompareTo(GetRarityOrder(ca.unitDefinition.rarity));
                if (rarityCompare != 0) return rarityCompare;

                int enhanceCompare = (cb.ownedInstance?.enhanceLevel ?? 0)
                    .CompareTo(ca.ownedInstance?.enhanceLevel ?? 0);
                if (enhanceCompare != 0) return enhanceCompare;

                return string.Compare(ca.unitDefinition.unitName, cb.unitDefinition.unitName, System.StringComparison.Ordinal);
            });

            for (int i = 0; i < pickerCards.Count; i++)
                if (pickerCards[i] != null) pickerCards[i].transform.SetSiblingIndex(i);
        }
    }
}
