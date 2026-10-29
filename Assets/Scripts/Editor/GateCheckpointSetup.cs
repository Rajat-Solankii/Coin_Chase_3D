using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class GateCheckpointSetup
{

    [MenuItem("Tools/Setup Gate Checkpoints")]
    public static void ExecuteSetup()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "SampleScene") return;

        Debug.Log("<color=cyan><b>=== Final Gate Checkpoint Alignment & Setup ===</b></color>");

        // Delete any empty CheckpointGate wrapper if it has no MeshRenderer
        GameObject emptyWrapper = GameObject.Find("CheckpointGate");
        if (emptyWrapper != null && emptyWrapper.GetComponentsInChildren<MeshRenderer>().Length == 0)
        {
            Debug.Log($"Destroying empty wrapper: {emptyWrapper.name}");
            Object.DestroyImmediate(emptyWrapper);
        }

        // Target locations:
        Vector3 forestGateTarget = new Vector3(313.21f, 0f, 92.87f);
        Vector3 parkourGateTarget = new Vector3(240.78f, 9.73f, 141.58f);

        GameObject forestGate = null;
        GameObject parkourGate = null;

        foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            GameObject root = mr.transform.root.gameObject;
            // Also check if mr is child of a gate object
            Transform t = mr.transform;
            while (t != null)
            {
                if (t.name.Contains("tripo") || t.name.Contains("Gate"))
                {
                    float dF = Vector3.Distance(t.position, forestGateTarget);
                    float dP = Vector3.Distance(t.position, parkourGateTarget);

                    if (dF < 10f && forestGate == null)
                    {
                        forestGate = t.gameObject;
                    }
                    if (dP < 10f && parkourGate == null)
                    {
                        parkourGate = t.gameObject;
                    }
                }
                t = t.parent;
            }
        }

        Debug.Log($"Forest Gate found: {(forestGate != null ? forestGate.name + " at " + forestGate.transform.position + " rot=" + forestGate.transform.eulerAngles : "NULL")}");
        Debug.Log($"Parkour Gate found: {(parkourGate != null ? parkourGate.name + " at " + parkourGate.transform.position + " rot=" + parkourGate.transform.eulerAngles : "NULL")}");

        if (forestGate != null)
        {
            forestGate.transform.SetParent(null, true);
            forestGate.name = "CheckpointGate_Forest";
            SetupGateTrigger(forestGate, "Checkpoint 1 (Forest Gate)", stopAbe: true);
        }

        if (parkourGate != null)
        {
            parkourGate.transform.SetParent(null, true);
            parkourGate.name = "CheckpointGate_Parkour";
            SetupGateTrigger(parkourGate, "Checkpoint 2 (Parkour Gate)", stopAbe: false);
        }

        // Clean up any remaining obsolete checkpoints in scene
        Checkpoint[] allCheckpoints = Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
        foreach (var cp in allCheckpoints)
        {
            bool isForest = (forestGate != null && cp.transform.IsChildOf(forestGate.transform));
            bool isParkour = (parkourGate != null && cp.transform.IsChildOf(parkourGate.transform));

            if (!isForest && !isParkour)
            {
                Debug.Log($"Purging old Checkpoint on: {cp.gameObject.name} at {cp.transform.position}");
                if (cp.gameObject.name.StartsWith("Checkpoint") && !cp.gameObject.name.Contains("Gate"))
                {
                    cp.gameObject.SetActive(false);
                }
                Object.DestroyImmediate(cp);
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("<color=green><b>=== Gate Checkpoints Finished and Scene Saved! ===</b></color>");
    }

    private static void SetupGateTrigger(GameObject gate, string label, bool stopAbe)
    {
        // Remove any old components on gate root
        Checkpoint oldRootCp = gate.GetComponent<Checkpoint>();
        if (oldRootCp != null) Object.DestroyImmediate(oldRootCp);

        BoxCollider oldRootCol = gate.GetComponent<BoxCollider>();
        if (oldRootCol != null) Object.DestroyImmediate(oldRootCol);

        // Get world renderers bounds
        Renderer[] renderers = gate.GetComponentsInChildren<Renderer>();
        Bounds worldBounds = new Bounds(gate.transform.position, Vector3.zero);
        bool hasBounds = false;
        foreach (var r in renderers)
        {
            if (r.gameObject.name == "CheckpointTrigger") continue;
            if (!hasBounds)
            {
                worldBounds = r.bounds;
                hasBounds = true;
            }
            else
            {
                worldBounds.Encapsulate(r.bounds);
            }
        }

        Debug.Log($"{label} World Bounds: Center={worldBounds.center}, Size={worldBounds.size}");

        // Child object CheckpointTrigger
        Transform trigTrans = gate.transform.Find("CheckpointTrigger");
        GameObject trigGO;
        if (trigTrans == null)
        {
            trigGO = new GameObject("CheckpointTrigger");
            trigGO.transform.SetParent(gate.transform, false);
        }
        else
        {
            trigGO = trigTrans.gameObject;
        }

        // Keep trigGO aligned with gate rotation, positioned at center of bounds
        trigGO.transform.position = worldBounds.center;
        trigGO.transform.rotation = gate.transform.rotation;

        BoxCollider box = trigGO.GetComponent<BoxCollider>();
        if (box == null) box = trigGO.AddComponent<BoxCollider>();
        box.isTrigger = true;

        // Size BoxCollider in local space
        // Since the gate arch might be rotated in world space, we can compute local bounds of the mesh
        MeshFilter mf = gate.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            Bounds localMeshBounds = mf.sharedMesh.bounds;
            Vector3 lossy = gate.transform.lossyScale;
            float localW = Mathf.Max(Mathf.Abs(localMeshBounds.size.x * lossy.x), 4.5f);
            float localH = Mathf.Max(Mathf.Abs(localMeshBounds.size.y * lossy.y), 4.0f);
            float localD = Mathf.Max(Mathf.Abs(localMeshBounds.size.z * lossy.z), 3.0f);

            box.size = new Vector3(localW, localH, localD);
            box.center = Vector3.zero;
            Debug.Log($"{label} Local BoxCollider Size: {box.size}");
        }
        else
        {
            box.size = new Vector3(5f, 4f, 4f);
            box.center = Vector3.zero;
        }

        // Setup Checkpoint component
        Checkpoint cp = trigGO.GetComponent<Checkpoint>();
        if (cp == null) cp = trigGO.AddComponent<Checkpoint>();
        cp.stopAbeThief = stopAbe;
        cp.hideOnCollect = false;

        // Ground spawn position:
        // Center X, Z and floor height (min.y + 0.15f)
        Vector3 groundSpawn = new Vector3(worldBounds.center.x, worldBounds.min.y + 0.15f, worldBounds.center.z);
        cp.spawnOffset = groundSpawn - trigGO.transform.position;

        Debug.Log($"<color=green>{label} Successfully Set Up! Trigger at {trigGO.transform.position}, Respawn at {groundSpawn}, stopAbe={stopAbe}</color>");
    }
}
