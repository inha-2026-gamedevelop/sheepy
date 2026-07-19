// System
using System;

// Unity
using UnityEditor;
using UnityEngine;

namespace Minsung.Sound
{
    // Bgm/Sfx Datas 배열 각 칸에 "Element 0" 대신 "0: Jump"처럼 인덱스 + enum 이름을 라벨로 보여준다.
    [CustomEditor(typeof(SoundData))]
    public class SoundDataEditor : Editor
    {
        // _sfxDatas의 배열 순서(ESfxState)별로 그 안의 _clips가 어떤 enum 인덱스와 대응하는지 매핑
        private static readonly Type[] SfxClipEnumTypes =
        {
            typeof(EPlayerSfx),  // ESfxState.Player
            typeof(ETimeSfx),    // ESfxState.Time
            typeof(EMonsterSfx), // ESfxState.Monster
            typeof(EBossSfx),    // ESfxState.Boss
            typeof(EObjectSfx),  // ESfxState.Object
            typeof(EUISfx),      // ESfxState.UI
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.propertyPath == "_bgmDatas")
                {
                    DrawBgmDatas(iterator);
                    continue;
                }
                if (iterator.propertyPath == "_sfxDatas")
                {
                    DrawSfxDatas(iterator);
                    continue;
                }

                using (new EditorGUI.DisabledScope(iterator.propertyPath == "m_Script"))
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        // EBgm 이름으로 라벨링해서 그린다. 배열 크기가 enum 항목 수와 다르면 맞추는 버튼을 보여준다
        private static void DrawBgmDatas(SerializedProperty bgmDatas)
        {
            string[] names = Enum.GetNames(typeof(EBgm));

            EditorGUILayout.LabelField("Bgm Datas", EditorStyles.boldLabel);

            if (bgmDatas.arraySize != names.Length)
            {
                EditorGUILayout.HelpBox(
                    $"Bgm Datas 크기({bgmDatas.arraySize})가 EBgm 항목 수({names.Length})와 다릅니다.",
                    MessageType.Warning);

                if (GUILayout.Button("EBgm 개수에 맞춰 크기 조정"))
                {
                    bgmDatas.arraySize = names.Length;
                }
            }

            ++EditorGUI.indentLevel;
            for (int i = 0; i < bgmDatas.arraySize; ++i)
            {
                string label = (i < names.Length) ? $"{i}: {names[i]}" : $"Element {i}";
                EditorGUILayout.PropertyField(bgmDatas.GetArrayElementAtIndex(i), new GUIContent(label), true);
            }
            --EditorGUI.indentLevel;
        }

        // ESfxState 카테고리 이름으로 바깥 배열을, 카테고리별 클립 enum(EPlayerSfx 등) 이름으로 안쪽 _clips 배열을 라벨링한다
        private static void DrawSfxDatas(SerializedProperty sfxDatas)
        {
            string[] categoryNames = Enum.GetNames(typeof(ESfxState));

            EditorGUILayout.LabelField("Sfx Datas", EditorStyles.boldLabel);

            if (sfxDatas.arraySize != SfxClipEnumTypes.Length)
            {
                EditorGUILayout.HelpBox(
                    $"Sfx Datas 크기({sfxDatas.arraySize})가 카테고리 수({SfxClipEnumTypes.Length})와 다릅니다.",
                    MessageType.Warning);

                if (GUILayout.Button("카테고리 수에 맞춰 크기 조정"))
                {
                    sfxDatas.arraySize = SfxClipEnumTypes.Length;
                }
            }

            ++EditorGUI.indentLevel;
            for (int i = 0; i < sfxDatas.arraySize; ++i)
            {
                string categoryLabel = (i < categoryNames.Length) ? $"{i}: {categoryNames[i]}" : $"Element {i}";
                EditorGUILayout.LabelField(categoryLabel, EditorStyles.boldLabel);

                ++EditorGUI.indentLevel;
                DrawSfxData(sfxDatas.GetArrayElementAtIndex(i), (i < SfxClipEnumTypes.Length) ? SfxClipEnumTypes[i] : null);
                --EditorGUI.indentLevel;

                EditorGUILayout.Space();
            }
            --EditorGUI.indentLevel;
        }

        // SfxData 한 칸(_sfxName + _clips)을 그린다. clipEnumType이 있으면 그 enum 이름으로 _clips 각 원소를 라벨링한다
        private static void DrawSfxData(SerializedProperty sfxData, Type clipEnumType)
        {
            SerializedProperty sfxNameProp = sfxData.FindPropertyRelative("_sfxName");
            SerializedProperty clipsProp   = sfxData.FindPropertyRelative("_clips");

            if (sfxNameProp != null)
            {
                EditorGUILayout.PropertyField(sfxNameProp);
            }
            if (clipsProp == null)
            {
                return;
            }

            string[] clipNames = (clipEnumType != null) ? Enum.GetNames(clipEnumType) : null;

            if ((clipNames != null) && (clipsProp.arraySize != clipNames.Length))
            {
                EditorGUILayout.HelpBox(
                    $"Clips 크기({clipsProp.arraySize})가 {clipEnumType.Name} 항목 수({clipNames.Length})와 다릅니다.",
                    MessageType.Warning);

                if (GUILayout.Button($"{clipEnumType.Name} 개수에 맞춰 크기 조정"))
                {
                    clipsProp.arraySize = clipNames.Length;
                }
            }

            EditorGUILayout.PropertyField(clipsProp, new GUIContent("Clips"), false);
            if (!clipsProp.isExpanded)
            {
                return;
            }

            ++EditorGUI.indentLevel;
            SerializedProperty sizeProp = clipsProp.FindPropertyRelative("Array.size");
            if (sizeProp != null)
            {
                EditorGUILayout.PropertyField(sizeProp);
            }
            for (int j = 0; j < clipsProp.arraySize; ++j)
            {
                string clipLabel = ((clipNames != null) && (j < clipNames.Length)) ? $"{j}: {clipNames[j]}" : $"Element {j}";
                EditorGUILayout.PropertyField(clipsProp.GetArrayElementAtIndex(j), new GUIContent(clipLabel));
            }
            --EditorGUI.indentLevel;
        }
    }
}
