using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestManager : MonoBehaviour
{
    public Camera detectionCamera;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            List<GameObject> players = new List<GameObject>();
            FindePlayers(players);
            FindResult result;
            bool found = FindPlayerManager.DetectPlayers(players, detectionCamera, out result);
            if (found)
            {
                Debug.Log("I see you: " + result.count);
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
