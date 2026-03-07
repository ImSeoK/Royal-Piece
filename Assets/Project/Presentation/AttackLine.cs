using UnityEngine;
using System.Collections;

namespace Chess.Presentation
{
    public class AttackLine : MonoBehaviour
    {
        private LineRenderer lineRenderer;
        public float fadeDuration = 0.15f;

        void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        public void Initialize(Vector3 start, Vector3 end)
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

            // 약간 위로 올려서 보이게
            start.z = -1;
            end.z = -1;

            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);

            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;

            Color red = new Color(1f, 0f, 0f, 1f);
            lineRenderer.startColor = red;
            lineRenderer.endColor = red;

            lineRenderer.sortingOrder = 5;

            StartCoroutine(FadeOut());
        }

        IEnumerator FadeOut()
        {
            float elapsed = 0f;
            Color startColor = new Color(1f, 0f, 0f, 1f);

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1 - (elapsed / fadeDuration);

                Color c = startColor;
                c.a = alpha;

                lineRenderer.startColor = c;
                lineRenderer.endColor = c;

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}