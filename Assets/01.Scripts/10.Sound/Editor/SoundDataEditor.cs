// System
using System;

// Unity
using UnityEditor;
using UnityEngine;

namespace Minsung.Sound
{
    // Bgm Datas 배열 각 칸에 "Element 0" 대신 EBgm 이름을 라벨로 보여준다.
    [CustomEditor(typeof(SoundData))]
    public class SoundDataEditor : Editor
    {
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
                string label = (i < names.Length) ? names[i] : $"Element {i}";
                EditorGUILayout.PropertyField(bgmDatas.GetArrayElementAtIndex(i), new GUIContent(label), true);
            }
            --EditorGUI.indentLevel;
        }
    }
}
