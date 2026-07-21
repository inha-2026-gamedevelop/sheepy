// System
using System.Collections.Generic;

// Unity
using UnityEngine;

namespace Minsung.Visual
{
    // 볼록 다각형을 직선으로 순차 분할하는 순수 기하 유틸 - 화면 균열(유리 자르기) 연출의 조각 모양 계산에 쓴다
    // 화면 전체(사각형)에서 시작해 선을 하나씩 그을 때마다, 그 선이 실제로 가로지르는 조각 하나만 둘로 쪼갠다(종이 자르기와 동일한 동작)
    public static class ConvexPolygonSplitter
    {
        private const float MIN_AREA = 1f; // 로컬(px) 단위 - 선이 조각 모서리를 스치기만 할 때 생기는 면적 0에 가까운 슬리버 방지

        /// <summary> lineP를 지나고 lineDir 방향인 무한 직선으로 regions를 분할한 새 목록을 반환한다(원본 불변) - 선이 실제로 가로지르는 조각만 둘로 나뉘고 나머지는 그대로 남는다 </summary>
        public static List<List<Vector2>> SplitByLine(List<List<Vector2>> regions, Vector2 lineP, Vector2 lineDir)
        {
            List<List<Vector2>> result = new List<List<Vector2>>(regions.Count + 1);
            Vector2 normal = new Vector2(-lineDir.y, lineDir.x).normalized; // 이 법선 기준 양/음 반평면으로 나눈다

            for (int r = 0; r < regions.Count; ++r)
            {
                List<Vector2> poly = regions[r];
                List<Vector2> pos = ClipHalfPlane(poly, lineP, normal);
                List<Vector2> neg = ClipHalfPlane(poly, lineP, -normal);

                bool posValid = (pos.Count >= 3) && (PolygonArea(pos) > MIN_AREA);
                bool negValid = (neg.Count >= 3) && (PolygonArea(neg) > MIN_AREA);

                if (posValid && negValid)
                {
                    result.Add(pos);
                    result.Add(neg);
                }
                else
                {
                    result.Add(poly); // 선이 이 조각을 실질적으로 가로지르지 않음 - 그대로 유지
                }
            }
            return result;
        }

        // Sutherland-Hodgman 반평면 클리핑 - dot(normal, point-lineP) >= 0인 쪽만 남긴다
        private static List<Vector2> ClipHalfPlane(List<Vector2> poly, Vector2 lineP, Vector2 normal)
        {
            List<Vector2> outPoly = new List<Vector2>();
            int n = poly.Count;
            for (int i = 0; i < n; ++i)
            {
                Vector2 cur  = poly[i];
                Vector2 next = poly[(i + 1) % n];
                float dCur  = Vector2.Dot(normal, cur - lineP);
                float dNext = Vector2.Dot(normal, next - lineP);
                bool curIn  = dCur >= 0f;
                bool nextIn = dNext >= 0f;

                if (curIn)
                {
                    outPoly.Add(cur);
                }
                if (curIn != nextIn)
                {
                    float t = dCur / (dCur - dNext);
                    outPoly.Add(Vector2.Lerp(cur, next, t));
                }
            }
            return outPoly;
        }

        public static float PolygonArea(List<Vector2> poly)
        {
            float sum = 0f;
            int n = poly.Count;
            for (int i = 0; i < n; ++i)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % n];
                sum += (a.x * b.y) - (b.x * a.y);
            }
            return Mathf.Abs(sum) * 0.5f;
        }

        public static Vector2 Centroid(List<Vector2> poly)
        {
            Vector2 c = Vector2.zero;
            for (int i = 0; i < poly.Count; ++i)
            {
                c += poly[i];
            }
            return c / Mathf.Max(1, poly.Count);
        }

        /// <summary> 점 p가 볼록다각형 poly 내부(또는 경계)에 있는지 - 모든 변에 대해 같은 쪽이면 내부. 정점 감김 방향과 무관 </summary>
        public static bool ContainsPoint(List<Vector2> poly, Vector2 p)
        {
            int n = poly.Count;
            if (n < 3)
            {
                return false;
            }
            bool hasPos = false;
            bool hasNeg = false;
            for (int i = 0; i < n; ++i)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % n];
                // 변 a->b 기준 p의 외적 부호(어느 쪽에 있는지)
                float cross = ((b.x - a.x) * (p.y - a.y)) - ((b.y - a.y) * (p.x - a.x));
                if (cross > 0.0001f)  { hasPos = true; }
                if (cross < -0.0001f) { hasNeg = true; }
                if (hasPos && hasNeg) // 서로 다른 쪽이 섞이면 외부
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary> lineP/lineDir 직선이 poly 경계와 만나는 두 지점(균열선/파편 연출용)을 찾는다. 가로지르지 않으면 false </summary>
        public static bool TryGetCrackSegment(List<Vector2> poly, Vector2 lineP, Vector2 lineDir, out Vector2 p0, out Vector2 p1)
        {
            Vector2 normal = new Vector2(-lineDir.y, lineDir.x).normalized;
            Vector2 hit0 = Vector2.zero;
            Vector2 hit1 = Vector2.zero;
            int hitCount = 0;
            int n = poly.Count;

            for (int i = 0; i < n; ++i)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % n];
                float da = Vector2.Dot(normal, a - lineP);
                float db = Vector2.Dot(normal, b - lineP);
                if ((da >= 0f) != (db >= 0f))
                {
                    float t = da / (da - db);
                    Vector2 hit = Vector2.Lerp(a, b, t);
                    if (hitCount == 0)
                    {
                        hit0 = hit;
                    }
                    else
                    {
                        hit1 = hit;
                    }
                    ++hitCount;
                }
            }

            p0 = hit0;
            p1 = hit1;
            return hitCount >= 2;
        }
    }
}
