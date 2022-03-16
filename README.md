# Unity-to-XREngine Exporter
Built-in export pipeline for Unity to https://github.com/XRFoundation/XREngine
# Usage

## System Requirements
NodeJS >12.0

Unity >2020

Bakery Lightmapper (optional)
Meshbaker (optional)

## Getting Started

Export any Unity scene as a GLB with menu item XREngine->Export Scene. This will bring up an export configuration window. 

### Export Parameters
**Name**: name of the GLTF and GLB files. Enter without file extension.

**Set Output Directory**: Set to your target XREngine project directory. By default, the scene will be exported into the /Outputs/GLB/ folder.

**Export Components**: Options for exporting specific component types

**Export**: Begins an export. Note that if you have a gameobject selected in editor, then only the selection is exported.

### Supported Components
**Lights**: point and direction light are currently supported.

**Cameras**: 

**Lightmaps**: Lightmaps are supported in two different export options:

  ***BAKE_COMBINED***: lightmaps are automatically combined with the diffuse channel and reprojected onto the mesh's uv0, then exported as an unlit material. Note that this will cause issues with instanced geometry.

  ***BAKE_SEPARATE***: lightmaps are exported as-is and loaded into the lightmap in the standard mesh material. Mesh uv2s are adjusted to apply lightmap scale and offset. 

**Colliders**: box and mesh colliders are automatically configured and exported in XREngine compatible format.

**LODs**: LOD Groups in Unity are automatically configured and exported. 

**Spawn Points**: Spawn points are exported by adding the ***Spawn Point*** script onto transforms in the scene. 

**Instancing**: Any Gameobjects which share the same mesh and material will be instanced by default. Currently only meshes with one material are supported. As previously noted, baking lightmaps onto Gameobjects that share the same mesh and material will break instancing.

**Skybox**: If the scene has a skybox with a valid cubemap, then it is exported into the project. Currently only supports one cubemap per XRE project.

### GLTF Optimization

This exporter uses a custom version of the gltfpack and meshoptimizer binary executable. Configure at your own risk -- engine support for all gltfpack optimization features is ongoing and currently error prone.

**Instanced Meshes**: gltfpack will attempt to instance all scene elements that share the same mesh and materials. Currently not working

**KTX2 Compression**: compresses textures using ktx2. This takes a long time but is extremely effective at reducing export file sizes

**Meshopt Compression**: compresses meshes using meshoptimizer algorithms. Currently mesh quantization is not supported

**Combine Materials**: attempts to merge all similar materials in the scene. Currently no observable changes occur on exported scenes from Unity

**Combine Nodes**: attempts to combine as many nodes as possible together to reduce draw calls. Currently causes meshes to break upon import to XRE editor

### Recommended Workflow

Split your scene into two parts: a "base" scene and a "colliders" scene. In the base scene, use this configuration:

![Unity_BH46tkMq3i](https://user-images.githubusercontent.com/94419856/157543849-b7620572-8828-4b95-ba1d-fc0973fb5b11.png)

(Instancing, Meshopt, KTX2 Compression enabled, colliders, lights disabled)

This exported file will contain all geometry, and can be optimized significantly.

In the colliders scene, use this configuration:

![Unity_8kUeAC3nFA](https://user-images.githubusercontent.com/94419856/157544345-dd1d6171-1ba9-4374-b65e-40cda6d24497.png)

(everything disabled except for colliders)

This will export the collider information in an XRE supported format. Meshopt compression breaks collider imports into XRE so leave this file unoptimized.

### Advanced Tools

**Serialize Into Persistent Assets**: This project uses GLTFast to import GLB and GLTF files into Unity. By default the imported assets are read-only, and do not have their own asset paths, which are required to correctly export using our exporter. Thus there is an intermediate step where all assets are duplicated and placed in either the "Assets/_XREngine_/PipelineAssets" folder or the "Assets/_XREngine_/PersistentAssets" folder. PipelineAssets is automatically cleared after each export operation, while PersistentAssets is manually maintained. It is highly recommended that, when using any advanced tools, this option is toggled on.

**Serialize Selected/All**: Performs the GLTF asset duplication upon either the current selected objects, or the entire loaded scene.

**Deserialize Selected/All**: Resets either selected or all objects in scene to original GLTF assets.

**Do MeshBake**: Performs Meshbaker operations as defined by ***Mesh Bake*** Monobehaviors placed in scene.

**Undo MeshBake**: All operations performed by the ***Mesh Bake*** monobehaviors are undone.

**Revert Backups**: Searches project for asset backup folders and performs a hard reset of all registered changes. Currently only materials are backed up into said folders.

## Known Issues

### Materials Black After Error During Export
In general, exceptions thrown during the SeinJS export will result in all materials in the scene being black. Quickly fix this after it occurs by selecting menu item SeinJS->Restore Materials.

### No Default Materials Allowed
Every material in the scene must be a project asset that resides somewhere within the Assets folder. Materials from unity's default asset registry will cause the exporter to fail.

