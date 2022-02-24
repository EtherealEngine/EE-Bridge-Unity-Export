using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace XREngine.XRProject
{
    public class SkyBox : RPComponent
    {
        public enum Mode
        {
            CUBEMAP,
            EQUIRECTANGULAR
        }

        JProperty MapPath => new JProperty(Type + (mode == Mode.CUBEMAP ? ".cubemap" : ".equirectangular") + "Path", PipelineSettings.XRELocalPath + "/cubemap/" + (mode == Mode.CUBEMAP ? "" : "rect.jpg"));

        public Mode mode;

        public override string Type => base.Type + ".skybox";

        public override JProperty Serialized => new JProperty("extras", new JObject(
            new JProperty(Type + ".backgroundType", (int)mode + 1),
            MapPath,
            new JProperty("xrproject.entity", transform.name)
        ));
    }

}
