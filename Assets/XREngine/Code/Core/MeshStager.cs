using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeshStager : MeshCombiner
{
#if UNITY_EDITOR
	private static event Action Reset;
	public static void ResetAll()
    {
		Reset.Invoke();
    }
	MeshFilter filt;
	Renderer rend;
	List<GameObject> activeGOs;
	List<GameObject> rootGOs;
	List<GameObject> combineRoots;
	public void Execute(Func<Transform, bool> filterFn)
	{
		CreateMultiMaterialMesh = true;

		filt = GetComponent<MeshFilter>();
		rend = GetComponent<Renderer>();
		
		if (filterFn == null)
		{
			rootGOs = new List<GameObject>();
			activeGOs = gameObject.GetComponentsInChildren<Transform>(false)
				.Where((x) => x != transform && x.GetComponent<Renderer>())
				.Select((tr) => tr.gameObject).ToList();

			var combineGroups = activeGOs.Where((go) => go.GetComponent<Renderer>())
				.GroupBy((go) => go.GetComponent<Renderer>().lightmapIndex).ToArray();

			for (int i = 0; i < combineGroups.Length; i++)
			{
				var combineGroup = combineGroups[i];
				int thisLightmapIndex = combineGroup.Key;
				Func<Transform, bool> filter = (tr => tr.GetComponent<Renderer>().lightmapIndex == thisLightmapIndex);

				string meshName = gameObject.name + "_" + (i + 1);
				string thisName = gameObject.name;
				gameObject.name = meshName;

				Execute(filter);

				gameObject.name = thisName;

				GameObject nuGO = new GameObject(meshName, new Type[]
				{
					typeof(MeshFilter),
					typeof(MeshRenderer)
				});

				nuGO.isStatic = gameObject.isStatic;
				nuGO.transform.SetParent(transform.parent);
				nuGO.transform.SetPositionAndRotation(transform.position, transform.rotation);

				nuGO.GetComponent<MeshFilter>().sharedMesh = filt.sharedMesh;
				nuGO.GetComponent<MeshRenderer>().sharedMaterials = rend.sharedMaterials;

				filt.sharedMesh = null;
				rend.sharedMaterials = new Material[0];

				nuGO.GetComponent<MeshRenderer>().lightmapIndex = thisLightmapIndex;

				rootGOs.Add(nuGO);
			}

			Reset += DoReset;
		}
		else
		{
			var partition = gameObject.GetComponentsInChildren<Transform>(false)
				.Where((x) =>
					x != transform &&
					x.GetComponent<Renderer>() &&
					!filterFn(x))
				.Select((tr) => tr.gameObject);
			foreach(var toHide in partition)
            {
				toHide.SetActive(false);
            }

			CombineMeshes(true);
			SaveCombinedMesh(filt.sharedMesh, "XREngine/PipelineAssets/");

			foreach(var toShow in partition)
            {
				toShow.SetActive(true);
            }
		}
		
	}

    private void DoReset()
    {
		foreach(var go in activeGOs)
        {
			if (go == null) continue;
			go.SetActive(true);
        }
		foreach(var root in rootGOs)
        {
			if (root == null) continue;
			DestroyImmediate(root);
        }
		rootGOs = null;
		filt.sharedMesh = null;
		rend.sharedMaterials = new Material[0];
		rend.lightmapIndex = -1;
		Reset -= DoReset;
    }

    public string SaveCombinedMesh(Mesh mesh, string folderPath)
	{
		bool meshIsSaved = AssetDatabase.Contains(mesh); // If is saved then only show it in the project view.

		#region Create directories if Mesh and path doesn't exists:
		folderPath = folderPath.Replace('\\', '/');
		if (!meshIsSaved && !AssetDatabase.IsValidFolder("Assets/" + folderPath))
		{
			string[] folderNames = folderPath.Split('/');
			folderNames = folderNames.Where((folderName) => !folderName.Equals("")).ToArray();
			folderNames = folderNames.Where((folderName) => !folderName.Equals(" ")).ToArray();

			folderPath = "/"; // Reset folder path.
			for (int i = 0; i < folderNames.Length; i++)
			{
				folderNames[i] = folderNames[i].Trim();
				if (!AssetDatabase.IsValidFolder("Assets" + folderPath + folderNames[i]))
				{
					string folderPathWithoutSlash = folderPath.Substring(0, folderPath.Length - 1); // Delete last "/" character.
					AssetDatabase.CreateFolder("Assets" + folderPathWithoutSlash, folderNames[i]);
				}
				folderPath += folderNames[i] + "/";
			}
			folderPath = folderPath.Substring(1, folderPath.Length - 2); // Delete first and last "/" character.
		}
		#endregion Create directories if Mesh and path doesn't exists.

		#region Save Mesh:
		if (!meshIsSaved)
		{
			string meshPath = "Assets/" + folderPath + "/" + mesh.name + ".asset";
			int assetNumber = 1;
			while (AssetDatabase.LoadAssetAtPath(meshPath, typeof(Mesh)) != null) // If Mesh with same name exists, change name.
			{
				meshPath = "Assets/" + folderPath + "/" + mesh.name + " (" + assetNumber + ").asset";
				assetNumber++;
			}

			AssetDatabase.CreateAsset(mesh, meshPath);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Debug.Log("<color=#ff9900><b>Mesh \"" + mesh.name + "\" was saved in the \"" + folderPath + "\" folder.</b></color>"); // Show info about saved mesh.
		}
		#endregion Save Mesh.
		
		//EditorGUIUtility.PingObject(mesh); // Show Mesh in the project view.
		return folderPath;
	}
#endif
}
