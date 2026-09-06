using System.Collections.Generic;
using UnityEngine;

namespace Chess.Core
{
    public class GachaSystem : MonoBehaviour
    {
        public static GachaSystem Instance { get; private set; }

        [Header("��í Ǯ (�ڵ� ����)")]
        public List<UnitDefinition> gachaPool = new List<UnitDefinition>();

        [Header("�̱� ���")]
        public int singlePullCost = 100;
        public int tenPullCost = 900;

        [Header("Ȯ�� ���� (�հ� 100)")]
        [Range(0, 100)] public float commonRate = 60f;
        [Range(0, 100)] public float rareRate = 30f;
        [Range(0, 100)] public float epicRate = 8f;
        [Range(0, 100)] public float legendaryRate = 2f;

        [Header("õ�� ����")]
        public int pityCap = 100;   // �� ������ Legendary ����

        // õ�� ī���� (���� ����)
        private int pityCounter = 0;
        private const string PITY_KEY = "GachaPityCounter";

        // �ܺο��� ���� õ�� ī���� �б�
        public int PityCounter => pityCounter;
        public int PityRemaining => pityCap - pityCounter;

        // ������������������������������������������������������������������������������
        // Unity �����ֱ�
        // ������������������������������������������������������������������������������

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        void Start()
        {
            LoadPity();
            BuildGachaPool();
        }

        // ������������������������������������������������������������������������������
        // õ�� ����/�ε�
        // ������������������������������������������������������������������������������

        void SavePity()
        {
            PlayerPrefs.SetInt(PITY_KEY, pityCounter);
            PlayerPrefs.Save();
        }

        void LoadPity()
        {
            pityCounter = PlayerPrefs.GetInt(PITY_KEY, 0);
            Debug.Log($"[Gacha] õ�� ī���� �ε�: {pityCounter} / {pityCap}");
        }

        void ResetPity()
        {
            pityCounter = 0;
            SavePity();
        }

        // ������������������������������������������������������������������������������
        // ��í Ǯ ����
        // ������������������������������������������������������������������������������

        void BuildGachaPool()
        {
            if (PlayerInventory.Instance == null)
            {
                Debug.LogError("[Gacha] PlayerInventory�� �����ϴ�!");
                return;
            }

            gachaPool.Clear();

            foreach (var unit in PlayerInventory.Instance.allUnitsDatabase)
            {
                if (unit != null && !unit.isKing && !unit.isPawn)
                    gachaPool.Add(unit);
            }

            Debug.Log($"[Gacha] ��í Ǯ ���� �Ϸ�: {gachaPool.Count}�� ����");
        }

        // ������������������������������������������������������������������������������
        // �̱� ����
        // ������������������������������������������������������������������������������

        /// <summary>
        /// ���� �̱� (õ�� ī���� ����)
        /// </summary>
        public UnitDefinition PullSingle()
        {
            if (gachaPool.Count == 0)
            {
                Debug.LogError("[Gacha] ��í Ǯ�� ����ֽ��ϴ�!");
                return null;
            }

            pityCounter++;

            UnitRarity rarity;

            // õ�� ���� �� Legendary ���� ����
            if (pityCounter >= pityCap)
            {
                rarity = UnitRarity.Legendary;
                Debug.Log($"[Gacha] õ�� ����! Legendary ���� ���� ({pityCounter}����)");
                ResetPity();
            }
            else
            {
                rarity = RollRarity();

                // Legendary ������ õ�� ����
                if (rarity == UnitRarity.Legendary)
                    ResetPity();
                else
                    SavePity();
            }

            UnitDefinition result = GetRandomUnitByRarity(rarity);
            Debug.Log($"[Gacha] ���� �̱� ���: {result?.unitName} ({rarity}) / õ��: {pityCounter}/{pityCap}");

            return result;
        }

        /// <summary>
        /// 10���� �̱� (10��° Rare �̻� ����)
        /// </summary>
        public List<UnitDefinition> PullTen()
        {
            List<UnitDefinition> results = new List<UnitDefinition>();

            for (int i = 0; i < 10; i++)
            {
                UnitDefinition unit;

                // 10번째: Rare 이상 보장 (천장 우선)
                if (i == 9)
                {
                    pityCounter++;
                    UnitRarity guaranteed;
                    if (pityCounter >= pityCap)
                    {
                        guaranteed = UnitRarity.Legendary;
                        Debug.Log($"[Gacha] 천장 발동! Legendary 확정 ({pityCounter}번째)");
                        ResetPity();
                    }
                    else
                    {
                        guaranteed = RollRarityGuaranteed();
                        if (guaranteed == UnitRarity.Legendary)
                            ResetPity();
                        else
                            SavePity();
                    }

                    unit = GetRandomUnitByRarity(guaranteed);
                    Debug.Log($"[Gacha] 10번째 결과: {unit?.unitName} ({guaranteed})");
                }
                else
                {
                    unit = PullSingle();
                }

                if (unit != null)
                    results.Add(unit);
            }

            Debug.Log($"[Gacha] 10���� �Ϸ�: {results.Count}�� ȹ�� / õ��: {pityCounter}/{pityCap}");
            return results;
        }

        // ������������������������������������������������������������������������������
        // Ȯ�� ���
        // ������������������������������������������������������������������������������

        UnitRarity RollRarity()
        {
            float roll = Random.Range(0f, 100f);
            float cumulative = 0f;

            cumulative += legendaryRate;
            if (roll < cumulative) return UnitRarity.Legendary;

            cumulative += epicRate;
            if (roll < cumulative) return UnitRarity.Epic;

            cumulative += rareRate;
            if (roll < cumulative) return UnitRarity.Rare;

            return UnitRarity.Common;
        }

        UnitRarity RollRarityGuaranteed()
        {
            // Legendary는 단일 뽑기와 동일 확률, Common 확률(60%)은 Epic으로 흡수
            float roll = Random.Range(0f, 100f);
            float cumulative = 0f;

            cumulative += legendaryRate;
            if (roll < cumulative) return UnitRarity.Legendary;

            cumulative += epicRate + commonRate;  // Epic이 Common 확률까지 흡수
            if (roll < cumulative) return UnitRarity.Epic;

            return UnitRarity.Rare;
        }

        UnitDefinition GetRandomUnitByRarity(UnitRarity rarity)
        {
            List<UnitDefinition> pool = gachaPool.FindAll(u => u.rarity == rarity);

            if (pool.Count == 0)
            {
                Debug.LogWarning($"[Gacha] {rarity} ��� ������ �����ϴ�! Common���� ��ü.");
                pool = gachaPool.FindAll(u => u.rarity == UnitRarity.Common);

                if (pool.Count == 0)
                {
                    Debug.LogError("[Gacha] ��í Ǯ�� ������ ����ֽ��ϴ�!");
                    return null;
                }
            }

            float totalWeight = 0f;
            foreach (var u in pool)
                totalWeight += Mathf.Max(u.gachaWeight, 0f);

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            foreach (var u in pool)
            {
                cumulative += Mathf.Max(u.gachaWeight, 0f);
                if (roll < cumulative)
                    return u;
            }

            return pool[pool.Count - 1];
        }

        // ������������������������������������������������������������������������������
        // �ܺ� ȣ��� (��� ���� ����)
        // ������������������������������������������������������������������������������

        public bool TryPullSingle(out UnitDefinition result)
        {
            result = null;

            if (!PlayerInventory.Instance.SpendGems(singlePullCost))
                return false;

            result = PullSingle();

            if (result != null)
                PlayerInventory.Instance.AddUnit(result);

            return true;
        }

        public bool TryPullTen(out List<UnitDefinition> results)
        {
            results = null;

            if (!PlayerInventory.Instance.SpendGems(tenPullCost))
                return false;

            results = PullTen();

            foreach (var unit in results)
            {
                if (unit != null)
                    PlayerInventory.Instance.AddUnit(unit);
            }

            return true;
        }
    }
}