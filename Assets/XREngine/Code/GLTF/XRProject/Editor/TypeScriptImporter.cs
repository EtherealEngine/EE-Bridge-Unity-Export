using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;
using XREngine.XRProject;
using System.IO;
using System.Text.RegularExpressions;

namespace XREngine.Editor
{
    [ScriptedImporter(1, new[] { ".ts" })]
    public class TypeScriptImporter : ScriptedImporter
    {
        const string iconPath = "Assets/XREngine/Content/Textures/xrproject/typescript-1174965.png";

        public override void OnImportAsset(AssetImportContext ctx)
        {
            TypeScriptAsset nuAsset = ScriptableObject.CreateInstance<TypeScriptAsset>();
            EditorGUIUtility.SetIconForObject(nuAsset, AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath));
            
            //nuAsset.text = File.ReadAllText(ctx.assetPath);
            ctx.AddObjectToAsset("script", nuAsset);
            ctx.SetMainObject(nuAsset);
        }
    }
}
