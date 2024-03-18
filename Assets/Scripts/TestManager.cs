using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestManager : MonoBehaviour
{
    public Camera[] detectionCameras;

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

            for (int i = 0; i < detectionCameras.Length; i++)
            {
                bool found = MonsterVisionProcessor.TryDetectPlayers(players, detectionCameras[i], out result);
                Debug.Log(detectionCameras[i].name);
                if (found)
                {
                    Debug.Log("I see you: " + result.VisibleCount);
                    for (int j = 0; j < result.VisibleCount; ++j)
                    {
                        Debug.Log(result.VisiblePlayers[j].PlayerObject.name + " : " + result.VisiblePlayers[j].ScreenSpaceRatio);
                    }
                }
                else
                {
                    Debug.Log("Can not see you");
                }
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
