using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

namespace Chess.Presentation
{
    /// <summary>
    /// 방사형 광선 VFX — UI Image 방식.
    /// GachaVFXController가 Setup() 후 Play() 코루틴을 실행하고,
    /// 종료 시 RequestStop()을 호출한다.
    /// </summary>
    public class RayBurstEffect : MonoBehaviour
    {
        private RectTransform _rotator;
        private CanvasGroup   _canvasGroup;
        private float         _rotationSpeedRad;
        private bool          _stopping        = false;
        private float         _fadeOutDuration = 0.3f;

        public void Setup(Sprite raySprite, int rayCount, float rotationSpeedRad, Color color, Vector2 raySize)
        {
            _rotationSpeedRad = rotationSpeedRad;

            _canvasGroup               = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha         = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable  = false;

            // 회전 컨테이너 (모든 광선의 부모)
            GameObject rotatorObj = new GameObject("RayRotator", typeof(RectTransform));
            rotatorObj.transform.SetParent(transform, false);
            _rotator                  = rotatorObj.GetComponent<RectTransform>();
            _rotator.anchorMin        = new Vector2(0.5f, 0.5f);
            _rotator.anchorMax        = new Vector2(0.5f, 0.5f);
            _rotator.sizeDelta        = Vector2.zero;
            _rotator.anchoredPosition = Vector2.zero;

            float angleStep = 360f / rayCount;
            for (int i = 0; i < rayCount; i++)
            {
                GameObject rayObj = new GameObject($"Ray_{i}", typeof(RectTransform));
                rayObj.transform.SetParent(_rotator, false);

                Image img         = rayObj.AddComponent<Image>();
                img.sprite        = raySprite;
                img.color         = color;
                img.raycastTarget = false;

                RectTransform rt  = rayObj.GetComponent<RectTransform>();
                rt.pivot            = new Vector2(0.5f, -15f / raySize.y);   // 하단이 중심에서 15px 떨어진 지점부터 시작
                rt.anchorMin        = new Vector2(0.5f, 0.5f);
                rt.anchorMax        = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta        = raySize;
                rt.localRotation    = Quaternion.Euler(0f, 0f, angleStep * i);
            }
        }

        /// <summary>링 이펙트 완료 시점에 호출 — 페이드아웃 후 코루틴 종료.</summary>
        public void RequestStop(float fadeOutDuration = 0.3f)
        {
            _fadeOutDuration = fadeOutDuration;
            _stopping        = true;
        }

        /// <summary>
        /// 등장(fadeInDuration) → 회전 → RequestStop 수신 후 퇴장.
        /// </summary>
        public IEnumerator Play(float fadeInDuration = 0.22f)
        {
            // 등장 페이드인과 회전을 동시에 시작
            _canvasGroup.DOFade(0.2f, fadeInDuration).SetEase(Ease.OutQuad);

            // 회전 (매 프레임 rotationSpeedRad rad 회전, RequestStop 호출까지)
            while (!_stopping)
            {
                if (_rotator != null)
                    _rotator.Rotate(0f, 0f, _rotationSpeedRad * Mathf.Rad2Deg);
                yield return null;
            }

            // 퇴장 — 페이드아웃 중에도 회전 유지
            _canvasGroup.DOKill();
            _canvasGroup.DOFade(0f, _fadeOutDuration).SetEase(Ease.InQuad);
            float elapsed = 0f;
            while (elapsed < _fadeOutDuration)
            {
                if (_rotator != null)
                    _rotator.Rotate(0f, 0f, _rotationSpeedRad * Mathf.Rad2Deg);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
}
