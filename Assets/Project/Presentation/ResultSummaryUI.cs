using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Chess.Core;

namespace Chess.Presentation
{
    /// <summary>
    /// 10연 결과 요약 화면.
    /// - 피처 카드(최고 등급 or 전달된 featureIdx): 상단 크게, 앞면 강제 표시
    /// - 나머지 9장: 3×3 그리드, Epic/Legendary는 뒷면 → 탭하면 플립
    /// - 모든 Epic/Legendary 카드 공개 완료 시 confirmButton 활성화
    /// </summary>
    public class ResultSummaryUI : MonoBehaviour
    {
        [Header("피처 카드 슬롯 (최고 등급 1장)")]
        public Transform featureCardSlot;

        [Header("나머지 카드 그리드 (3×3 Grid Layout Group 권장)")]
        public Transform gridContainer;

        [Header("카드 프리팹 (CardUI 컴포넌트 필요)")]
        public GameObject largePrefab;   // BestUnit 슬롯용
        public GameObject smallPrefab;   // 그리드 9장용

        [Header("확인 버튼 (모든 카드 공개 전까지 비활성)")]
        public Button confirmButton;

        [Header("10연 설정")]
        public float cardAppearInterval = 0.1f;  // 그리드 카드 1장씩 등장 간격
        public float gridAppearDelay    = 0.8f;  // 9장 완료 후 bestUnit 등장까지 딜레이

        private int pendingFlipCount = 0;
        private Coroutine _activeSequence;

        // 스킵용 시퀀스 상태
        private List<UnitDefinition>    _seqResults;
        private HashSet<UnitDefinition> _seqPreExisting;
        private int                     _seqFeatureIdx;
        private System.Action           _seqOnComplete;
        private System.Action           _seqOnAllFlipped;
        private HashSet<int>            _spawnedIndices = new HashSet<int>();

        // ─────────────────────────────────────────────
        // 외부 진입점
        // ─────────────────────────────────────────────

        /// <param name="featureIdx">피처 카드 인덱스. -1이면 최고 등급을 자동 탐색.</param>
        public void Setup(List<UnitDefinition> results,
                          HashSet<UnitDefinition> preExisting = null,
                          int featureIdx = -1)
        {
            ClearSlots();
            if (results == null || results.Count == 0) return;

            pendingFlipCount = 0;
            if (confirmButton != null) confirmButton.interactable = false;

            int idx = featureIdx >= 0 ? featureIdx : FindHighestRarityIndex(results);

            for (int i = 0; i < results.Count; i++)
            {
                bool isFeature = (i == idx);
                bool isNew     = preExisting == null || !preExisting.Contains(results[i]);

                if (isFeature)
                {
                    // 피처 카드: Step 3에서 이미 공개됨 → 앞면 강제
                    SpawnCard(results[i], featureCardSlot, isNew, forceShowFront: true);
                }
                else
                {
                    bool needsFlip = results[i] != null &&
                                     (results[i].rarity == UnitRarity.Epic ||
                                      results[i].rarity == UnitRarity.Legendary);

                    if (needsFlip) pendingFlipCount++;

                    GameObject obj = SpawnCard(results[i], gridContainer, isNew);
                    if (needsFlip && obj != null)
                    {
                        CardUI card = obj.GetComponent<CardUI>();
                        card?.EnableTapToFlip(OnCardFlipped);
                    }
                }
            }

            // 플립 대상 없으면 즉시 확인 버튼 활성화
            if (pendingFlipCount == 0 && confirmButton != null)
                confirmButton.interactable = true;
        }

        // ─────────────────────────────────────────────
        // 10연 시퀀스: 9장 그리드 → 딜레이 → bestUnit
        // ─────────────────────────────────────────────

        public void Clear()
        {
            if (_activeSequence != null) { StopCoroutine(_activeSequence); _activeSequence = null; }
            ClearSlots();
            pendingFlipCount = 0;
            if (confirmButton != null) confirmButton.interactable = false;
        }

        public void RunSetupSequence(List<UnitDefinition> results,
                                     HashSet<UnitDefinition> preExisting = null,
                                     int featureIdx = -1,
                                     System.Action onComplete = null,
                                     System.Action onAllFlipped = null)
        {
            _seqResults      = results;
            _seqPreExisting  = preExisting;
            _seqFeatureIdx   = featureIdx >= 0 ? featureIdx : FindHighestRarityIndex(results);
            _seqOnComplete   = onComplete;
            _seqOnAllFlipped = onAllFlipped;
            _spawnedIndices.Clear();

            if (_activeSequence != null) StopCoroutine(_activeSequence);
            _activeSequence = StartCoroutine(SetupSequence());
        }

        // 스킵: 코루틴 중단 후 남은 카드 즉시 등장
        public void SkipSequence()
        {
            if (_activeSequence != null)
            {
                StopCoroutine(_activeSequence);
                _activeSequence = null;
            }

            if (_seqResults == null) return;

            for (int i = 0; i < _seqResults.Count; i++)
            {
                if (_spawnedIndices.Contains(i)) continue;

                bool isFeature = (i == _seqFeatureIdx);
                bool isNew     = _seqPreExisting == null || !_seqPreExisting.Contains(_seqResults[i]);
                bool needsFlip = _seqResults[i] != null &&
                                 (_seqResults[i].rarity == UnitRarity.Epic ||
                                  _seqResults[i].rarity == UnitRarity.Legendary);
                if (needsFlip) pendingFlipCount++;

                Transform parent = isFeature ? featureCardSlot : gridContainer;
                GameObject obj = SpawnCard(_seqResults[i], parent, isNew,
                                           forceShowFront: isFeature && !needsFlip);
                if (needsFlip && obj != null)
                {
                    CardUI card = obj.GetComponent<CardUI>();
                    card?.EnableTapToFlip(OnCardFlipped);
                }

                _spawnedIndices.Add(i);
            }

            if (pendingFlipCount == 0)
            {
                if (confirmButton != null) confirmButton.interactable = true;
                _seqOnAllFlipped?.Invoke();
            }

            _seqOnComplete?.Invoke();
        }

        IEnumerator SetupSequence()
        {
            ClearSlots();
            if (_seqResults == null || _seqResults.Count == 0) yield break;

            pendingFlipCount = 0;
            if (confirmButton != null) confirmButton.interactable = false;

            // 9장 그리드 순차 등장
            for (int i = 0; i < _seqResults.Count; i++)
            {
                if (i == _seqFeatureIdx) continue;
                bool isNew     = _seqPreExisting == null || !_seqPreExisting.Contains(_seqResults[i]);
                bool needsFlip = _seqResults[i] != null &&
                                 (_seqResults[i].rarity == UnitRarity.Epic ||
                                  _seqResults[i].rarity == UnitRarity.Legendary);
                if (needsFlip) pendingFlipCount++;

                GameObject obj = SpawnCard(_seqResults[i], gridContainer, isNew);
                if (needsFlip && obj != null)
                {
                    CardUI card = obj.GetComponent<CardUI>();
                    card?.EnableTapToFlip(OnCardFlipped);
                }

                _spawnedIndices.Add(i);
                yield return new WaitForSeconds(cardAppearInterval);
            }

            // 딜레이 후 bestUnit 마지막 등장
            yield return new WaitForSeconds(gridAppearDelay);

            bool featureIsNew    = _seqPreExisting == null || !_seqPreExisting.Contains(_seqResults[_seqFeatureIdx]);
            bool featureNeedsFlip = _seqResults[_seqFeatureIdx] != null &&
                                    (_seqResults[_seqFeatureIdx].rarity == UnitRarity.Epic ||
                                     _seqResults[_seqFeatureIdx].rarity == UnitRarity.Legendary);
            if (featureNeedsFlip) pendingFlipCount++;

            GameObject featureObj = SpawnCard(_seqResults[_seqFeatureIdx], featureCardSlot,
                                              featureIsNew, forceShowFront: !featureNeedsFlip);
            if (featureNeedsFlip && featureObj != null)
            {
                CardUI card = featureObj.GetComponent<CardUI>();
                card?.EnableTapToFlip(OnCardFlipped);
            }

            _spawnedIndices.Add(_seqFeatureIdx);

            if (pendingFlipCount == 0)
            {
                if (confirmButton != null) confirmButton.interactable = true;
                _seqOnAllFlipped?.Invoke();
            }

            _seqOnComplete?.Invoke();
        }

        // ─────────────────────────────────────────────
        // 플립 콜백
        // ─────────────────────────────────────────────

        void OnCardFlipped()
        {
            pendingFlipCount = Mathf.Max(0, pendingFlipCount - 1);
            if (pendingFlipCount == 0)
            {
                if (confirmButton != null) confirmButton.interactable = true;
                _seqOnAllFlipped?.Invoke();
            }
        }

        // ─────────────────────────────────────────────
        // 내부 헬퍼
        // ─────────────────────────────────────────────

        GameObject SpawnCard(UnitDefinition unit, Transform parent,
                             bool isNew, bool forceShowFront = false)
        {
            bool isFeatureSlot = (parent == featureCardSlot);
            GameObject prefab = isFeatureSlot ? largePrefab : smallPrefab;
            if (prefab == null || parent == null) return null;
            GameObject obj = Instantiate(prefab, parent);

            RectTransform parentRect = parent as RectTransform;
            if (parentRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);

            CardUI card = obj.GetComponent<CardUI>();
            card?.Initialize(unit, isNew, forceShowFront, slideIn: isFeatureSlot);
            return obj;
        }

        int FindHighestRarityIndex(List<UnitDefinition> results)
        {
            int best = 0;
            for (int i = 1; i < results.Count; i++)
            {
                if (results[i] != null && results[best] != null &&
                    results[i].rarity > results[best].rarity)
                    best = i;
            }
            return best;
        }

        void ClearSlots()
        {
            if (featureCardSlot != null)
                foreach (Transform child in featureCardSlot) Destroy(child.gameObject);
            if (gridContainer != null)
                foreach (Transform child in gridContainer) Destroy(child.gameObject);
        }
    }
}
