using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace XREngine.XREngineProject
{
    public class XRESystem : RPComponent
    {
        public enum InjectionPoint
        {
            NONE,
            UPDATE,
            FIXED_EARLY,
            FIXED,
            FIXED_LATE,
            PRE_RENDER,
            POST_RENDER
        }

        public TypeScriptAsset script;

        public InjectionPoint injectionPoint;

        public bool client;

        public bool server;

        public string args;

        public string XRESrcPath => "__$project$__/" + PipelineSettings.XREProjectName + "/assets/scripts/" + script.scriptName;

        public override string Type => base.Type + ".system";

        public override JProperty Serialized => new JProperty("extras", new JObject(
            new JProperty(Type + ".systemUpdateType", injectionPoint.ToString()),
            new JProperty(Type + ".filePath", XRESrcPath),
            new JProperty(Type + ".enableClient", client),
            new JProperty(Type + ".enableServer", server),
            new JProperty(Type + ".args", args),
            new JProperty("xrengine.entity", transform.name)
        ));

        public override void HandleExport()
        {
            base.HandleExport();
            Debug.Log("exporting system node with properties: \n" + Serialized.ToString(Newtonsoft.Json.Formatting.Indented));
            try
            {
                if (!Directory.Exists(PipelineSettings.XREScriptsFolder))
                    Directory.CreateDirectory(PipelineSettings.XREScriptsFolder);
                string finalPath = PipelineSettings.XREScriptsFolder + script.scriptName;
                File.WriteAllText(finalPath, File.ReadAllText(script.source));
            }catch(System.Exception e)
            {
                Debug.LogError("failed to export system node script.\nerror: " + e);
            }
        }
    }

}
