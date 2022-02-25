using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace XREngine.XREngineProject
{
    public class Envmap : RPComponent
    {
        public enum SourceType
        {
            DEFAULT,
            TEXTURE,
            COLOR
        }

        public enum TextureType
        {
            CUBEMAP,
            EQUIRECTANGULAR
        }

        public override string Type => base.Type + ".envmap";

        public override JProperty Serialized => new JProperty("extras", new JObject(
            new JProperty(Type + ".type", SourceType.TEXTURE),
            new JProperty(Type + ".envMapIntensity", RenderSettings.ambientIntensity),
            new JProperty(Type + ".envMapSourceURL", PipelineSettings.XRELocalPath + "/cubemap/"),
            new JProperty(Type + ".envMapTextureType", TextureType.CUBEMAP),

            new JProperty("xrengine.entity", transform.name)
        ));
    }

}
