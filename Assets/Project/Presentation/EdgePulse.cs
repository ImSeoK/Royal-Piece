using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Chess.Presentation
{
    /// <summary>
    /// Legendary 카드 테두리 펄스 이펙트.
    /// 카드 프리팹의 테두리 Image에 컴포넌트로 붙인다.
    /// CardUI.Initialize에서 Legendary 등급일 때 Pulse() 호출.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class EdgePulse : MonoBehaviour
    {
        [Header("Shader 글로우 Material (Shader Graph _GlowIntensity 파라미터 노출 필요)")]
        public Material glowMaterial;

        [Header("펄스 설정")]
        public float pulseAlphaMin  = 0.3f;
        public float pulseAlphaMax  = 1.0f;
        public float pulseDuration  = 0.35f;
        public int   loopCount      = 4;

        private Image borderImage;
        private Tween alphaTween;
        private Tween glowTween;

        void Awake()
        {
            borderImage = GetComponent<Image>();
        }

        // ─────────────────────────────────────────────
        // 외부 진입점
        // ─────────────────────────────────────────────

        public void Pulse()
        {
            if (borderImage == null) return;

            // 기존 트윈 중단
            alphaTween?.Kill();
            glowTween?.Kill();

            // 테두리 알파 Yoyo 펄스
            Color c = borderImage.color;
            borderImage.color = new Color(c.r, c.g, c.b, pulseAlphaMax);
            alphaTween = borderImage
                .DOFade(pulseAlphaMin, pulseDuration)
                .SetLoops(loopCount * 2, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);

            // Shader Graph _GlowIntensity 제어 (Material 연결 시)
            if (glowMaterial != null)
            {
                glowMaterial.SetFloat("_GlowIntensity", 0f);
                glowTween = DOTween
                    .To(
                        () => glowMaterial.GetFloat("_GlowIntensity"),
                        v  => glowMaterial.SetFloat("_GlowIntensity", v),
                        1f,
                        pulseDuration
                    )
                    .SetLoops(loopCount * 2, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
        }

        void OnDestroy()
        {
            alphaTween?.Kill();
            glowTween?.Kill();
        }
    }
}
