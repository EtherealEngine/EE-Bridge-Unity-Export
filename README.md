# Unity-to-XREngine Exporter
Built-in export pipeline for Unity to https://github.com/XRFoundation/XREngine
# Usage

## System Requirements
NodeJS >12.0

Unity >2020

Bakery Lightmapper (optional)

## Getting Started
Run the applicable 'init-pipeline' script from the root project directory.

Export any Unity scene as a GLB with menu item XREngine->Export Scene. This will bring up an export configuration window. 

### Export Parameters
**Name**: name of the GLTF and GLB files. Enter without file extension.

**Set Output Directory**: By default, the scene will be exported into the /Outputs/GLB/ folder in the project.

**Export Colliders**: Toggles whether collider data will be included in export. Currently only box and mesh colliders are supported.

**Export**: Begins an export. Note that if you have a gameobject selected in editor, then only the selection is exported.

### Supported Components
**Lights**: point and direction light are currently supported.

**Cameras**: 

**Lightmaps**: 

  ***BAKE_COMBINED***: lightmaps are automatically combined with the diffuse channel and reprojected onto the mesh's uv0, then exported as an unlit material. Note that this will cause issues with instanced geometry.

  ***BAKE_SEPARATE***: lightmaps are exported as-is and loaded into the lightmap in the standard mesh material. Mesh uv2s are adjusted to apply lightmap scale and offset. 

**Colliders**: box and mesh colliders are automatically configured and exported in XREngine compatible format.

**LODs**: LOD Groups in Unity are automatically configured and exported. 

**Spawn Points**: Spawn points are exported by adding the ***Spawn Point*** script onto transforms in the scene. 

**Instancing**: Any Gameobjects which share the same mesh and material will be instanced by default. Currently only meshes with one material are supported. As previously noted, baking lightmaps onto Gameobjects that share the same mesh and material will break instancing.

**Skybox**: If the scene has a skybox with a valid cubemap, then it is exported into the project. Currently only supports one cubemap per XRE project.

## Known Issues

### Materials Black After Error During Export
In general, exceptions thrown during the SeinJS export will result in all materials in the scene being black. Quickly fix this after it occurs by selecting menu item SeinJS->Restore Materials.

### No Default Materials Allowed
Every material in the scene must be a project asset that resides somewhere within the Assets folder. Materials from unity's default asset registry will cause the exporter to fail.

