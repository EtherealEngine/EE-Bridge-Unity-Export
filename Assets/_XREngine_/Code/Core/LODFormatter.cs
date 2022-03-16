using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;
using XREngine.XREngineProject;

namespace XREngine
{
    public class LODFormatter
    {
        public static void FormatLODs()
        {
            var lodGroups = GameObject.FindObjectsOfType<Transform>().Where((tr) => tr.gameObject.activeInHierarchy && 
                                                                                    Regex.IsMatch(tr.name, ".*_LODGroup"));
            foreach (var group in lodGroups)
            {
                LODGroup thisLOD = group.gameObject.GetComponent<LODGroup>();
                if(thisLOD == null)
                    thisLOD = group.gameObject.AddComponent<LODGroup>();

                var children = group.GetComponentsInChildren<Transform>().Where((tr) => tr != group && Regex.IsMatch(tr.name, @"(?<=_LOD)\d+"));
                int nChildren = children.Count();
                thisLOD.SetLODs(children.Select((tr, i) => new LOD((nChildren - i - 1) * 1f / nChildren, tr.GetComponentsInChildren<MeshRenderer>())).ToArray());
            }
        }

        public static void ConvertToInstancing()
        {
            var lodGroups = GameObject.FindObjectsOfType<LODGroup>().Where((lg) => lg.gameObject.activeInHierarchy &&
                                                                                   lg.enabled);
            foreach(var lg in lodGroups)
            {
                var lods = lg.GetLODs();
                var lod0 = lods[0];
                lod0.renderers.ToList().ForEach((rend) =>
                {
                    var go = rend.gameObject;
                    go.AddComponent<InstancedMesh>();
                });
                for(int i = 1; i < lods.Length; i++)
                {
                    var lodi = lods[i];
                    lodi.renderers.ToList().ForEach((rend) => rend.enabled = false);
                }
                lg.enabled = false;
            }
        }
    }

}
