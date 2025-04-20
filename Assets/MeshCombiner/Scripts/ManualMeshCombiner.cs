using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace MeshCombiner_Rookie0ne
{
    [ExecuteInEditMode]
    public class ManualMeshCombiner : MeshCombiner
    {

        [TextArea(3, 3)] [ReadOnly] [SerializeField]
        private string Notes2 = "Place This Script on an empty Game object " +
                               "and then assign the objects you want to combine to the corresponding field. " +
                               "Then just hit the combineMeshes boolean and watch it work.";

        [Tooltip("A list of Game Objects. " +
                 "When Combining meshes each of those objects that has " +
                 "a mesh renderer and a mesh filter will be combined")]
        [SerializeField]
        private List<GameObject> objectsToCombine;

        [Tooltip("When clicked the CombineMeshes function will be called. " +
                 "Then this bool will automatically again be set to false.")]
        [SerializeField]
        private bool combineMeshes;

        private void OnValidate()
        {
            if (combineMeshes)
            {
                RobustCombineMeshes(objectsToCombine);
                combineMeshes = false;
            }
        }
    }
}