using UnityEngine;
using UnityEditor;

/// <summary>
/// Menu za (re)izgradnju placeholder humanoid rig-a pod Player-om.
/// Runtime ga ProceduralPlayerAnimator gradi sam ako nedostaje;
/// menu je za manuelnu izgradnju u editoru (prefab/scene).
/// Menja se pravim Blender rig-om kad FBX stigne (docs/ANIMACIJE.md).
/// Tools > Build Placeholder Rig.
/// </summary>
public static class PlaceholderRigBuilder
{
    [MenuItem("Tools/Build Placeholder Rig")]
    public static void Build()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("PlaceholderRigBuilder: Player objekat nije nadjen.");
            return;
        }

        ProceduralPlayerAnimator.BuildRig(player);
        Debug.Log("PlaceholderRigBuilder: rig izgradjen pod " + player.name + ".");
    }
}
