using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestManager : MonoBehaviour
{
    public Camera detectionCamera;

    public ComputeShader computeShader;

    public bool useGPU;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            MonsterVisionProcessor.Initialize(computeShader, useGPU);

            List<GameObject> players = new List<GameObject>();
            FindePlayers(players);
            VisionResult result;
            bool found = MonsterVisionProcessor.TryDetectPlayers(players, detectionCamera, out result);
            if (found)
            {
                Debug.Log("I see you: " + result.VisibleCount);
                for (int i = 0; i < result.VisibleCount; ++i)
                {
                    Debug.Log(result.VisiblePlayers[i].PlayerObject.name + " : " + result.VisiblePlayers[i].ScreenSpaceRatio);
                }
            }
            else
            {
                Debug.Log("Can not see you");
            }
        }
    }

    private static void FindePlayers(List<GameObject> players)
    {
        int playerLayerIndex = LayerMask.NameToLayer("Player");
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == playerLayerIndex)
            {
                players.Add(obj);
            }
        }
    }
}
