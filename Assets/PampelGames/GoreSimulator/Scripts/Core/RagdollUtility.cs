// ----------------------------------------------------
// Gore Simulator
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace PampelGames.GoreSimulator
{
    internal static class RagdollUtility
    {
        public static void ToggleRagdoll(IEnumerable<GoreBone> goreBones, bool active, SkinnedMeshRenderer smr, bool updateWhenOffscreenDefault)
        {
            if (active)
            {
                foreach (var goreBone in goreBones)
                {
                    goreBone._rigidbody.isKinematic = false;
                    goreBone._rigidbody.linearVelocity = Vector3.zero;
                    goreBone._rigidbody.angularVelocity = Vector3.zero;
                }

                smr.updateWhenOffscreen = true;
            }
            else
            {
                foreach (var goreBone in goreBones)
                {
                    // goreBone._rigidbody.velocity = Vector3.zero;
                    // goreBone._rigidbody.angularVelocity = Vector3.zero;
                    goreBone._rigidbody.isKinematic = true;
                }

                smr.updateWhenOffscreen = updateWhenOffscreenDefault;
            }
            
        }
        
        /********************************************************************************************************************************/
        // Sub-module Ragdoll Cut 
        /********************************************************************************************************************************/

        public static bool ExecuteRagdollCut(GoreSimulator _goreSimulator, SubModuleCutRagdoll moduleCutRagdoll, 
            BonesClass bonesClass, CutIndexClass cutIndexClass, ExecutionCutClass executionCutClass, SubModuleClass subModuleClass,
            List<Vector3> bakedVertices,
            out GoreMultiCut goreMultiCut)
        {
            goreMultiCut = default;
            var indexesStorageClass = bonesClass.indexesStorageClass;

            if (indexesStorageClass.cutMeshes[cutIndexClass.cutIndex] != null)
                indexesStorageClass.cutMeshes[cutIndexClass.cutIndex].Clear();
            else
                indexesStorageClass.cutMeshes[cutIndexClass.cutIndex] = new Mesh();
                
            var validateContinue = MeshCutJobs.IndexesSnapshotCut(_goreSimulator.smr.transform, _goreSimulator.storage.meshDataClass, _goreSimulator.meshNativeDataClass, 
                _goreSimulator.originalMesh, executionCutClass, indexesStorageClass.cutMeshes[cutIndexClass.cutIndex], false, out var cutCenters);
            if (!validateContinue) return false;
                
            subModuleClass.cutPosition = cutCenters[^1];
            
            indexesStorageClass.cutMeshes[cutIndexClass.cutIndex].RecalculateBounds();
            
            subModuleClass.cutDirection = CutUtility.GetCutDirection(_goreSimulator.smr.transform, 
                 indexesStorageClass.cutMeshes[cutIndexClass.cutIndex].bounds.center, subModuleClass.cutPosition);

            subModuleClass.subRagdoll = true;
            var detachedObject = CreateRagdollObject(_goreSimulator.smr,
                _goreSimulator.gameObject.name + " - " + bonesClass.bone.name + " - " + cutIndexClass.cutIndex + " - Ragdoll");

            goreMultiCut = detachedObject.gameObject.AddComponent<GoreMultiCut>();
            goreMultiCut.status = Enums.MultiCutStatus.Ragdoll;
            goreMultiCut._goreSimulator = _goreSimulator;
            goreMultiCut.bakedMesh = _goreSimulator.bakedMesh;

            moduleCutRagdoll.ExecuteRagdollSubModule(detachedObject, indexesStorageClass.cutMeshes[cutIndexClass.cutIndex], cutIndexClass, bonesClass,
                cutCenters, subModuleClass, goreMultiCut);

            return true;
        }

        private static GameObject CreateRagdollObject(SkinnedMeshRenderer smr, string objName)
        {
            var smrTransform = smr.transform;
            var originalSMRParent = smrTransform.parent;
            var originalRootBoneParent = smr.rootBone.parent;

            GameObject tempObj = new GameObject();
            tempObj.transform.SetPositionAndRotation(smrTransform.position, smrTransform.rotation);

            smr.transform.SetParent(tempObj.transform);
            smr.rootBone.SetParent(tempObj.transform);
            var ragdollCutCopy = Object.Instantiate(tempObj);
            ragdollCutCopy.name = objName;
            
            smr.transform.SetParent(originalSMRParent);
            smr.rootBone.SetParent(originalRootBoneParent);
            
            Object.Destroy(tempObj);
            return ragdollCutCopy;
        }
        
        
    }
}