using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

namespace Chess.Presentation
{
    /// <summary>
    /// 가챠 VFX 링 확장 이펙트.
    /// 프리팹 자식으로 Image를 최대 4개 배치하고 rings 배열에 할당.
    /// GachaVFXController에서 PlayRings(count) 코루틴으로 호출.
    /// </summary>
    public class RingEffect : MonoBehaviour
    {
        [Header("링 Image 배열 (프리팹 자식, 최대 4개)")]
        public Image[] rings;

        [Header("링 설정")]
        public float expandScale       = 3.0f;
        public float expandDuration    = 0.6f;
        public float delayBetweenRings = 0.12f;
        [Range(0.1f, 1f), Tooltip("expandDuration 대비 페이드아웃 완료 비율 (0.25 = 스케일 70% 시점)")]
        public float alphaFadeRatio    = 0.25f;

        public IEnumerator PlayRings(int count, Color rarityColor)
        {
            int playCount = Mathf.Min(count, rings.Length);

            // 전체 링 숨김 (Instantiate 시 기본 스프라이트 노출 방지)
            foreach (var r in rings)
                if (r != null) r.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0f);

            for (int i = 0; i < playCount; i++)
            {
                Image ring = rings[i];
                if (ring == null) continue;

                // 초기 상태 리셋 + 희귀도 색상 적용
                ring.transform.localScale = Vector3.one;
                ring.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 1f);

                // Scale + Fade 동시 재생 (fire-and-forget)
                ring.transform
                    .DOScale(expandScale, expandDuration)
                    .SetEase(Ease.OutCubic);
                ring.DOFade(0f, expandDuration * alphaFadeRatio).SetEase(Ease.OutQuad);

                if (i < playCount - 1)
                    yield return new WaitForSeconds(delayBetweenRings);
            }

            // 마지막 링 애니메이션이 끝날 때까지 대기
            yield return new WaitForSeconds(expandDuration);
        }
    }
}
