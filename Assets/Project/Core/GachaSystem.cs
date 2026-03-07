using System.Collections.Generic;
using UnityEngine;

namespace Chess.Core
{
    public class GachaSystem : MonoBehaviour
    {
        public static GachaSystem Instance { get; private set; }

        [Header("가챠 풀 (자동 생성)")]
        public List<UnitDefinition> gachaPool = new List<UnitDefinition>();

        [Header("뽑기 비용")]
        public int singlePullCost = 100;
        public int tenPullCost = 900;

        [Header("확률 설정 (합계 100)")]
        [Range(0, 100)] public float commonRate = 60f;
        [Range(0, 100)] public float rareRate = 30f;
        [Range(0, 100)] public float epicRate = 8f;
        [Range(0, 100)] public float legendaryRate = 2f;

        [Header("천장 설정")]
        public int pityCap = 100;   // 몇 연차에 Legendary 보장

        // 천장 카운터 (저장 포함)
        private int pityCounter = 0;
        private const string PITY_KEY = "GachaPityCounter";

        // 외부에서 현재 천장 카운터 읽기
        public int PityCounter => pityCounter;
        public int PityRemaining => pityCap - pityCounter;

        // ───────────────────────────────────────
        // Unity 생명주기
        // ───────────────────────────────────────

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

        // ───────────────────────────────────────
        // 천장 저장/로드
        // ───────────────────────────────────────

        void SavePity()
        {
            PlayerPrefs.SetInt(PITY_KEY, pityCounter);
            PlayerPrefs.Save();
        }

        void LoadPity()
        {
            pityCounter = PlayerPrefs.GetInt(PITY_KEY, 0);
            Debug.Log($"[Gacha] 천장 카운터 로드: {pityCounter} / {pityCap}");
        }

        void ResetPity()
        {
            pityCounter = 0;
            SavePity();
        }

        // ───────────────────────────────────────
        // 가챠 풀 생성
        // ───────────────────────────────────────

        void BuildGachaPool()
        {
            if (PlayerInventory.Instance == null)
            {
                Debug.LogError("[Gacha] PlayerInventory가 없습니다!");
                return;
            }

            gachaPool.Clear();

            foreach (var unit in PlayerInventory.Instance.allUnitsDatabase)
            {
                if (unit != null && !unit.isKing && !unit.isPawn)
                    gachaPool.Add(unit);
            }

            Debug.Log($"[Gacha] 가챠 풀 생성 완료: {gachaPool.Count}개 유닛");
        }

        // ───────────────────────────────────────
        // 뽑기 로직
        // ───────────────────────────────────────

        /// <summary>
        /// 단일 뽑기 (천장 카운터 포함)
        /// </summary>
        public UnitDefinition PullSingle()
        {
            if (gachaPool.Count == 0)
            {
                Debug.LogError("[Gacha] 가챠 풀이 비어있습니다!");
                return null;
            }

            pityCounter++;

            UnitRarity rarity;

            // 천장 도달 시 Legendary 강제 지급
            if (pityCounter >= pityCap)
            {
                rarity = UnitRarity.Legendary;
                Debug.Log($"[Gacha] 천장 도달! Legendary 보장 지급 ({pityCounter}연차)");
                ResetPity();
            }
            else
            {
                rarity = RollRarity();

                // Legendary 나오면 천장 리셋
                if (rarity == UnitRarity.Legendary)
                    ResetPity();
                else
                    SavePity();
            }

            UnitDefinition result = GetRandomUnitByRarity(rarity);
            Debug.Log($"[Gacha] 단일 뽑기 결과: {result?.unitName} ({rarity}) / 천장: {pityCounter}/{pityCap}");

            return result;
        }

        /// <summary>
        /// 10연차 뽑기 (10번째 Rare 이상 보장)
        /// </summary>
        public List<UnitDefinition> PullTen()
        {
            List<UnitDefinition> results = new List<UnitDefinition>();

            for (int i = 0; i < 10; i++)
            {
                UnitDefinition unit;

                // 10번째: Rare 이상 보장
                if (i == 9)
                {
                    UnitRarity guaranteed = RollRarityGuaranteed();
                    // 천장 카운터도 증가
                    pityCounter++;
                    if (guaranteed == UnitRarity.Legendary)
                        ResetPity();
                    else
                        SavePity();

                    unit = GetRandomUnitByRarity(guaranteed);
                    Debug.Log($"[Gacha] 10연차 보장: {unit?.unitName} ({guaranteed})");
                }
                else
                {
                    unit = PullSingle();
                }

                if (unit != null)
                    results.Add(unit);
            }

            Debug.Log($"[Gacha] 10연차 완료: {results.Count}개 획득 / 천장: {pityCounter}/{pityCap}");
            return results;
        }

        // ───────────────────────────────────────
        // 확률 계산
        // ───────────────────────────────────────

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
            float totalRate = legendaryRate + epicRate + rareRate;
            float roll = Random.Range(0f, totalRate);
            float cumulative = 0f;

            cumulative += legendaryRate;
            if (roll < cumulative) return UnitRarity.Legendary;

            cumulative += epicRate;
            if (roll < cumulative) return UnitRarity.Epic;

            return UnitRarity.Rare;
        }

        UnitDefinition GetRandomUnitByRarity(UnitRarity rarity)
        {
            List<UnitDefinition> pool = gachaPool.FindAll(u => u.rarity == rarity);

            if (pool.Count == 0)
            {
                Debug.LogWarning($"[Gacha] {rarity} 등급 유닛이 없습니다! Common으로 대체.");
                pool = gachaPool.FindAll(u => u.rarity == UnitRarity.Common);

                if (pool.Count == 0)
                {
                    Debug.LogError("[Gacha] 가챠 풀이 완전히 비어있습니다!");
                    return null;
                }
            }

            return pool[Random.Range(0, pool.Count)];
        }

        // ───────────────────────────────────────
        // 외부 호출용 (골드 차감 포함)
        // ───────────────────────────────────────

        public bool TryPullSingle(out UnitDefinition result)
        {
            result = null;

            if (!PlayerInventory.Instance.SpendCurrency(singlePullCost))
                return false;

            result = PullSingle();

            if (result != null)
                PlayerInventory.Instance.AddUnit(result);

            return true;
        }

        public bool TryPullTen(out List<UnitDefinition> results)
        {
            results = null;

            if (!PlayerInventory.Instance.SpendCurrency(tenPullCost))
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