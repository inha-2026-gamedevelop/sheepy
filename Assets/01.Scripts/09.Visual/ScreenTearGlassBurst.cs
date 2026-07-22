// System
using System.Collections.Generic;

// Unity
using UnityEngine;
using UnityEngine.UI;

namespace Minsung.Visual
{
    // 균열선이 그어지는 순간의 유리 파편 버스트 - 다양한 모양의 유리 조각(볼록다각형) + 흰 스파클을 절차적으로 생성해
    // 절단면에서 방사형으로 튕겨나가며 페이드아웃한다. 유리 조각은 가장자리가 밝고(빛 반사) 가운데가 투명해(비침) 유리처럼 보인다
    // ParticleSystem 대신 UI 메시로 그려 Screen Space Overlay Canvas 안에서 안전하게 합성된다
    [RequireComponent(typeof(CanvasRenderer))]
    public class ScreenTearGlassBurst : MaskableGraphic
    {
        /****************************************
        *                Types
        ****************************************/

        private struct Particle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float   RotationDeg;
            public float   AngularVelocityDeg;
            public float   Size;
            public float   Life;       // 0~1, 1에서 시작해 0으로
            public float   LifeSpeed;  // 초당 감소량
            public bool    IsShard;    // true=유리 조각, false=흰 스파클
            public int     ShapeIndex; // 유리 조각 모양 템플릿 인덱스
        }

        /****************************************
        *                Fields
        ****************************************/

        private readonly List<Particle> _particles = new List<Particle>();
        private List<Vector2[]> _shapes; // 다양한 유리 조각 모양(단위 크기 볼록다각형) - 첫 Emit에서 1회 생성

        // 가장자리는 밝게(빛 반사), 가운데는 거의 투명(뒤가 비침) - 유리 느낌
        private Color _shardEdgeColor = new Color(0.9f, 0.97f, 1f, 0.85f);
        private Color _shardCoreColor = new Color(0.75f, 0.9f, 1f, 0.1f);
        private Color _sparkleColor   = new Color(1f, 1f, 1f, 0.95f);

        public override Texture mainTexture => Texture2D.whiteTexture;
        public bool IsFinished => _particles.Count == 0;

        /****************************************
        *                Methods
        ****************************************/

        // 선(local, 두 끝점)을 따라 파편을 흩뿌린다 - shardCount/sparkleCount로 밀도 조절
        public void Emit(Vector2 lineStart, Vector2 lineEnd, int shardCount, int sparkleCount,
            float speedMin, float speedMax, float lifeMin, float lifeMax)
        {
            Vector2 dir = (lineEnd - lineStart);
            float len = dir.magnitude;
            if (len < 0.001f)
            {
                return;
            }
            dir /= len;
            Vector2 perp = new Vector2(-dir.y, dir.x);

            EnsureShapes();

            for (int i = 0; i < shardCount; ++i)
            {
                SpawnParticle(lineStart, lineEnd, dir, perp, speedMin, speedMax, lifeMin, lifeMax, isShard: true);
            }
            for (int i = 0; i < sparkleCount; ++i)
            {
                SpawnParticle(lineStart, lineEnd, dir, perp, speedMin, speedMax, lifeMin, lifeMax, isShard: false);
            }
            SetVerticesDirty();
        }

        // 다양한 유리 조각 모양(3~5각형, 일부는 길쭉하게 찌그러진 조각)을 1회 생성
        private void EnsureShapes()
        {
            if (_shapes != null)
            {
                return;
            }
            _shapes = new List<Vector2[]>();
            for (int t = 0; t < 12; ++t)
            {
                int n = Random.Range(3, 6); // 3~5개 꼭짓점
                float[] angs = new float[n];
                for (int i = 0; i < n; ++i)
                {
                    angs[i] = Random.value * Mathf.PI * 2f;
                }
                System.Array.Sort(angs); // 각도 정렬 -> 볼록다각형

                float sx = Random.Range(0.5f, 1.2f); // 비균등 스케일로 길쭉/납작한 조각도 섞이게
                float sy = Random.Range(0.35f, 1.2f);
                Vector2[] verts = new Vector2[n];
                for (int i = 0; i < n; ++i)
                {
                    float r = Random.Range(0.5f, 1f);
                    verts[i] = new Vector2(Mathf.Cos(angs[i]) * r * sx, Mathf.Sin(angs[i]) * r * sy);
                }
                _shapes.Add(verts);
            }
        }

        private void SpawnParticle(Vector2 lineStart, Vector2 lineEnd, Vector2 dir, Vector2 perp,
            float speedMin, float speedMax, float lifeMin, float lifeMax, bool isShard)
        {
            Vector2 originOnLine = Vector2.Lerp(lineStart, lineEnd, Random.value);

            // 절단면 양쪽으로 방사형으로 터진다 - 수직(perp) 기준 ±spread 각도로 퍼뜨려 유리가 튀어나가는 느낌
            float sideSign = (Random.value < 0.5f) ? -1f : 1f;
            float spreadDeg = Random.Range(-38f, 38f);
            Vector2 outDir = Rotate(perp * sideSign, spreadDeg);
            Vector2 vel = outDir * Random.Range(speedMin, speedMax);

            float life = Random.Range(lifeMin, lifeMax);
            _particles.Add(new Particle
            {
                Position           = originOnLine,
                Velocity           = vel,
                RotationDeg        = Random.Range(0f, 360f),
                AngularVelocityDeg = Random.Range(-260f, 260f),
                Size               = isShard ? Random.Range(14f, 42f) : Random.Range(3f, 7f),
                Life               = 1f,
                LifeSpeed          = 1f / Mathf.Max(0.05f, life),
                IsShard            = isShard,
                ShapeIndex         = isShard ? Random.Range(0, _shapes.Count) : 0,
            });
        }

        // 벡터를 도(deg) 단위로 회전
        private static Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(r);
            float s = Mathf.Sin(r);
            return new Vector2((v.x * c) - (v.y * s), (v.x * s) + (v.y * c));
        }

        private void Update()
        {
            if (_particles.Count == 0)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            const float gravity = -70f;  // px/s^2 - 아주 약하게(방사형으로 터진 뒤 살짝 가라앉는 정도)
            const float drag    = 2.4f;  // 공기저항 - 터진 파편이 점점 감속해 자연스럽게 멎는다
            for (int i = _particles.Count - 1; i >= 0; --i)
            {
                Particle p = _particles[i];
                p.Velocity -= p.Velocity * Mathf.Min(1f, drag * dt); // 감속
                p.Velocity.y += gravity * dt;
                p.Position += p.Velocity * dt;
                p.RotationDeg += p.AngularVelocityDeg * dt;
                p.Life -= p.LifeSpeed * dt;

                if (p.Life <= 0f)
                {
                    _particles.RemoveAt(i);
                }
                else
                {
                    _particles[i] = p;
                }
            }
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            for (int i = 0; i < _particles.Count; ++i)
            {
                if (_particles[i].IsShard)
                {
                    AddShard(vh, _particles[i]);
                }
                else
                {
                    AddSparkle(vh, _particles[i]);
                }
            }
        }

        // 유리 조각 - 볼록다각형 팬(중심=투명, 가장자리=밝음)으로 그려 유리처럼 가운데가 비치고 테두리가 빛난다
        private void AddShard(VertexHelper vh, Particle p)
        {
            if ((_shapes == null) || (p.ShapeIndex < 0) || (p.ShapeIndex >= _shapes.Count))
            {
                return;
            }
            Vector2[] shape = _shapes[p.ShapeIndex];
            float fade  = Mathf.Clamp01(p.Life);
            float scale = p.Size * Mathf.Clamp01(p.Life + 0.3f);
            float rad = p.RotationDeg * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);

            Color edge = _shardEdgeColor; edge.a *= fade;
            Color core = _shardCoreColor; core.a *= fade;

            int baseIndex = vh.currentVertCount;
            AddVert(vh, p.Position, core); // 중심(거의 투명 - 뒤가 비침)
            for (int i = 0; i < shape.Length; ++i)
            {
                Vector2 v = shape[i] * scale;
                Vector2 rotated = new Vector2((v.x * c) - (v.y * s), (v.x * s) + (v.y * c));
                AddVert(vh, p.Position + rotated, edge); // 가장자리(밝음)
            }

            int n = shape.Length;
            for (int i = 0; i < n; ++i)
            {
                int a = baseIndex + 1 + i;
                int b = baseIndex + 1 + ((i + 1) % n);
                vh.AddTriangle(baseIndex, a, b);
            }
        }

        // 스파클 - 작은 흰 사각형(유리가 빛을 튕기는 글린트)
        private void AddSparkle(VertexHelper vh, Particle p)
        {
            Color col = _sparkleColor; col.a *= Mathf.Clamp01(p.Life);
            float half = p.Size * 0.5f * Mathf.Clamp01(p.Life + 0.3f);

            float rad = p.RotationDeg * Mathf.Deg2Rad;
            Vector2 right = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * half;
            Vector2 up    = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad)) * half;

            int baseIndex = vh.currentVertCount;
            AddVert(vh, p.Position - right - up, col);
            AddVert(vh, p.Position + right - up, col);
            AddVert(vh, p.Position + right + up, col);
            AddVert(vh, p.Position - right + up, col);

            vh.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
            vh.AddTriangle(baseIndex, baseIndex + 2, baseIndex + 3);
        }

        private static void AddVert(VertexHelper vh, Vector2 pos, Color col)
        {
            UIVertex v = UIVertex.simpleVert;
            v.color    = col;
            v.position = pos;
            v.uv0      = Vector2.zero;
            vh.AddVert(v);
        }
    }
}
