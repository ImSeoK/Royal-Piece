using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Chess.Core;

namespace Chess.Presentation
{
    public class GachaManager : MonoBehaviour
    {
        [Header("HUD")]
        public TextMeshProUGUI gemText;
        public TextMeshProUGUI pityText;
        public RectTransform pityBarFill;
        public Button backButton;

        [Header("대기 화면 (WaitingPanel)")]
        public GameObject waitingPanel;
        public Button singlePullButton;
        public Button tenPullButton;

        [Header("단독 연출 패널 (RevealPanel)")]
        public GameObject revealPanel;
        public Transform revealGrid;           // 피처 카드 1장 스폰 위치

        [Header("결과 확인 (ResultPanel)")]
        public GameObject resultPanel;
        public Transform resultGrid;           // ResultSummaryUI 미연결 시 폴백용
        public Transform singlePullSlot;       // 1연 카드 전용 위치 (10연 featureCardSlot과 별도)
        public Button skipButton;              // 카드 등장 중 표시 → 완료 시 숨김
        public Button pullAgainButton;         // 카드 등장 완료 후 표시
        public Button closeResultButton;

        [Header("프리팹")]
        public GameObject gachaCardPrefab;

[Header("시스템")]
        public GachaSystem gachaSystem;

        [Header("연출")]
        public GachaVFXController vfxController;
        public ResultSummaryUI resultSummaryUI;

        // ─────────────────────────────────────────────
        // 내부 상태
        // ─────────────────────────────────────────────

        private List<UnitDefinition> currentResults  = new List<UnitDefinition>();
        private HashSet<UnitDefinition> preExistingUnits = new HashSet<UnitDefinition>();
        private int  currentFeatureIdx  = 0;
        private bool lastPullWasTen     = false;
        private bool _isPulling         = false;
        private Coroutine revealCoroutine;

        // ─────────────────────────────────────────────
        // Unity 라이프사이클
        // ─────────────────────────────────────────────

        void Start()
        {
            if (gachaSystem == null)
                gachaSystem = FindFirstObjectByType<GachaSystem>();

            if (gachaSystem == null)
            {
                Debug.LogError("[GachaManager] GachaSystem을 찾을 수 없습니다!");
                return;
            }

            RegisterButtons();
            ShowWaitingPanel();
            UpdateHUD();
        }

        void RegisterButtons()
        {
            if (backButton        != null) backButton.onClick.AddListener(OnBackClicked);
            if (singlePullButton  != null) singlePullButton.onClick.AddListener(OnSinglePullClicked);
            if (tenPullButton     != null) tenPullButton.onClick.AddListener(OnTenPullClicked);
            if (skipButton        != null) skipButton.onClick.AddListener(OnSkipClicked);
            if (pullAgainButton   != null) pullAgainButton.onClick.AddListener(OnPullAgainClicked);
            if (closeResultButton != null) closeResultButton.onClick.AddListener(OnCloseResultClicked);
        }

        // ─────────────────────────────────────────────
        // 뽑기 진입점
        // ─────────────────────────────────────────────

        void OnSinglePullClicked()
        {
            if (_isPulling) return;
            _isPulling = true;
            if (singlePullButton != null) singlePullButton.interactable = false;
            if (tenPullButton    != null) tenPullButton.interactable    = false;
            lastPullWasTen = false;
            currentResults.Clear();
            SnapshotExistingUnits();

            UnitDefinition result;
            if (!gachaSystem.TryPullSingle(out result)) return;
            if (result != null) currentResults.Add(result);

            AudioManager.Instance?.Play("sfx_gacha_start");
            if (revealCoroutine != null) StopCoroutine(revealCoroutine);
            revealCoroutine = StartCoroutine(SinglePullSequence());
        }

        void OnTenPullClicked()
        {
            if (_isPulling) return;
            _isPulling = true;
            if (singlePullButton != null) singlePullButton.interactable = false;
            if (tenPullButton    != null) tenPullButton.interactable    = false;
            lastPullWasTen = true;
            currentResults.Clear();
            SnapshotExistingUnits();

            List<UnitDefinition> results;
            if (!gachaSystem.TryPullTen(out results)) return;
            currentResults = results;

            AudioManager.Instance?.Play("sfx_gacha_start");
            if (revealCoroutine != null) StopCoroutine(revealCoroutine);
            revealCoroutine = StartCoroutine(TenPullSequence());
        }

        // ─────────────────────────────────────────────
        // 1연 시퀀스
        // ─────────────────────────────────────────────

        IEnumerator SinglePullSequence()
        {
            if (currentResults.Count == 0) yield break;

            currentFeatureIdx = 0;
            UnitDefinition unit = currentResults[0];

            // 이전 결과 클리어 후 VFX (깨끗한 배경에서 재생)
            ClearPreviousResult();
            if (vfxController != null)
                yield return StartCoroutine(vfxController.PlayVFX(unit.rarity));

            AudioManager.Instance?.Play(GetGradeSoundId(unit));

            // VFX 완료 후 패널 열기 + 카드 스폰
            if (resultSummaryUI != null) resultSummaryUI.Clear();
            OpenResultPanel();

            Transform spawnTarget = singlePullSlot != null ? singlePullSlot
                : (resultSummaryUI != null && resultSummaryUI.featureCardSlot != null)
                    ? resultSummaryUI.featureCardSlot : resultGrid;
            GameObject cardObj = SpawnCard(spawnTarget, unit);

            // Epic/Legendary: 카드 탭 → 플립 후 활성화 / Common·Rare: 즉시 활성화
            bool needsFlip = unit.rarity == UnitRarity.Epic || unit.rarity == UnitRarity.Legendary;
            if (needsFlip)
            {
                CardUI card = cardObj?.GetComponent<CardUI>();
                if (card != null)
                {
                    bool flipDone = false;
                    card.EnableTapToFlip(() => flipDone = true);
                    yield return new WaitUntil(() => flipDone);
                }
            }

            // 공개 완료 → 닫기 + 다시뽑기 활성화
            _isPulling = false;
            if (pullAgainButton != null) pullAgainButton.interactable = true;
            if (resultSummaryUI?.confirmButton != null) resultSummaryUI.confirmButton.interactable = true;
        }

        // ─────────────────────────────────────────────
        // 10연 시퀀스
        // ─────────────────────────────────────────────

        IEnumerator TenPullSequence()
        {
            if (currentResults.Count == 0) yield break;

            // 오프닝 VFX (bestUnit 등급 기준, 카드 없이)
            currentFeatureIdx = FindHighestRarityIndex(currentResults);
            UnitDefinition featureUnit = currentResults[currentFeatureIdx];

            bool hasFanfare = currentResults.Exists(u => u != null && u.rarity >= UnitRarity.Epic);
            if (hasFanfare) AudioManager.Instance?.Play("sfx_gacha_fanfare");

            // 이전 결과 클리어 후 VFX (깨끗한 배경에서 재생), 완료 후 패널 전환
            ClearPreviousResult();
            if (vfxController != null)
                yield return StartCoroutine(vfxController.PlayVFX(featureUnit.rarity));

            AudioManager.Instance?.Play(GetGradeSoundId(featureUnit));

            // ResultPanel 직접 전환 (9장 그리드 → bestUnit 순서는 ResultSummaryUI가 처리)
            ShowResultPanel();
        }

        // ─────────────────────────────────────────────
        // 패널 전환
        // ─────────────────────────────────────────────

        void ShowWaitingPanel()
        {
            _isPulling = false;
            SetSkipMode(false);
            SetPanelActive(waitingPanel, true);
            SetPanelActive(revealPanel,  false);
            SetPanelActive(resultPanel,  false);
            UpdateHUD();
        }

        // 패널만 열고 카드 빌드는 하지 않음 (1연용)
        void OpenResultPanel()
        {
            SetPanelActive(waitingPanel, false);
            SetPanelActive(revealPanel,  false);
            SetPanelActive(resultPanel,  true);
            if (pullAgainButton != null) pullAgainButton.interactable = false;
            UpdateHUD();
        }

        void ShowResultPanel()
        {
            SetPanelActive(waitingPanel, false);
            SetPanelActive(revealPanel,  false);
            SetPanelActive(resultPanel,  true);
            BuildResultPanel();
            UpdateHUD();

        }

        void BuildResultPanel()
        {
            if (resultSummaryUI != null)
            {
                if (lastPullWasTen)
                {
                    SetSkipMode(true);
                    resultSummaryUI.RunSetupSequence(currentResults, preExistingUnits, currentFeatureIdx,
                        onComplete: () =>
                        {
                            _isPulling = false;
                            if (skipButton != null) skipButton.gameObject.SetActive(false);
                        },
                        onAllFlipped: () =>
                        {
                            if (pullAgainButton != null) pullAgainButton.interactable = true;
                        });
                }
                else
                {
                    resultSummaryUI.Setup(currentResults, preExistingUnits, currentFeatureIdx);
                    _isPulling = false;
                }
                return;
            }

            // ResultSummaryUI 미연결 시 폴백
            ClearGrid(resultGrid);
            foreach (var unit in currentResults)
                SpawnCard(resultGrid, unit);
        }

        void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }

        // ─────────────────────────────────────────────
        // 버튼 핸들러
        // ─────────────────────────────────────────────

        void OnSkipClicked()
        {
            if (resultSummaryUI != null) resultSummaryUI.SkipSequence();
        }

        void OnPullAgainClicked()
        {
            if (pullAgainButton != null) pullAgainButton.interactable = false;
            if (lastPullWasTen) OnTenPullClicked();
            else                OnSinglePullClicked();
        }

        // 시퀀스 중: 스킵 버튼 표시 + 다시뽑기 잠금 / 완료: 반대
        void SetSkipMode(bool isSkipping)
        {
            if (skipButton      != null) skipButton.gameObject.SetActive(isSkipping);
            if (pullAgainButton != null) pullAgainButton.interactable = !isSkipping;
        }

        void OnCloseResultClicked() => ShowWaitingPanel();

        void OnBackClicked()
        {
            if (revealCoroutine != null) StopCoroutine(revealCoroutine);
            SceneManager.LoadScene("MainMenu");
        }

        // ─────────────────────────────────────────────
        // 헬퍼
        // ─────────────────────────────────────────────

        void SnapshotExistingUnits()
        {
            preExistingUnits.Clear();
            if (PlayerInventory.Instance == null) return;
            foreach (var u in PlayerInventory.Instance.ownedUnits)
                if (u != null) preExistingUnits.Add(u);
        }

        int FindHighestRarityIndex(List<UnitDefinition> results)
        {
            int best = 0;
            for (int i = 1; i < results.Count; i++)
                if (results[i] != null && results[best] != null &&
                    results[i].rarity > results[best].rarity)
                    best = i;
            return best;
        }

        GameObject SpawnCard(Transform grid, UnitDefinition unit)
        {
            if (gachaCardPrefab == null || grid == null) return null;
            bool isNew = !preExistingUnits.Contains(unit);
            GameObject cardObj = Instantiate(gachaCardPrefab, grid);

            RectTransform gridRect = grid as RectTransform;
            if (gridRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);

            CardUI card = cardObj.GetComponent<CardUI>();
            if (card != null) card.Initialize(unit, isNew);
            return cardObj;
        }

        void ClearPreviousResult()
        {
            SetPanelActive(resultPanel, false);
            ClearGrid(resultGrid);
            if (singlePullSlot != null) ClearGrid(singlePullSlot);
            if (resultSummaryUI != null) resultSummaryUI.Clear();
        }

        void ClearGrid(Transform grid)
        {
            if (grid == null) return;
            foreach (Transform child in grid)
                Destroy(child.gameObject);
        }

        void UpdateHUD()
        {
            if (PlayerInventory.Instance == null) return;

            if (gemText != null)
                gemText.text = PlayerInventory.Instance.gems.ToString("N0");

            if (gachaSystem != null)
            {
                if (pityText != null)
                    pityText.text = gachaSystem.PityCounter.ToString();

                if (pityBarFill != null)
                {
                    float fillAmount = (float)gachaSystem.PityCounter / gachaSystem.pityCap;
                    pityBarFill.anchorMin = new Vector2(0f, 0f);
                    pityBarFill.anchorMax = new Vector2(fillAmount, 1f);
                    pityBarFill.offsetMin = Vector2.zero;
                    pityBarFill.offsetMax = Vector2.zero;
                }

                if (singlePullButton != null)
                    singlePullButton.interactable =
                        PlayerInventory.Instance.gems >= gachaSystem.singlePullCost;
                if (tenPullButton != null)
                    tenPullButton.interactable =
                        PlayerInventory.Instance.gems >= gachaSystem.tenPullCost;
            }
        }

        string GetGradeSoundId(UnitDefinition unit)
        {
            switch (unit.rarity)
            {
                case UnitRarity.Legendary: return "sfx_gacha_grade_ssr";
                case UnitRarity.Epic:      return "sfx_gacha_grade_sr";
                case UnitRarity.Rare:      return "sfx_gacha_grade_r";
                default:                   return "sfx_gacha_grade_n";
            }
        }
    }
}
