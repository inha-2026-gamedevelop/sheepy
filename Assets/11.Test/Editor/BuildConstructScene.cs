// Construct 3 (Sheepy) 레이아웃 -> Unity 씬 빌더
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ConstructImport
{
    [System.Serializable]
    public class SpriteDef
    {
        public string name;
        public float pivotX = 0.5f;
        public float pivotY = 0.5f;
    }

    [System.Serializable]
    public class Inst
    {
        public string obj;
        public int layer;
        public string layerName;
        public float x, y, w, h;
        public float angleRad;
        public float r = 1, g = 1, b = 1, a = 1;
        public float ox = 0.5f, oy = 0.5f;
        public string sprite;
        public float nativeW, nativeH;
    }

    [System.Serializable]
    public class Manifest
    {
        public string layout;
        public int layoutWidth, layoutHeight;
        public int pixelsPerUnit = 100;
        public SpriteDef[] sprites;
        public Inst[] instances;
    }

    public static class BuildConstructScene
    {
        [MenuItem("Tools/Construct Import/Build Scene From Manifest...")]
        public static void BuildFromManifest()
        {
            string abs = EditorUtility.OpenFilePanel("Construct 매니페스트(JSON) 선택", Application.dataPath, "json");
            if (string.IsNullOrEmpty(abs))
            {
                return;
            }

            string assetPath = ToAssetPath(abs);
            if (assetPath == null)
            {
                EditorUtility.DisplayDialog("오류", "매니페스트는 프로젝트 Assets 폴더 안에 있어야 합니다.", "확인");
                return;
            }

            string json = File.ReadAllText(abs);
            Manifest m = JsonUtility.FromJson<Manifest>(json);
            if (m == null || m.instances == null)
            {
                EditorUtility.DisplayDialog("오류", "매니페스트 파싱 실패.", "확인");
                return;
            }

            string manifestDirAbs = Path.GetDirectoryName(abs);
            string spritesDirAbs = Path.Combine(manifestDirAbs, "sprites");
            string spritesDirAsset = ToAssetPath(spritesDirAbs);

            // 1 스프라이트 임포트 설정
            ConfigureSprites(m, spritesDirAsset);

            // 2 씬 구성
            BuildHierarchy(m, spritesDirAsset);

            EditorUtility.DisplayDialog("완료",
                string.Format("레이아웃 '{0}' 구성 완료\n인스턴스 {1}개 / 스프라이트 {2}개",
                    m.layout, m.instances.Length, m.sprites != null ? m.sprites.Length : 0), "확인");
        }

        static void ConfigureSprites(Manifest m, string spritesDirAsset)
        {
            if (m.sprites == null || spritesDirAsset == null) { return; }
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var sd in m.sprites)
                {
                    string p = spritesDirAsset + "/" + sd.name;
                    var ti = AssetImporter.GetAtPath(p) as TextureImporter;
                    if (ti == null) { continue; }
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.spritePixelsPerUnit = m.pixelsPerUnit;
                    ti.spritePivot = new Vector2(0.5f, 0.5f); // 위치계산에서 origin 보정하므로 Center 통일
                    ti.mipmapEnabled = false;
                    ti.alphaIsTransparency = true;
                    ti.wrapMode = TextureWrapMode.Clamp;
                    ti.filterMode = FilterMode.Bilinear;
                    ti.textureCompression = TextureImporterCompression.Uncompressed;
                    var settings = new TextureImporterSettings();
                    ti.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)SpriteAlignment.Center;
                    ti.SetTextureSettings(settings);
                    EditorUtility.SetDirty(ti);
                    ti.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        static void BuildHierarchy(Manifest m, string spritesDirAsset)
        {
            float ppu = m.pixelsPerUnit <= 0 ? 100f : m.pixelsPerUnit;

            var root = new GameObject(m.layout);
            Undo.RegisterCreatedObjectUndo(root, "Build Construct Scene");

            // 레이어별 그룹 캐시
            var layerGroups = new System.Collections.Generic.Dictionary<int, Transform>();

            for (int i = 0; i < m.instances.Length; ++i)
            {
                Inst it = m.instances[i];

                Transform parent;
                if (!layerGroups.TryGetValue(it.layer, out parent))
                {
                    var g = new GameObject(string.Format("{0:00}_{1}", it.layer, it.layerName));
                    g.transform.SetParent(root.transform, false);
                    parent = g.transform;
                    layerGroups[it.layer] = parent;
                }

                var go = new GameObject(it.obj + "_" + i);
                go.transform.SetParent(parent, false);

                // Construct origin(좌상단,Y하향) -> 인스턴스 중심(Construct 좌표)
                float cx = it.x + (0.5f - it.ox) * it.w;
                float cy = it.y + (0.5f - it.oy) * it.h;
                go.transform.position = new Vector3(cx / ppu, -cy / ppu, 0f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, -it.angleRad * Mathf.Rad2Deg);

                if (!string.IsNullOrEmpty(it.sprite))
                {
                    var sr = go.AddComponent<SpriteRenderer>();
                    if (spritesDirAsset != null)
                    {
                        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(spritesDirAsset + "/" + it.sprite);
                        sr.sprite = sp;
                    }
                    sr.color = new Color(it.r, it.g, it.b, it.a);
                    sr.sortingOrder = i; // 배열 순서 = 정확한 페인트 순서

                    // 인스턴스 크기 / 원본 프레임 크기 = 스케일
                    float sx = it.nativeW > 0 ? it.w / it.nativeW : 1f;
                    float sy = it.nativeH > 0 ? it.h / it.nativeH : 1f;
                    go.transform.localScale = new Vector3(sx, sy, 1f);
                }
            }

            Selection.activeGameObject = root;
        }

        static string ToAssetPath(string absolute)
        {
            absolute = absolute.Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            if (!absolute.StartsWith(dataPath)) { return null; }
            return "Assets" + absolute.Substring(dataPath.Length);
        }
    }
}
#endif
