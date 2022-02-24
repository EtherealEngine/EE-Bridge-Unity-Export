using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace XREngine.Utilities
{
    public class GLTFUtilities
    {
        private static ComputeShader _fastMapper;
        static ComputeShader FastMapper
        {
            get
            {
                if(_fastMapper == null)
                {
                    _fastMapper = AssetDatabase.LoadAssetAtPath<ComputeShader>(@"Assets/XREngine/Code/Shaders/FastMap.compute");
                }
                return _fastMapper;
            }
        }
        private const string _whiteTexPath = @"Assets/XREngine/Code/Utilities/WhiteTex.png";
        private static Texture2D _whiteTex;
        static Texture2D WhiteTex
        {
            get
            {
                if(_whiteTex == null)
                {
                    _whiteTex = AssetDatabase.LoadAssetAtPath<Texture2D>(_whiteTexPath);
                    if(_whiteTex == null)
                    {
                        _whiteTex = new Texture2D(512, 512, TextureFormat.ARGB32, false);
                        _whiteTex.SetPixels(Enumerable.Repeat(Color.white, _whiteTex.width * _whiteTex.height).ToArray());
                        _whiteTex.Apply();

                        File.WriteAllBytes(_whiteTexPath, _whiteTex.EncodeToPNG());
                        AssetDatabase.ImportAsset(_whiteTexPath);
                        AssetDatabase.Refresh();
                    }
                }
                return _whiteTex;
            }
        }
        private const string _blackTexPath = @"Assets/XREngine/Code/Utilities/BlackTex.png";
        private static Texture2D _blackTex;
        static Texture2D BlackTex
        {
            get
            {
                if (_blackTex == null)
                {
                    _blackTex = AssetDatabase.LoadAssetAtPath<Texture2D>(_blackTexPath);
                    if (_blackTex == null)
                    {
                        _blackTex = new Texture2D(512, 512, TextureFormat.ARGB32, false);
                        _blackTex.SetPixels(Enumerable.Repeat(Color.black, _blackTex.width * _blackTex.height).ToArray());
                        _blackTex.Apply();

                        File.WriteAllBytes(_blackTexPath, _blackTex.EncodeToPNG());
                        AssetDatabase.ImportAsset(_blackTexPath);
                        AssetDatabase.Refresh();
                    }
                }
                return _blackTex;
            }
        }
        public static Texture2D BlitPropertyIntoMap(Texture inputMap, Color inputProp)
        {
            SetTextureImporterFormat(inputMap, true);
            RenderTexture _rt = RenderTexture.active;
            RenderTexture targetMap = new RenderTexture(inputMap.width, inputMap.height, inputMap.mipmapCount, inputMap.graphicsFormat);
            targetMap.enableRandomWrite = true;

            RenderTexture srcMap = new RenderTexture(inputMap.width, inputMap.height, inputMap.mipmapCount, inputMap.graphicsFormat);
            srcMap.enableRandomWrite = true;

            Graphics.Blit(inputMap, srcMap);

            
            

            int kernel = FastMapper.FindKernel("FastMap");

            FastMapper.SetInt("width", inputMap.width);
            FastMapper.SetInt("height", inputMap.height);

            FastMapper.SetTexture(kernel, "InputMap", srcMap);
            FastMapper.SetVector("InputProp", inputProp);

            FastMapper.SetTexture(kernel, "Result", targetMap);

            int threadGCount = inputMap.width * inputMap.height / 128 + 1;

            FastMapper.Dispatch(kernel, threadGCount, 1, 1);

            Texture2D result = new Texture2D(inputMap.width, inputMap.height, inputMap.graphicsFormat, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
            RenderTexture.active = targetMap;

            result.ReadPixels(new Rect(0, 0, result.width, result.height), 0, 0);
            result.Apply();
            RenderTexture.active = _rt;
            targetMap.Release();
            srcMap.Release();

            return result;
        }
        public static void BlitPropertiesIntoMaps(Material _mat)
        {
            
            //apply _Color to _MainTex then set _Color to white
            Color col = _mat.GetColor("_Color");
            Texture mainTex = _mat.GetTexture("_MainTex");
            if (mainTex == null)
                mainTex = WhiteTex;

            Texture2D nuMainTex = BlitPropertyIntoMap(mainTex, col);
            nuMainTex.name = _mat.name + "_diffuse";

            nuMainTex = GenerateAsset(nuMainTex, true);

            _mat.SetTexture("_MainTex", nuMainTex);
            _mat.SetColor("_Color", Color.white);


            //apply _Metallic and _Roughness to _MetallicGlossMap then set both to 0.5
            float metallic = _mat.GetFloat("_Metallic");
            float roughness = _mat.GetFloat("_Roughness");

            Color metalRoughness = new Color(metallic, roughness, 1, 1);
            Texture mRMap = _mat.GetTexture("_MetallicGlossMap");
            if (mRMap == null)
                mRMap = WhiteTex;

            Texture2D nuMRMap = BlitPropertyIntoMap(mRMap, metalRoughness);
            nuMRMap.name = _mat.name + "_roughmetallic";
            nuMRMap = GenerateAsset(nuMRMap, true);

            _mat.SetTexture("_MetallicGlossMap", nuMRMap);
            _mat.SetFloat("_Metallic", 0.5f);
            _mat.SetFloat("_Roughness", 0.5f);

            //apply _EmissionColor to _EmissionMap then set _EmissionColor to white
            Color emissionCol = _mat.GetColor("_EmissionColor");
            Texture emissionMap = _mat.GetTexture("_EmissionMap");
            if (emissionMap == null)
                emissionMap = WhiteTex;
           
            Texture2D nuEmission = BlitPropertyIntoMap(emissionMap, emissionCol);
            nuEmission.name = _mat.name + "_emission";
            
            
            nuEmission = GenerateAsset(nuEmission, true);

            _mat.SetTexture("_EmissionMap", nuEmission);
            _mat.SetColor("_EmissionColor", Color.white);

            AssetDatabase.SaveAssetIfDirty(_mat);

        }



        public static Texture2D GenerateAsset(Texture2D tex, bool savePersistent)
        {
            SetTextureImporterFormat(tex, true);
            Texture2D nuTex = new Texture2D(tex.width, tex.height, TextureFormat.ARGB32, tex.mipmapCount, true);
            nuTex.name = tex.name + "_" + System.DateTime.Now.Ticks;

            RenderTexture nuTexR = new RenderTexture(tex.width, tex.height, tex.mipmapCount, RenderTextureFormat.ARGB32);
            nuTexR.enableRandomWrite = true;
            RenderTexture _rt = RenderTexture.active;

            Graphics.Blit(tex, nuTexR);

            nuTex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            nuTex.Apply();
            RenderTexture.active = _rt;
            nuTexR.Release();
            string pRoot = savePersistent ? PipelineSettings.PipelinePersistentFolder : PipelineSettings.PipelineAssetsFolder;
            if (!Directory.Exists(pRoot))
            {
                Directory.CreateDirectory(pRoot);
            }
            
            string nuPath = pRoot.Replace(Application.dataPath, "Assets") + nuTex.name + ".png";
            File.WriteAllBytes(nuPath, nuTex.EncodeToPNG());
            AssetDatabase.Refresh();
            nuTex = AssetDatabase.LoadAssetAtPath<Texture2D>(nuPath);
            SetTextureImporterFormat(nuTex, true);
            
            /*
            AssetDatabase.CreateAsset(nuTex, pRoot.Replace(Application.dataPath, "Assets") + nuTex.name + ".asset");
            AssetDatabase.Refresh();
            */
            UnityEngine.Debug.Log("Generated texture " + nuTex + " from " + tex);
            return nuTex;
        }

        public static void SetTextureImporterFormat(Texture texture, bool isReadable)
        {
            if (null == texture) return;

            string assetPath = AssetDatabase.GetAssetPath(texture);
            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter != null)
            {
                tImporter.textureType = TextureImporterType.Default;

                tImporter.isReadable = isReadable;
                
                tImporter.textureCompression = TextureImporterCompression.Uncompressed;
                tImporter.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    format = TextureImporterFormat.RGBA32,
                    textureCompression = TextureImporterCompression.Uncompressed,
                    overridden = true
                });
                AssetDatabase.ImportAsset(assetPath);
            }
        }
    }

}
