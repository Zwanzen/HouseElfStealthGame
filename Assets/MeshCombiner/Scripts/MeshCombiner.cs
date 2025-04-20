using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Unity.Collections;
using UnityEditor;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MeshCombiner_Rookie0ne
{
    public class MeshCombiner : MonoBehaviour
    {
        #region Parameters

        [Tooltip("Only for when when you get the error due to the amount of vertices.\n Otherwise leave to false")]
        [SerializeField] private bool useUint32 = false;
        
        [TextArea(2, 2)] [ReadOnly] [SerializeField]
        private string Notes1 = "I would not suggest using this script at runtime as it runs multiple calculations to achieve the final result.";
        
        [Header("What to do with previous mesh holders")]

        [Tooltip("If true then all the objects that are set to be combined will be disabled in the hierarchy")]
        [SerializeField]
        private bool disableCombinedObjs = true;

        [Tooltip("If true then all the objects that are set to be combined will be destroyed")] [SerializeField]
        private bool destroyCombinedObjs = false;

        [Header("Instanced Objects based on different materials")]
        [Tooltip("The name that the final created object holding the combined meshes, mesh filter and mesh renderer")]
        [SerializeField] private string instancedObjectName = "InstancedObject";
        [Tooltip("The name of the created mesh(s)")]
        [SerializeField] private string finalCombinedMeshName = "CombinedMesh";
        
        [Header("??? Go to Final Object ???")]
        [Tooltip("If true, final object will be created")]
        [SerializeField] bool createFinalObject = true;
        [Tooltip("If true, the instanced objects based on different materials will be destroyed")]
        [SerializeField] private bool destroyPreviousCreationsDuringFinalMerge = true;
        [SerializeField] private bool addMeshColliderToTheFinalObj = true;

        [Header("Final Event")]
        [Tooltip("An event that triggers when meshes are combined.")]
        public UnityEvent meshesCombinedEvent = new UnityEvent();

        //The reason these variables exist is because in some scenario's they might be needed to be accessed.
        //That is why the meshesCombinedEvent exists.
        [HideInInspector] public Mesh newMesh;
        [HideInInspector] public Material[] sharedMaterials;
    
        #endregion
        
        protected void RobustCombineMeshes(List<GameObject> objectsToCombine)
        {
            //|-----------------------------------------------------------------||\\
            //To combine the meshes into the final new mesh we need to follow the following process.
            //First we have to combine the meshes that have the same materials to one another
            //And then we have to combine the created meshes into a final mesh final result to one single mesh
            //|-----------------------------------------------------------------||\\

            #region Part0 - Create a new list of decombined single mesh&material objects out of the original list of objects that are to be combined. Then use the new list as the combine list

            List<GameObject> newObjectsToCombineList = new List<GameObject>();
            foreach (var objectToCombine in objectsToCombine)
            {
                newObjectsToCombineList.AddRange(MeshDeCombine(objectToCombine));
            }

            #endregion

            #region Part1 - Create Separate GameObject Lists foreach different material (objects with multiple materials will belong to only one list)
            
            //find the mesh filters and mesh renderers that need to be combined
            List<MeshFilter> meshFilters = GetMeshFilters(newObjectsToCombineList);
            //find the materials needed
            List<Material> _sharedMaterials = FindTheMaterialsNeeded(meshFilters);
            //Create Separate GameObject Lists foreach different material
            //(objects with multiple materials will belong to only one list)
            List<ObjListAndMaterial> objListAndMaterials = new List<ObjListAndMaterial>();

            foreach (var _sharedMaterial in _sharedMaterials)
            {
                List<ObjListWithMaterialIndex> objListThatUseThatMaterialWithTheMaterialIndex = FindObjectsThatUseThatMaterial(newObjectsToCombineList, _sharedMaterial, objListAndMaterials);
                
                ObjListAndMaterial newObjListAndMaterial = new ObjListAndMaterial
                {
                    objectsToCombine = objListThatUseThatMaterialWithTheMaterialIndex,
                    material = _sharedMaterial
                };
                
                objListAndMaterials.Add(newObjListAndMaterial);
            }
            #endregion

            #region Part2 - Mesh Combine each of the GameObject Lists that were created in Part1
            
            List<GameObject> instancedObjects = new List<GameObject>();

            //apply mesh combination foreach of those lists
            //When same material -> merge sub meshes
            foreach (var objList in objListAndMaterials)
            {
                List<GameObject> _objsToCombine = new List<GameObject>();
                foreach (var oTC in objList.objectsToCombine)
                {
                    _objsToCombine.Add(oTC.objectToCombine);
                }

                GameObject instancedObj = CombineMeshes(_objsToCombine, true);
                instancedObjects.Add(instancedObj);
            }

            #endregion

            #region Part3 - Combine the results of Part2 into one final result
            
            //if the createFinalObject is set to true, make the final object and destroy each of the previously created objects
            //when different material -> dont merge sub meshes
            if (createFinalObject)
            {
                GameObject finalResultingObj = CombineMeshes(instancedObjects, false);

                if (addMeshColliderToTheFinalObj)
                    finalResultingObj.AddComponent<MeshCollider>();

                var mesh = finalResultingObj.GetComponent<MeshFilter>().sharedMesh;
                mesh.SetTriangles(mesh.triangles, 0);
                
                
                SaveMesh(mesh, "mesh");
            }
            
            #endregion

            #region Part4 - Destroy or Deactivate objects on based on the initial parameters

            //Destroy the DeCombined Meshes
            //It is necessary to use this IEnumerator because DestroyImmediate can not be called during OnValidate()
            StartCoroutine(DestroyImmediateAfterTime(newObjectsToCombineList));
            
            //disable previous objs that had the mesh filter and mesh renderer.
            if (disableCombinedObjs)
            {
                foreach (var t in objectsToCombine)
                {
                    t.SetActive(false);
                }
            }

            //destroy previous objs that had the mesh filter and mesh renderer.
            if (destroyCombinedObjs)
            {
                //It is necessary to use this IEnumerator because DestroyImmediate can not be called during OnValidate()
                StartCoroutine(DestroyImmediateAfterTime(objectsToCombine));
            }

            //destroy the previously combined objects that had separate materials as they are no longer needed 
            if (destroyPreviousCreationsDuringFinalMerge)
            {
                //It is necessary to use this IEnumerator because DestroyImmediate can not be called during OnValidate()
                StartCoroutine(DestroyImmediateAfterTime(instancedObjects));
            }
            #endregion
            
            //trigger event
            meshesCombinedEvent.Invoke();
        }

        #region Combine&DeCombine Meshes

        private GameObject CombineMeshes(List<GameObject> objectsToCombine, bool mergeSubMeshes)
        {
            //find the mesh filters and mesh renderers that need to be combined
            List<MeshFilter> meshFilters = GetMeshFilters(objectsToCombine);
            List<MeshRenderer> meshRenderers = GetMeshRenderers(objectsToCombine);

            //create the combine instance
            CombineInstance[] combine = new CombineInstance[meshFilters.Count];

            //calculate the combine instance parts
            CalculateCombineInstanceParts(combine, meshFilters);

            //find the materials needed
            List<Material> _sharedMaterials = FindTheMaterialsNeeded(meshFilters);

            //create the new mesh
            Mesh _newMesh = new Mesh
            {
                name = finalCombinedMeshName
            };
            if(useUint32)
                _newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            _newMesh.CombineMeshes(combine, mergeSubMeshes, true);

            //Create the new instanced object that will hold the instanced mesh
            GameObject _instancedObj = CreateTheNewObject();

            //Add the necessary mesh filter and mesh renderer
            MeshFilter _meshFilter = _instancedObj.GetComponent<MeshFilter>();
            _meshFilter = _meshFilter == null ? _instancedObj.AddComponent<MeshFilter>() : _meshFilter;
            MeshRenderer _meshRenderer = _instancedObj.GetComponent<MeshRenderer>();
            _meshRenderer = _meshRenderer == null ? _instancedObj.AddComponent<MeshRenderer>() : _meshRenderer;

            //Assign mesh
            _meshFilter.sharedMesh = _newMesh;

            //Assign materials
            Renderer _renderer = _instancedObj.GetComponent<Renderer>();
            _renderer.sharedMaterials = _sharedMaterials.ToArray();
            
            //assign variables
            //The reason these variables exist is because in some scenario's they might be needed to be accessed.
            //That is why the meshesCombinedEvent exists.
            newMesh = _newMesh;
            sharedMaterials = _sharedMaterials.ToArray();
            
            //not sure why but this is needed
            _instancedObj.SetActive(true);

            return _instancedObj;
        }

        /// <summary>
        /// Creates multiple game objects with single meshes out of one game object with multiple sub mesh
        /// </summary>
        private List<GameObject> MeshDeCombine(GameObject objToDeCombine)
        {
            //get the mesh filter
            MeshFilter _meshFilter = objToDeCombine.GetComponent<MeshFilter>();
            //get the mesh renderer
            Renderer _renderer = objToDeCombine.GetComponent<Renderer>();
            //get the mesh that is to be deCombined
            Mesh _meshToDeCombine = _meshFilter.sharedMesh;

            //create the sub mesh descriptor list
            List<SubMeshDescriptor> subMeshDescriptors = new List<SubMeshDescriptor>();
            
            //populate the sub mesh descriptor list with the sub meshes that are to be deCombined
            for (int i = 0; i < _meshToDeCombine.subMeshCount; i++)
            {
                subMeshDescriptors.Add(_meshToDeCombine.GetSubMesh(i));
            }

            List<GameObject> deCombinedInstances = new List<GameObject>();
            for (var subMeshIndex = 0; subMeshIndex < subMeshDescriptors.Count; subMeshIndex++)
            {
                var subMeshDescriptor = subMeshDescriptors[subMeshIndex];
                GameObject newInstance = new GameObject
                {
                    name = "DeCombinedInstance",
                    transform =
                    {
                        parent = this.transform,
                        rotation = objToDeCombine.transform.rotation,
                        position = objToDeCombine.transform.position,
                        localScale = objToDeCombine.transform.localScale
                    }
                };

                MeshFilter _mF = newInstance.AddComponent<MeshFilter>();
                MeshRenderer _mR = newInstance.AddComponent<MeshRenderer>();
                
                Mesh subMesh = new Mesh
                {
                    name = "Sub_Mesh"
                };
                if(useUint32)
                    subMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

                // Copy the vertices, triangles, and other data for the sub mesh
                subMesh.SetVertices(new List<Vector3>(_meshToDeCombine.vertices));
                subMesh.SetTriangles(_meshToDeCombine.GetTriangles(subMeshIndex), 0);

                // If you have other data (normals, UVs, etc.), copy them as well
                subMesh.SetNormals(new List<Vector3>(_meshToDeCombine.normals));
                subMesh.SetUVs(0, new List<Vector2>(_meshToDeCombine.uv));

                _mF.sharedMesh = subMesh;
                
                //assign the material for the sub mesh
                _mR.sharedMaterial = _renderer.sharedMaterials[subMeshIndex];
                deCombinedInstances.Add(newInstance);
            }

            return deCombinedInstances;
        }
        
        #endregion
        
        public static void SaveMesh(Mesh mesh, string name)
        {
            string path = EditorUtility.SaveFilePanel("Save Separate Mesh Asset", "Assets/", name, "asset");
            if (string.IsNullOrEmpty(path)) return;

            path = FileUtil.GetProjectRelativePath(path);

            Mesh meshToSave = Object.Instantiate(mesh) as Mesh;

            AssetDatabase.CreateAsset(meshToSave, path);
            AssetDatabase.SaveAssets();
        }

        #region HelperFunctions

        private static List<MeshFilter> GetMeshFilters(List<GameObject> objectsToCombine)
        {
            //find the mesh filters that need to be combined
            List<MeshFilter> meshFilters = objectsToCombine.Select(mF => mF.GetComponent<MeshFilter>()).ToList();

            // remove null -> not sure this is needed but i think there was a case where i found it necessary.
            for (int i = 0; i < meshFilters.Count; i++)
            {
                if (meshFilters[i] != null) continue;
                meshFilters.RemoveAt(i);
                i--;
            }

            return meshFilters;
        }

        private static List<MeshRenderer> GetMeshRenderers(List<GameObject> objectsToCombine)
        {
            //find the mesh renderers that need to be combined
            List<MeshRenderer> meshRenderers = objectsToCombine.Select(mF => mF.GetComponent<MeshRenderer>()).ToList();

            for (int i = 0; i < meshRenderers.Count; i++)
            {
                if (meshRenderers[i] != null) continue;
                meshRenderers.RemoveAt(i);
                i--;
            }

            return meshRenderers;
        }

        private static void CalculateCombineInstanceParts(CombineInstance[] combine, List<MeshFilter> meshFilters)
        {
            //calculate the combine instance parts

            for (int i = 0; i < meshFilters.Count; i++)
            {
                //Assign the mesh
                combine[i].mesh = meshFilters[i].sharedMesh;
                //assign the transform
                combine[i].transform = Matrix4x4.TRS(meshFilters[i].transform.localPosition,
                    meshFilters[i].transform.localRotation,
                    meshFilters[i].transform.localScale);
            }
        }

        private static List<Material> FindTheMaterialsNeeded(List<MeshFilter> meshFilters)
        {
            //find the materials needed
            List<Material> _sharedMaterials = new List<Material>();
            foreach (var meshFilter in meshFilters)
            {
                //find the materials each mesh Filter has
                Material[] _newMaterials = meshFilter.GetComponent<Renderer>().sharedMaterials;

                foreach (var _newMaterial in _newMaterials)
                {
                    //if the material is not already in the materials list add it
                    if (!_sharedMaterials.Contains(_newMaterial))
                        _sharedMaterials.Add(_newMaterial);
                }
            }

            return _sharedMaterials;
        }

        private GameObject CreateTheNewObject()
        {
            Vector3 vector3Zero = Vector3.zero;
            //Create the new instanced object that will hold the instanced mesh
            GameObject _instancedObj = new GameObject
            {
                name = instancedObjectName,
                transform =
                {
                    parent = this.transform,
                    localRotation = Quaternion.Euler(vector3Zero),
                    localPosition = vector3Zero,
                    localScale = Vector3.one
                }
            };

            return _instancedObj;
        }
        
        private static List<ObjListWithMaterialIndex> FindObjectsThatUseThatMaterial(List<GameObject> objectsToCombine , Material sharedMaterial, List<ObjListAndMaterial> objListAndMaterialsAlreadyAdded)
        {
            List<ObjListWithMaterialIndex> objListThatUseThatMaterial = new List<ObjListWithMaterialIndex>();

            //search the objects to combine
            foreach (var objectToCombine in objectsToCombine)
            {
                
                //object already in a list
                bool objAlreadyInAList = false;
                
                //check if object already in a list
                foreach (var ObjListAndMaterial in objListAndMaterialsAlreadyAdded)
                {
                    if(objAlreadyInAList) continue;
                    foreach (var oTC in ObjListAndMaterial.objectsToCombine)
                    {
                        if(objAlreadyInAList) continue;
                        if (oTC.objectToCombine == objectToCombine)
                            objAlreadyInAList = true;
                    }
                }
                
                //object already in a list so skip it
                if(objAlreadyInAList) continue;
                
                // foreach object get the materials it has
                List<Material> objectToCombineMaterials =
                    objectToCombine.GetComponent<Renderer>().sharedMaterials.ToList();

                //if the material i want to check is part of the materials list
                if (objectToCombineMaterials.Contains(sharedMaterial))
                {
                    int materialIndex = -1;

                    //find the index of the material i want to check
                    for (var i = 0; i < objectToCombineMaterials.Count; i++)
                    {
                        var _mat = objectToCombineMaterials[i];
                        if (_mat != sharedMaterial) continue;

                        //if index found then break
                        materialIndex = i;
                        break;
                    }

                    //if the material is still -1 then its not part of this object so continue
                    if(materialIndex == -1) continue;
                    
                    ObjListWithMaterialIndex objListWithMaterialIndex = new ObjListWithMaterialIndex
                    {
                        objectToCombine = objectToCombine,
                        materialSubMeshIIndex = materialIndex
                    };

                    objListThatUseThatMaterial.Add(objListWithMaterialIndex);
                }
            }

            return objListThatUseThatMaterial;
        }
        
        
        //It is necessary to use this IEnumerator because DestroyImmediate can not be called during OnValidate()
        private static IEnumerator DestroyImmediateAfterTime(List<GameObject> objsToDestroy)
        {
            yield return new WaitForSeconds(0.1f);
            foreach (var obj in objsToDestroy)
            {
                DestroyImmediate(obj);
            }
            objsToDestroy.Clear();
        }

        #endregion
    }
    
    

    #region HelperClasses
    
    public class ObjListAndMaterial
    {
        public List<ObjListWithMaterialIndex> objectsToCombine;
        public Material material;
    }

    public class ObjListWithMaterialIndex
    {
        public GameObject objectToCombine;
        public int materialSubMeshIIndex;
    }
    
    #endregion
}