// System
using System.Collections.Generic;

// Unity
using UnityEngine;
using UnityEngine.UI;

namespace Minsung.Visual
{
    // 화면 균열 연출의 조각 하나 - 임의의 볼록다각형(직선 자르기로 만들어진 영역)을 그리고 지정된 RenderTexture를 화면 정렬 UV로 샘플링한다
    // 부모에 RectMask2D(풀스크린)를 두면 화면 밖으로 넘치는 부분이 잘려 화면 안쪽만 남는다
    [RequireComponent(typeof(CanvasRenderer))]
    public class ScreenTearShard : MaskableGraphic
    {
        /****************************************
        *                Fields
        ****************************************/

        private RenderTexture _source;
        private List<Vector2> _polygonLocal; // 그래픽 로컬 좌표계 기준 볼록다각형 정점(반시계 또는 시계 한 방향으로 정렬)

        // 기본 UI 셰이더가 이 텍스처를 color와 곱해 그린다 - RawImage와 동일 방식
        public override Texture mainTexture => _source != null ? _source : Texture2D.whiteTexture;

        /****************************************
        *                Methods
        ****************************************/

        // 코디네이터(ScreenTearOverlay)가 조각 모양을 주입한다
        public void Setup(RenderTexture source, List<Vector2> polygonLocal)
        {
            _source       = source;
            _polygonLocal = polygonLocal;
            SetMaterialDirty();
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if ((_polygonLocal == null) || (_polygonLocal.Count < 3))
            {
                return;
            }

            Rect rect = GetPixelAdjustedRect();
            Vector2 centroid = ConvexPolygonSplitter.Centroid(_polygonLocal);

            // 중심(팬의 꼭짓점) + 다각형 정점들
            AddVertex(vh, centroid, rect);
            for (int i = 0; i < _polygonLocal.Count; ++i)
            {
                AddVertex(vh, _polygonLocal[i], rect);
            }

            int count = _polygonLocal.Count;
            for (int i = 1; i <= count; ++i)
            {
                int next = (i == count) ? 1 : i + 1;
                vh.AddTriangle(0, i, next);
            }
        }

        // 정점 하나 추가 - UV는 화면 정규화 위치라 카메라를 다르게 움직이면 조각 경계에서 이미지가 어긋나 '깨진 화면'이 된다
        private void AddVertex(VertexHelper vh, Vector2 local, Rect rect)
        {
            Vector2 uv = new Vector2(
                (local.x - rect.x) / rect.width,
                (local.y - rect.y) / rect.height);

            UIVertex vert = UIVertex.simpleVert;
            vert.color    = color;
            vert.position = local;
            vert.uv0      = uv;
            vh.AddVert(vert);
        }
    }
}
