using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using MeshCombiner_Rookie0ne;
using UnityEngine;

namespace MeshCombiner_Rookie0ne
{
    [ExecuteInEditMode]
    public class ManualMeshCombiner_Alternate_1 : MeshCombiner
    {
        [TextArea(3, 3)] [SerializeField] private string Notes2 = "Place This Script on an empty Game object " +
                                                                  "and then assign the parent of the objects you want to combine to the corresponding field. " +
                                                                  "Then just hit the combineMeshes boolean and watch it work.";

        [Tooltip("In this field Assign the parent of the objects that you want to combine\n" +
                 "(for obvious reasons only objects with mesh filters and mesh renderers will be combined)")]
        [SerializeField]
        private GameObject objectsToCombineParent;

        [Tooltip("If true then the combined mesh will include both active and inactive children.\n" +
                 "if false then the combined mesh will only include the active children")]
        [SerializeField]
        private bool combineInactiveChildren;

        [Tooltip("Assign here any child that you want to specifically exclude from the mesh combination process")]
        [SerializeField] private List<GameObject> childrenToExcludeFromCombination = new List<GameObject>();

        [Tooltip("When clicked the CombineMeshes function will be called. " +
                 "Then this bool will automatically again be set to false.")]
        [SerializeField]
        private bool combineMeshes;

        private void OnValidate()
        {
            if (combineMeshes)
            {
                List<GameObject> objectsToCombine = GetChildrenObjectsToCombine(objectsToCombineParent);
                RobustCombineMeshes(objectsToCombine);
                combineMeshes = false;
            }
        }

        private List<GameObject> GetChildrenObjectsToCombine(GameObject _objectsToCombineParent)
        {
            List<GameObject> _objectsToCombine = new List<GameObject>();
            
            //loop through the children
            LoopThroughChildren(_objectsToCombineParent.transform, _objectsToCombine);
            Debug.Log("_objectsToCombine count :" + _objectsToCombine.Count);
             
            return _objectsToCombine;
        }

        private void LoopThroughChildren(Transform parentTransform, List<GameObject> _objectsToCombine)
        {
            foreach (Transform childTr in parentTransform.transform)
            {
                //loop through the rest of the children.
                LoopThroughChildren(childTr, _objectsToCombine);
                
                GameObject child = childTr.gameObject;
                MeshFilter mF = child.GetComponent<MeshFilter>();
                MeshRenderer mR = child.GetComponent<MeshRenderer>();

                //check if the child has both a meshRenderer as well as a meshFilter
                if (mR == null || mF == null) continue;

                //if set to dont combine inactive children and child not active then continue
                if (!combineInactiveChildren)
                {
                    if (!child.activeSelf) continue;
                }

                //check if the child is on the exclusion list
                bool found = false;
                foreach (var childToExclude in childrenToExcludeFromCombination)
                {
                    if (childToExclude != child) continue;
                    found = true;
                    break;
                }
                //if true then the child is on the exclusion list so continue
                if(found) continue;
                
                //if it got to here that means the child is suitable to be combined
                //add the child to the list of objects to combine
                _objectsToCombine.Add(child);
            }
        }
    }
}