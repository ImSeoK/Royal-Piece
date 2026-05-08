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
        public TextMeshProUGUI goldText;
        public TextMeshProUGUI pityText;
        public RectTransform pityBarFill;   // PityBarFill RectTransform ����
        public Button backButton;

        [Header("��� ȭ�� (WaitingPanel)")]
        public GameObject waitingPanel;
        public Button singlePullButton;
        public Button tenPullButton;

        [Header("ī�� ���� ���� (RevealPanel)")]
        public GameObject revealPanel;
        public Transform revealGrid;
        public Button skipButton;

        [Header("��� Ȯ�� (ResultPanel)")]
        public GameObject resultPanel;
        public Transform resultGrid;
        public Button pullAgainButton;
        public Button closeResultButton;

        [Header("������")]
        public GameObject gachaCardPrefab;

        [Header("���� ����")]
        public float cardRevealInterval = 0.3f;

        [Header("�ý���")]
        public GachaSystem gachaSystem;

        private List<UnitDefinition> currentResults = new List<UnitDefinition>();
        private bool isRevealing = false;
        private Coroutine revealCoroutine;
        private bool lastPullWasTen = false;

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
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);
            if (singlePullButton != null)
                singlePullButton.onClick.AddListener(OnSinglePullClicked);
            if (tenPullButton != null)
                tenPullButton.onClick.AddListener(OnTenPullClicked);
            if (skipButton != null)
                skipButton.onClick.AddListener(OnSkipClicked);
            if (pullAgainButton != null)
                pullAgainButton.onClick.AddListener(OnPullAgainClicked);
            if (closeResultButton != null)
                closeResultButton.onClick.AddListener(OnCloseResultClicked);
        }

        void UpdateHUD()
        {
            if (PlayerInventory.Instance == null) return;

            if (goldText != null)
                goldText.text = PlayerInventory.Instance.currency.ToString("N0");

            if (gachaSystem != null)
            {
                // õ�� �ؽ�Ʈ
                if (pityText != null)
                    pityText.text = gachaSystem.PityCounter.ToString();

                // õ�� �� fill (0~1 ���� ������ anchorMax.x ����)
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
                        PlayerInventory.Instance.currency >= gachaSystem.singlePullCost;
                if (tenPullButton != null)
                    tenPullButton.interactable =
                        PlayerInventory.Instance.currency >= gachaSystem.tenPullCost;
            }
        }

        void ShowWaitingPanel()
        {
            SetPanelActive(waitingPanel, true);
            SetPanelActive(revealPanel, false);
            SetPanelActive(resultPanel, false);
            UpdateHUD();
        }

        void ShowRevealPanel()
        {
            SetPanelActive(waitingPanel, false);
            SetPanelActive(revealPanel, true);
            SetPanelActive(resultPanel, false);
        }

        void ShowResultPanel()
        {
            SetPanelActive(waitingPanel, false);
            SetPanelActive(revealPanel, false);
            SetPanelActive(resultPanel, true);
            BuildResultPanel();
            UpdateHUD();
        }

        void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }

        void OnSinglePullClicked()
        {
            lastPullWasTen = false;
            currentResults.Clear();
            UnitDefinition result;
            if (gachaSystem.TryPullSingle(out result))
            {
                if (result != null) currentResults.Add(result);
                StartReveal();
            }
        }

        void OnTenPullClicked()
        {
            lastPullWasTen = true;
            currentResults.Clear();
            List<UnitDefinition> results;
            if (gachaSystem.TryPullTen(out results))
            {
                currentResults = results;
                StartReveal();
            }
        }

        void StartReveal()
        {
            ClearGrid(revealGrid);
            ShowRevealPanel();
            if (revealCoroutine != null) StopCoroutine(revealCoroutine);
            revealCoroutine = StartCoroutine(RevealCardsSequentially());
        }

        IEnumerator RevealCardsSequentially()
        {
            isRevealing = true;
            if (skipButton != null) skipButton.gameObject.SetActive(true);

            for (int i = 0; i < currentResults.Count; i++)
            {
                if (!isRevealing) break;
                SpawnCard(revealGrid, currentResults[i]);
                yield return new WaitForSeconds(cardRevealInterval);
            }

            isRevealing = false;
            yield return new WaitForSeconds(0.5f);
            ShowResultPanel();
        }

        void OnSkipClicked()
        {
            if (revealCoroutine != null)
            {
                StopCoroutine(revealCoroutine);
                revealCoroutine = null;
            }
            isRevealing = false;
            ClearGrid(revealGrid);
            foreach (var unit in currentResults)
                SpawnCard(revealGrid, unit);
            StartCoroutine(DelayedShowResult(0.3f));
        }

        IEnumerator DelayedShowResult(float delay)
        {
            yield return new WaitForSeconds(delay);
            ShowResultPanel();
        }

        void BuildResultPanel()
        {
            ClearGrid(resultGrid);
            foreach (var unit in currentResults)
                SpawnCard(resultGrid, unit);
        }

        void OnPullAgainClicked()
        {
            if (lastPullWasTen)
                OnTenPullClicked();
            else
                OnSinglePullClicked();
        }

        void OnCloseResultClicked() => ShowWaitingPanel();

        void SpawnCard(Transform grid, UnitDefinition unit)
        {
            if (gachaCardPrefab == null || grid == null) return;
            GameObject cardObj = Instantiate(gachaCardPrefab, grid);
            GachaResultCard card = cardObj.GetComponent<GachaResultCard>();
            if (card != null) card.Initialize(unit);
        }

        void ClearGrid(Transform grid)
        {
            if (grid == null) return;
            foreach (Transform child in grid)
                Destroy(child.gameObject);
        }

        void OnBackClicked()
        {
            if (revealCoroutine != null) StopCoroutine(revealCoroutine);
            SceneManager.LoadScene("MainMenu");
        }
    }
}