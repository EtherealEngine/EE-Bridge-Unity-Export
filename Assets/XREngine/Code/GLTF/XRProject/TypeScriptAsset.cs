using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace XREngine.XRProject
{
    [Serializable]
    public class TypeScriptAsset : ScriptableObject
    {
#if UNITY_EDITOR
        public string scriptName => Regex.Match(source, @"[\w-_]+\.\w+$").Value;
        public string source => UnityEditor.AssetDatabase.GetAssetPath(this);
#endif
    }

}
