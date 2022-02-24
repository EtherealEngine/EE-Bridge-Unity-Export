var tool = require('gltf-import-export')
var path = require('path')

args = process.argv.slice(2)
name = args[0]
outPath = args[1]
const inputGLTF = "../Outputs/GLTF/" + name + ".gltf";
const newFile = outPath + name + ".glb";

tool.ConvertGltfToGLB(inputGLTF, newFile)
