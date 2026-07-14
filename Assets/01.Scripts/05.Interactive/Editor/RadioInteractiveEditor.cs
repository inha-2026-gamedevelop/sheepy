// Unity
using UnityEditor;
using UnityEngine;

using Minsung.Sound;

namespace Minsung.Interactive
{
    // 클립 인덱스를 raw int 대신 실제 클립 이름 드롭다운으로 고를 수 있게 해준다.
    [CustomEditor(typeof(RadioInteractive))]
    public class RadioInteractiveEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.propertyPath == "_clipIndex")
                {
                    DrawClipIndexPopup();
                    continue;
                }

                using (new EditorGUI.DisabledScope(iterator.propertyPath == "m_Script"))
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        // 현재 지정된 _bgm 카테고리의 클립 목록을 드롭다운으로 보여주고, 선택 결과를 _clipIndex에 저장한다. 맨 앞 항목은 무작위(-1)
        private void DrawClipIndexPopup()
        {
            SerializedProperty clipIndexProp = serializedObject.FindProperty("_clipIndex");

            SoundData soundDB = FindSoundDB();
            if (soundDB == null)
            {
                EditorGUILayout.PropertyField(clipIndexProp);
                EditorGUILayout.HelpBox("SoundDB 에셋을 찾을 수 없습니다.", MessageType.Warning);
                return;
            }

            EBgm        bgm   = (EBgm)serializedObject.FindProperty("_bgm").enumValueIndex;
            AudioClip[] clips = GetClipsSilently(soundDB, bgm);

            if ((clips == null) || (clips.Length == 0))
            {
                EditorGUILayout.PropertyField(clipIndexProp);
                EditorGUILayout.HelpBox($"{bgm} 카테고리에 등록된 클립이 없습니다.", MessageType.Warning);
                return;
            }

            string[] options = new string[clips.Length + 1];
            options[0] = "(무작위)";
            for (int i = 0; i < clips.Length; ++i)
            {
                options[i + 1] = (clips[i] != null) ? clips[i].name : $"(비어 있음) [{i}]";
            }

            int currentSelection = Mathf.Clamp(clipIndexProp.intValue + 1, 0, options.Length - 1);
            int newSelection     = EditorGUILayout.Popup("Clip Index", currentSelection, options);
            clipIndexProp.intValue = newSelection - 1;
        }

        // SoundData.GetBgmClip은 범위를 벗어나면 경고 로그를 남기므로, 인스펙터 미리보기용으로 조용히 조회
        private static AudioClip[] GetClipsSilently(SoundData soundDB, EBgm bgm)
        {
            BgmData[] datas = soundDB.BgmDatas;
            int       index = (int)bgm;
            if ((datas == null) || (index < 0) || (index >= datas.Length))
            {
                return null;
            }

            return datas[index].Clips;
        }

        private static SoundData FindSoundDB()
        {
            string[] guids = AssetDatabase.FindAssets("t:SoundData");
            if (guids.Length == 0)
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<SoundData>(path);
        }
    }
}
