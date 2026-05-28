using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Chess.Core;
using DG.Tweening;

namespace Chess.Presentation
{
    public class GachaVFXController : MonoBehaviour
    {
        private static Material  _vfxMat;
        private static Texture2D _circleTex;

        [Header("등급별 파티클 프리팹")]
        public GameObject vfxPrefabCommon;
        public GameObject vfxPrefabRare;
        public GameObject vfxPrefabEpic;
        public GameObject vfxPrefabLegendary;

        [Header("링 이펙트 프리팹")]
        public GameObject ringEffectPrefab;

        [Header("화면 플래시 (전체 화면 흰색 Image의 CanvasGroup)")]
        public CanvasGroup flashOverlay;

        [Header("방사형 광선")]
        public Sprite   raySprite;
        public Vector2  raySize = new Vector2(40f, 280f);

        [Header("VFX 생성 위치 부모")]
        public Transform vfxRoot;

        void Awake()
        {
            if (flashOverlay != null) flashOverlay.alpha = 0f;
        }

        // ─────────────────────────────────────────────
        // 외부 진입점
        // ─────────────────────────────────────────────

        /// <summary>
        /// 등급별 VFX 시퀀스 재생. 플립은 포함하지 않음 — 유저 탭 후 GachaManager가 직접 호출.
        /// cardRect null이면 화면 중앙에서 재생 (오프닝 VFX용).
        /// </summary>
        public IEnumerator PlayVFX(UnitRarity rarity, RectTransform cardRect = null)
        {
            Vector3 worldPos = cardRect != null ? cardRect.position : Vector3.zero;

            // 1. 화면 플래시
            PlayFlash(rarity);

            // 2. 파티클 버스트 (fire-and-forget)
            StartCoroutine(SpawnParticle(rarity, worldPos));

            // 3. 방사형 광선 (fire-and-forget — 링과 동시 시작)
            RayBurstEffect rayBurst = SpawnRayBurst(rarity);
            if (rayBurst != null) StartCoroutine(rayBurst.Play());

            // 4. 링 이펙트 (완료 대기)
            int ringCount = GetRingCount(rarity);
            if (ringEffectPrefab != null && ringCount > 0)
            {
                Transform parent = vfxRoot != null ? vfxRoot : transform;
                GameObject ringObj = Instantiate(ringEffectPrefab, parent, false);

                RectTransform ringRect = ringObj.GetComponent<RectTransform>();
                if (ringRect != null)
                {
                    Canvas canvas = GetComponentInParent<Canvas>();
                    Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                        ? canvas.worldCamera : null;
                    RectTransform parentRect = parent as RectTransform;

                    if (cardRect != null && parentRect != null)
                    {
                        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, cardRect.position);
                        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            parentRect, screenPoint, cam, out Vector2 localPoint))
                            ringRect.anchoredPosition = localPoint;
                    }
                    else
                    {
                        ringRect.anchoredPosition = Vector2.zero;
                    }

                    Vector3 lp = ringRect.localPosition;
                    ringRect.localPosition = new Vector3(lp.x, lp.y, 0f);
                }

                RingEffect ring = ringObj.GetComponent<RingEffect>();
                if (ring != null)
                    yield return StartCoroutine(ring.PlayRings(ringCount, GetRarityColor(rarity)));
                Destroy(ringObj);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }

            // 5. 광선 페이드아웃 완료까지 대기 후 제거
            if (rayBurst != null)
            {
                rayBurst.RequestStop(0.3f);
                yield return new WaitForSeconds(0.35f);
                Destroy(rayBurst.gameObject);
            }
        }

        // ─────────────────────────────────────────────
        // 화면 플래시
        // ─────────────────────────────────────────────

        void PlayFlash(UnitRarity rarity)
        {
            if (flashOverlay == null) return;

            float intensity = rarity switch
            {
                UnitRarity.Legendary => 0.75f,
                UnitRarity.Epic      => 0.45f,
                UnitRarity.Rare      => 0.25f,
                _                    => 0.10f,
            };

            flashOverlay.DOKill();
            flashOverlay.alpha = 0f;
            flashOverlay.DOFade(intensity, 0.055f)
                .OnComplete(() => flashOverlay.DOFade(0f, 0.484f).SetEase(Ease.OutCubic));
        }

        // ─────────────────────────────────────────────
        // 파티클 생성 & 수치 코드 세팅
        // ─────────────────────────────────────────────

        IEnumerator SpawnParticle(UnitRarity rarity, Vector3 position)
        {
            GameObject prefab = GetPrefab(rarity);
            if (prefab == null) yield break;

            Transform parent = vfxRoot != null ? vfxRoot : transform;
            GameObject obj = Instantiate(prefab, position, Quaternion.identity, parent);
            int vfxLayer = LayerMask.NameToLayer("VFX");
            if (vfxLayer >= 0) obj.layer = vfxLayer;
            ParticleSystem ps = obj.GetComponent<ParticleSystem>();

            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                SetupParticle(ps, rarity);
                ps.Play();
                yield return new WaitForSeconds(3.0f); // maxLifetime(2.5) + 여유
            }

            Destroy(obj);
        }

        void SetupParticle(ParticleSystem ps, UnitRarity rarity)
        {
            var main     = ps.main;
            var emission = ps.emission;
            var col      = ps.colorOverLifetime;
            var shape    = ps.shape;
            var sol      = ps.sizeOverLifetime;
            var lvol     = ps.limitVelocityOverLifetime;

            main.loop            = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // 중심에서 방사형 버스트
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.05f;

            Color rarityColor;
            int   burstCount;

            switch (rarity)
            {
                case UnitRarity.Common:
                    main.startSpeed    = new ParticleSystem.MinMaxCurve(10f, 14f);
                    main.startSize     = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
                    main.duration      = 0.3f;
                    main.gravityModifier = 0.4f;
                    rarityColor = new Color(0.667f, 0.667f, 0.667f); // #aaaaaa
                    burstCount  = 35;
                    break;

                case UnitRarity.Rare:
                    main.startSpeed    = new ParticleSystem.MinMaxCurve(12f, 16f);
                    main.startSize     = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
                    main.duration      = 0.35f;
                    main.gravityModifier = 0.5f;
                    rarityColor = new Color(0.353f, 0.678f, 1.0f);   // #5aadff
                    burstCount  = 60;
                    break;

                case UnitRarity.Epic:
                    main.startSpeed    = new ParticleSystem.MinMaxCurve(14f, 18f);
                    main.startSize     = new ParticleSystem.MinMaxCurve(0.06f, 0.15f);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
                    main.duration      = 0.4f;
                    main.gravityModifier = 0.6f;
                    rarityColor = new Color(0.710f, 0.482f, 1.0f);   // #b57bff
                    burstCount  = 85;
                    break;

                case UnitRarity.Legendary:
                default:
                    main.startSpeed    = new ParticleSystem.MinMaxCurve(16f, 20f);
                    main.startSize     = new ParticleSystem.MinMaxCurve(0.07f, 0.20f);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
                    main.duration      = 0.5f;
                    main.gravityModifier = 0.7f;
                    rarityColor = new Color(1.0f, 0.776f, 0.259f);   // #ffc642
                    burstCount  = 130;
                    break;
            }

            main.startColor = rarityColor;

            // 연속 방출 제거 — 버스트만 사용
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burstCount) });

            // Color over Lifetime: 등급 색 → 투명
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(rarityColor, 0f),
                    new GradientColorKey(rarityColor, 1f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.7f, 0f),
                    new GradientAlphaKey(0.7f, 0.4f),
                    new GradientAlphaKey(0f, 1f),
                }
            );
            col.enabled = true;
            col.color   = gradient;

            // 크기 감소 (1 → 0)
            sol.enabled = true;
            sol.size    = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            // 속도 감쇠
            lvol.enabled        = true;
            lvol.separateAxes   = false;
            lvol.limitMultiplier = 1f;
            lvol.limit          = new ParticleSystem.MinMaxCurve(0f);
            lvol.dampen         = 0.15f;

            var psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                if (_vfxMat == null)
                {
                    Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                             ?? Shader.Find("Particles/Standard Unlit")
                             ?? Shader.Find("Universal Render Pipeline/Unlit")
                             ?? Shader.Find("Sprites/Default");
                    if (sh != null)
                    {
                        _vfxMat = new Material(sh);
                        _vfxMat.SetTexture("_BaseMap", GetCircleTexture());
                    }
                    else
                    {
                        Debug.LogWarning("[GachaVFX] 파티클 셰이더를 찾지 못했습니다. 파티클이 흰색 사각형으로 렌더됩니다.");
                    }
                }
                if (_vfxMat != null)
                    psr.sharedMaterial = _vfxMat;
            }
        }

        // ─────────────────────────────────────────────
        // 헬퍼
        // ─────────────────────────────────────────────

        static Texture2D GetCircleTexture()
        {
            if (_circleTex != null) return _circleTex;

            const int size = 128;
            _circleTex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            _circleTex.filterMode = FilterMode.Bilinear;
            float center = size * 0.5f;
            // 가우시안 σ = 0.35r → 중심 불투명, 가장자리로 갈수록 자연스럽게 페이드
            float sigma = center * 0.35f;
            float twoSigmaSq = 2f * sigma * sigma;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx   = x + 0.5f - center;
                float dy   = y + 0.5f - center;
                float distSq = dx * dx + dy * dy;
                float alpha  = Mathf.Exp(-distSq / twoSigmaSq);
                // 반지름 밖은 하드컷 없이 가우시안 자체가 자연스럽게 0에 수렴
                _circleTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            _circleTex.Apply();
            return _circleTex;
        }

        // ─────────────────────────────────────────────
        // 방사형 광선 생성
        // ─────────────────────────────────────────────

        RayBurstEffect SpawnRayBurst(UnitRarity rarity)
        {
            if (raySprite == null) return null;

            (int count, float speed) = GetRayParams(rarity);
            if (count == 0) return null;

            Transform parent = vfxRoot != null ? vfxRoot : transform;
            GameObject obj   = new GameObject("RayBurst", typeof(RectTransform));
            obj.transform.SetParent(parent, false);

            RectTransform rt  = obj.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            RayBurstEffect effect = obj.AddComponent<RayBurstEffect>();
            effect.Setup(raySprite, count, speed, GetRarityColor(rarity), raySize);
            return effect;
        }

        (int count, float speed) GetRayParams(UnitRarity rarity) => rarity switch
        {
            UnitRarity.Legendary => (18, 0.15f),
            UnitRarity.Epic      => (10, 0.11f),
            _                    => (0,  0f),
        };

        GameObject GetPrefab(UnitRarity rarity) => rarity switch
        {
            UnitRarity.Legendary => vfxPrefabLegendary,
            UnitRarity.Epic      => vfxPrefabEpic,
            UnitRarity.Rare      => vfxPrefabRare,
            _                    => vfxPrefabCommon,
        };

        int GetRingCount(UnitRarity rarity) => rarity switch
        {
            UnitRarity.Legendary => 4,
            UnitRarity.Epic      => 3,
            UnitRarity.Rare      => 2,
            _                    => 1,
        };

        Color GetRarityColor(UnitRarity rarity) => rarity switch
        {
            UnitRarity.Legendary => new Color(1.0f,   0.776f, 0.259f),
            UnitRarity.Epic      => new Color(0.710f, 0.482f, 1.0f),
            UnitRarity.Rare      => new Color(0.29f,  0.62f,  1.00f),
            _                    => new Color(0.54f,  0.61f,  0.71f),
        };
    }
}
