using System.Collections.Generic;
using UnityEngine;

namespace Chess.Core
{
    [System.Serializable]
    public class PlayerInventoryData
    {
        public List<OwnedUnitInstance> ownedUnits = new List<OwnedUnitInstance>();
        public int currency = 1000;
        public int gems = 0;
    }

    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        [Header("���� ���� �ν��Ͻ�")]
        public List<OwnedUnitInstance> ownedUnitInstances = new List<OwnedUnitInstance>();

        [Header("���")]
        public int currency = 1000;
        public int gems = 0;

        [Header("��ü ���� �����ͺ��̽�")]
        public List<UnitDefinition> allUnitsDatabase = new List<UnitDefinition>();

        [Header("Debug")]
        public bool ignoreFirestore = false;

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
                LoadInventory(); // 빠른 로컬 로드 (오프라인 안전)
            }
            else
                Destroy(gameObject);
        }

        void Start()
        {
            // Awake가 모두 끝난 후 구독 → FirebaseManager 초기화 순서와 무관
            if (Instance != this) return;
            FirebaseManager.Instance?.SubscribeDataReady(ApplyFromFirestore);
        }

        /// <summary>Firestore에서 로드한 데이터로 로컬 상태 덮어쓰기.</summary>
        void ApplyFromFirestore(FirestorePlayerData data)
        {
            if (data == null) return; // 신규 유저 — 로컬 데이터 유지
            if (ignoreFirestore) return; // Debug: Firestore 덮어쓰기 무시

            currency = data.currency;
            gems     = data.gems;
            ownedUnitInstances.Clear();
            foreach (var entry in data.ownedUnits)
            {
                ownedUnitInstances.Add(new OwnedUnitInstance(entry.unitName)
                {
                    instanceId   = entry.instanceId,
                    enhanceLevel = entry.enhanceLevel,
                });
            }

            // 로컬 캐시도 갱신
            SaveInventoryLocal();
            Debug.Log($"[Inventory] Firestore 동기화 완료: {ownedUnitInstances.Count}개 유닛, {currency} 골드");
        }

        public void AddUnit(UnitDefinition unit)
        {
            if (unit == null) return;
            ownedUnitInstances.Add(new OwnedUnitInstance(unit.unitName));
            SaveInventory();
            Debug.Log("[Inventory] " + unit.unitName + " ȹ��!");
        }

        public bool TryEnhance(OwnedUnitInstance target)
        {
            if (target == null) return false;
            if (target.enhanceLevel >= 10) { Debug.LogWarning("[Enhance] �ִ� ����!"); return false; }

            UnitDefinition def = GetDefinition(target.unitName);
            if (def == null) return false;

            if (currency < def.enhanceGoldCost) { Debug.LogWarning("[Enhance] ��� ����!"); return false; }

            int materialCount = CountMaterials(target);
            if (materialCount < def.enhanceMaterialCount)
            {
                Debug.LogWarning("[Enhance] ��� ����! �ʿ�: " + def.enhanceMaterialCount + ", ����: " + materialCount);
                return false;
            }

            ConsumeMaterials(target, def.enhanceMaterialCount);
            SpendCurrency(def.enhanceGoldCost);
            target.enhanceLevel++;
            SaveInventory();
            Debug.Log("[Enhance] " + target.unitName + " +" + target.enhanceLevel + " ����!");
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
            Debug.Log("[Sell] " + target.unitName + " �Ǹ� -> +" + sellPrice + " ���");
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
            if (currency < amount) { Debug.LogWarning("[Inventory] ��� ����!"); return false; }
            currency -= amount;
            SaveInventory();
            return true;
        }

        public void AddCurrency(int amount)
        {
            currency += amount;
            SaveInventory();
        }

        public bool SpendGems(int amount)
        {
            if (gems < amount) { Debug.LogWarning("[Inventory] 로열 젬 부족!"); return false; }
            gems -= amount;
            SaveInventory();
            return true;
        }

        public void AddGems(int amount)
        {
            gems += amount;
            SaveInventory();
        }

        public void SaveInventory()
        {
            SaveInventoryLocal();
            SaveInventoryFirestore();
        }

        void SaveInventoryLocal()
        {
            var data = new PlayerInventoryData
            {
                currency   = currency,
                gems       = gems,
                ownedUnits = new List<OwnedUnitInstance>(ownedUnitInstances),
            };
            PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        void SaveInventoryFirestore()
        {
            var fm = FirebaseManager.Instance;
            if (fm == null || !fm.IsLoggedIn) return;

            var fsData = BuildFirestoreData();
            fm.SavePlayerData(fsData);
        }

        FirestorePlayerData BuildFirestoreData()
        {
            var fsData = new FirestorePlayerData
            {
                currency    = currency,
                gems        = gems,
                rankPoints  = RankSystem.GetPoints(),
                lastDailyBonus    = PlayerPrefs.GetString("daily_bonus_date",      ""),
                dailyMissionDate  = PlayerPrefs.GetString("daily_mission_date",    ""),
                dailyMissionWins  = PlayerPrefs.GetInt   ("daily_mission_wins",    0),
                dailyMissionClaimed = PlayerPrefs.GetInt ("daily_mission_claimed", 0) == 1,
            };
            foreach (var inst in ownedUnitInstances)
                fsData.ownedUnits.Add(new OwnedUnitEntry
                {
                    instanceId   = inst.instanceId,
                    unitName     = inst.unitName,
                    enhanceLevel = inst.enhanceLevel,
                });
            return fsData;
        }

        public void LoadInventory()
        {
            if (!PlayerPrefs.HasKey(SAVE_KEY)) { GiveStarterUnits(); return; }
            string json = PlayerPrefs.GetString(SAVE_KEY);
            PlayerInventoryData data = JsonUtility.FromJson<PlayerInventoryData>(json);
            currency = data.currency;
            gems     = data.gems;
            ownedUnitInstances = data.ownedUnits ?? new List<OwnedUnitInstance>();
            Debug.Log("[Inventory] �ε� �Ϸ�: " + ownedUnitInstances.Count + "��, " + currency + " ���");
        }

        void GiveStarterUnits()
        {
            AddStarterUnit("WRook", 2);
            AddStarterUnit("WKnight", 2);
            AddStarterUnit("WBishop", 2);
            AddStarterUnit("WQueen", 1);
            AddStarterUnit("다윈", 1);
            AddStarterUnit("폴", 1);
            AddStarterUnit("라크", 1);
            AddStarterUnit("랍", 1);
            AddStarterUnit("카토", 1);
            AddStarterUnit("토드라", 1);
            AddStarterUnit("자르칸", 1);
            AddStarterUnit("돔", 1);
            AddStarterUnit("벨크라스", 1);
            AddStarterUnit("글래시온", 1);
            AddStarterUnit("제피로스", 1);
            AddStarterUnit("JS", 1);
            AddStarterUnit("리세", 1);
            AddStarterUnit("레나", 1);
            AddStarterUnit("엘로비아", 1);
            currency = 99999;
            gems     = 9999;
            SaveInventory();
        }

        void AddStarterUnit(string unitName, int count)
        {
            UnitDefinition unit = GetDefinition(unitName);
            if (unit == null) { Debug.LogWarning("[Inventory] " + unitName + " ����!"); return; }
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