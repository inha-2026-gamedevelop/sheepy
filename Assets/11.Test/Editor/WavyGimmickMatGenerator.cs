using UnityEditor;
using UnityEngine;
using System.IO;

public static class WavyGimmickMatGenerator
{
    [InitializeOnLoadMethod]
    public static void GenerateMaterial()
    {
        string matPath = "Assets/Resources/WavyGimmickMat.mat";
        
        // 매테리얼이 이미 존재하면 다시 만들지 않음
        if (AssetDatabase.LoadAssetAtPath<Material>(matPath) != null)
        {
            return;
        }

        Shader shader = Shader.Find("Custom/WavyGimmick");
        if (shader == null)
        {
            Debug.LogError("Custom/WavyGimmick 쉐이더를 찾을 수 없습니다. 컴파일을 기다려주세요.");
            return;
        }

        // Resources 폴더가 없으면 생성
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        Material mat = new Material(shader);
        AssetDatabase.CreateAsset(mat, matPath);
        AssetDatabase.SaveAssets();
        
        Debug.Log("WavyGimmickMat 매테리얼이 Assets/Resources 경로에 자동 생성되었습니다.");
    }
}
