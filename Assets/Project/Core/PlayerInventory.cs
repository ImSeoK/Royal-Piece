using System.Collections.Generic;
using UnityEngine;

namespace Chess.Core
{
    [System.Serializable]
    public class PlayerInventoryData
    {
        public List<OwnedUnitInstance> ownedUnits = new List<OwnedUnitInstance>();
        public int currency = 1000;
    }

    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        [Header("보유 유닛 인스턴스")]
        public List<OwnedUnitInstance> ownedUnitInstances = new List<OwnedUnitInstance>();

        [Header("골드")]
        public int currency = 1000;

        [Header("전체 유닛 데이터베이스")]
        public List<UnitDefinition> allUnitsDatabase = new List<UnitDefinition>();

        public List<UnitDefinition> ownedUnits
        {
            get
            {
                var list = new List<UnitDefinition>();
                foreach (var inst in ownedUnitInstances)
                {
                    var def = GetDefinition(inst.unitName);
                    if (def != null) list.Add(def);
                }
                return list;
            }
        }

        private const string SAVE_KEY = "PlayerInventory";

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                ResetInventory(); //LoadInventory(); //
            }
            else
                Destroy(gameObject);
        }

        public void AddUnit(UnitDefinition unit)
        {
            if (unit == null) return;
            ownedUnitInstances.Add(new OwnedUnitInstance(unit.unitName));
            SaveInventory();
            Debug.Log("[Inventory] " + unit.unitName + " 획득!");
        }

        public bool TryEnhance(OwnedUnitInstance target)
        {
            if (target == null) return false;
            if (target.enhanceLevel >= 10) { Debug.LogWarning("[Enhance] 최대 레벨!"); return false; }

            UnitDefinition def = GetDefinition(target.unitName);
            if (def == null) return false;

            if (currency < def.enhanceGoldCost) { Debug.LogWarning("[Enhance] 골드 부족!"); return false; }

            int materialCount = CountMaterials(target);
            if (materialCount < def.enhanceMaterialCount)
            {
                Debug.LogWarning("[Enhance] 재료 부족! 필요: " + def.enhanceMaterialCount + ", 보유: " + materialCount);
                return false;
            }

            ConsumeMaterials(target, def.enhanceMaterialCount);
            SpendCurrency(def.enhanceGoldCost);
            target.enhanceLevel++;
            SaveInventory();
            Debug.Log("[Enhance] " + target.unitName + " +" + target.enhanceLevel + " 성공!");
            return true;
        }

        int CountMaterials(OwnedUnitInstance target)
        {
            int count = 0;
            foreach (var inst in ownedUnitInstances)
                if (inst != target && inst.unitName == target.unitName) count++;
            return count;
        }

        void ConsumeMaterials(OwnedUnitInstance target, int count)
        {
            int removed = 0;
            var toRemove = new List<OwnedUnitInstance>();
            foreach (var inst in ownedUnitInstances)
            {
                if (inst != target && inst.unitName == target.unitName)
                {
                    toRemove.Add(inst);
                    if (++removed >= count) break;
                }
            }
            foreach (var r in toRemove) ownedUnitInstances.Remove(r);
        }

        public void SellUnit(OwnedUnitInstance target)
        {
            if (target == null) return;
            UnitDefinition def = GetDefinition(target.unitName);
            int sellPrice = def != null ? GetSellPrice(def.rarity) : 50;
            ownedUnitInstances.Remove(target);
            AddCurrency(sellPrice);
            SaveInventory();
            Debug.Log("[Sell] " + target.unitName + " 판매 -> +" + sellPrice + " 골드");
        }

        int GetSellPrice(UnitRarity rarity)
        {
            switch (rarity)
            {
                case UnitRarity.Common: return 50;
                case UnitRarity.Rare: return 150;
                case UnitRarity.Epic: return 400;
                case UnitRarity.Legendary: return 1000;
                default: return 50;
            }
        }

        public bool SpendCurrency(int amount)
        {
            if (currency < amount) { Debug.LogWarning("[Inventory] 골드 부족!"); return false; }
            currency -= amount;
            SaveInventory();
            return true;
        }

        public void AddCurrency(int amount)
        {
            currency += amount;
            SaveInventory();
        }

        public void SaveInventory()
        {
            PlayerInventoryData data = new PlayerInventoryData();
            data.currency = currency;
            data.ownedUnits = new List<OwnedUnitInstance>(ownedUnitInstances);
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        public void LoadInventory()
        {
            if (!PlayerPrefs.HasKey(SAVE_KEY)) { GiveStarterUnits(); return; }
            string json = PlayerPrefs.GetString(SAVE_KEY);
            PlayerInventoryData data = JsonUtility.FromJson<PlayerInventoryData>(json);
            currency = data.currency;
            ownedUnitInstances = data.ownedUnits ?? new List<OwnedUnitInstance>();
            Debug.Log("[Inventory] 로드 완료: " + ownedUnitInstances.Count + "개, " + currency + " 골드");
        }

        void GiveStarterUnits()
        {
            AddStarterUnit("WRook", 2);
            AddStarterUnit("WKnight", 2);
            AddStarterUnit("WBishop", 2);
            AddStarterUnit("WQueen", 1);
            currency = 99999;
            SaveInventory();
        }

        void AddStarterUnit(string unitName, int count)
        {
            UnitDefinition unit = GetDefinition(unitName);
            if (unit == null) { Debug.LogWarning("[Inventory] " + unitName + " 없음!"); return; }
            for (int i = 0; i < count; i++)
                ownedUnitInstances.Add(new OwnedUnitInstance(unitName));
        }

        public UnitDefinition GetDefinition(string unitName)
            => allUnitsDatabase.Find(u => u.unitName == unitName);

        public void ResetInventory()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            ownedUnitInstances.Clear();
            currency = 0;
            GiveStarterUnits();
        }
    }
}