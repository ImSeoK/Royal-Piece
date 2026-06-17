using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Chess.Core;
using DG.Tweening;

namespace Chess.Presentation
{
    /// <summary>
    /// 가챠 카드 1장.
    /// - Common/Rare  : Initialize 즉시 앞면 표시
    /// - Epic/Legendary: Initialize 시 뒷면(심볼) 표시 → GachaVFXController가 FlipToFront() 호출
    /// </summary>
    public class CardUI : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        // Inspector 필드
        // ─────────────────────────────────────────────

        [Header("앞/뒷면 루트 오브젝트")]
        public GameObject frontFaceRoot;
        public GameObject backFaceRoot;

        [Header("앞면 — UI 참조")]
        public Image cardImage;          // 배경+테두리+코너 통합 이미지 (등급별 교체)
        public Image unitImage;
        public TextMeshProUGUI unitNameText;
        public TextMeshProUGUI rarityText;
        public TextMeshProUGUI unitClassText;
        public TextMeshProUGUI acquisitionBadge; // "신규" / "보유"

        [Header("앞면 — 카드 전체 스프라이트 (등급별 4종)")]
        public Sprite cardCommon;
        public Sprite cardRare;
        public Sprite cardEpic;
        public Sprite cardLegendary;

        [Header("뒷면 — UI 참조")]
        public Image backCardImage;      // 뒷면 카드 아트워크 전체 (등급별 교체)
        public Image symbolImage;        // 기물 타입 심볼 (아트워크 위에 얹힘)

        [Header("뒷면 — 카드 전체 스프라이트 (Epic / Legendary 2종)")]
        public Sprite backCardEpic;
        public Sprite backCardLegendary;

        [Header("슬라이드인 설정")]
        public float slideDistance = 120f;
        public float slideDuration = 0.75f;

        [Header("플립 설정")]
        public float flipHalfDuration = 0.15f;

        // ─────────────────────────────────────────────
        // 내부 상태
        // ─────────────────────────────────────────────

        private RectTransform rectTransform;
        private EdgePulse edgePulse;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            edgePulse     = GetComponentInChildren<EdgePulse>();
        }

        // ─────────────────────────────────────────────
        // 외부 진입점
        // ─────────────────────────────────────────────

        /// <param name="forceShowFront">true이면 Epic/Legendary도 앞면으로 시작 (결과 화면 피처 카드용)</param>
        /// <param name="slideIn">false이면 등장 애니메이션 없이 즉시 표시 (그리드 소형 카드용)</param>
        public void Initialize(UnitDefinition unit, bool isNew = true, bool forceShowFront = false, bool slideIn = true)
        {
            if (unit == null) return;

            BindFrontFace(unit, isNew);
            BindBackFace(unit);
            if (slideIn) PlaySlideIn();

            bool showBackFirst = !forceShowFront &&
                                 (unit.rarity == UnitRarity.Epic ||
                                  unit.rarity == UnitRarity.Legendary);

            if (showBackFirst) ShowBackFace();
            else               ShowFrontFace();
        }

        /// <summary>
        /// 뒷면 상태에서 탭 시 FlipToFront를 실행하도록 활성화.
        /// onFlipComplete는 플립 애니메이션 완료 후 호출.
        /// </summary>
        public void EnableTapToFlip(System.Action onFlipComplete = null)
        {
            Button btn = GetComponent<Button>();
            if (btn == null) btn = gameObject.AddComponent<Button>();

            btn.interactable = true;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                btn.interactable = false;
                StartCoroutine(FlipAndNotify(onFlipComplete));
            });
        }

        IEnumerator FlipAndNotify(System.Action callback)
        {
            yield return StartCoroutine(FlipToFront());
            callback?.Invoke();
        }

        // ─────────────────────────────────────────────
        // 앞/뒷면 전환
        // ─────────────────────────────────────────────

        public void ShowFrontFace()
        {
            if (frontFaceRoot != null) frontFaceRoot.SetActive(true);
            if (backFaceRoot  != null) backFaceRoot.SetActive(false);
        }

        public void ShowBackFace()
        {
            if (frontFaceRoot != null) frontFaceRoot.SetActive(false);
            if (backFaceRoot  != null) backFaceRoot.SetActive(true);
        }

        /// <summary>
        /// 뒷면 → 앞면 플립 (Y스케일 0 중간에 면 교체).
        /// GachaVFXController에서 VFX 완료 후 호출.
        /// </summary>
        public IEnumerator FlipToFront()
        {
            // 1. X scale 1 → 0 (접히는 동작)
            yield return transform
                .DOScaleX(0f, flipHalfDuration)
                .SetEase(Ease.InSine)
                .WaitForCompletion();

            // 2. 중간 지점: 앞면으로 교체
            ShowFrontFace();

            // 3. X scale 0 → 1 (펼쳐지는 동작)
            yield return transform
                .DOScaleX(1f, flipHalfDuration)
                .SetEase(Ease.OutSine)
                .WaitForCompletion();

            // 4. Legendary이면 EdgePulse 활성화 (플립 완료 후)
            edgePulse?.Pulse();
        }

        // ─────────────────────────────────────────────
        // 데이터 바인딩
        // ─────────────────────────────────────────────

        void BindFrontFace(UnitDefinition unit, bool isNew)
        {
            // 카드 전체 이미지 교체
            if (cardImage != null)
            {
                Sprite card = GetCardSprite(unit.rarity);
                if (card != null) cardImage.sprite = card;
            }

            if (unitImage != null && unit.icon != null)
            {
                unitImage.sprite = unit.icon;
                unitImage.color  = Color.white;
            }

            if (unitNameText != null)
                unitNameText.text = unit.unitName;

            if (rarityText != null)
            {
                rarityText.text  = GetRarityText(unit.rarity);
                rarityText.color = GetRarityColor(unit.rarity);
            }

            if (unitClassText != null)
                unitClassText.text = unit.GetTypeText();

            if (acquisitionBadge != null)
                acquisitionBadge.text = isNew ? "신규" : "보유";
        }

        void BindBackFace(UnitDefinition unit)
        {
            // 뒷면 카드 전체 이미지 교체
            if (backCardImage != null)
            {
                Sprite back = GetBackCardSprite(unit.rarity);
                if (back != null) backCardImage.sprite = back;
            }

            // 기물 타입 심볼 — UnitDefinition.typeSymbol 사용
            if (symbolImage != null && unit.typeSymbol != null)
            {
                symbolImage.sprite = unit.typeSymbol;
                symbolImage.color  = GetSymbolColor(unit.rarity);
            }
        }

        // ─────────────────────────────────────────────
        // 슬라이드인
        // ─────────────────────────────────────────────

        void PlaySlideIn()
        {
            if (rectTransform == null) return;

            // DOAnchorPos는 Grid Layout Group과 충돌 → DOScaleY로 교체
            transform.localScale = new Vector3(1f, 0f, 1f);
            transform.DOScaleY(1f, slideDuration).SetEase(Ease.OutBack);
        }

        // ─────────────────────────────────────────────
        // 등급별 리소스 매핑
        // ─────────────────────────────────────────────

        Sprite GetCardSprite(UnitRarity r) => r switch
        {
            UnitRarity.Rare      => cardRare,
            UnitRarity.Epic      => cardEpic,
            UnitRarity.Legendary => cardLegendary,
            _                    => cardCommon,
        };

        Sprite GetBackCardSprite(UnitRarity r) => r switch
        {
            UnitRarity.Legendary => backCardLegendary,
            UnitRarity.Epic      => backCardEpic,
            _                    => null,
        };

        string GetRarityText(UnitRarity r) => r switch
        {
            UnitRarity.Common    => "커먼",
            UnitRarity.Rare      => "레어",
            UnitRarity.Epic      => "에픽",
            UnitRarity.Legendary => "레전더리",
            _                    => "",
        };

        Color GetRarityColor(UnitRarity r) => r switch
        {
            UnitRarity.Common    => new Color(0.54f,  0.61f,  0.71f),
            UnitRarity.Rare      => new Color(0.29f,  0.62f,  1.00f),
            UnitRarity.Epic      => new Color(0.710f, 0.482f, 1.0f),
            UnitRarity.Legendary => new Color(1.0f,   0.776f, 0.259f),
            _                    => Color.white,
        };

        Color GetSymbolColor(UnitRarity r) => r switch
        {
            UnitRarity.Epic      => new Color(0.710f, 0.482f, 1.0f,   0.8f),
            UnitRarity.Legendary => new Color(1.0f,   0.776f, 0.259f, 0.8f),
            _                    => new Color(1f,     1f,     1f,     0.6f),
        };
    }
}
