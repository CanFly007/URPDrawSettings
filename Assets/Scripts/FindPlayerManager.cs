using System.Collections.Generic;
using UnityEngine;

public struct VisionResult
{
    public VisiblePlayer[] VisiblePlayers;
    public int VisibleCount;
}

public struct VisiblePlayer
{
    public GameObject PlayerObject;
    public float ScreenSpaceRatio;
}

public static class MonsterVisionProcessor
{
    private static readonly int s_ShaderPropertyColor = Shader.PropertyToID("_FindColor");
    private static MaterialPropertyBlock s_MaterialPropertyBlock = new MaterialPropertyBlock();

    private static Color[] s_DetectionColors = new Color[]
    {
        new Color(1, 0, 0, 1),
        new Color(0, 1, 0, 1),
        new Color(0, 0, 1, 1),
        new Color(1, 1, 0, 1),
        new Color(1, 0, 1, 1),
        new Color(0, 1, 1, 1),
    };

    public static bool TryDetectPlayers(List<GameObject> players, Camera monsterCamera, out VisionResult visionResult)
    {
        if (players.Count > s_DetectionColors.Length)
        {
            Debug.LogError("Insufficient detection colors for the number of players.");
            visionResult = new VisionResult();
            return false;
        }

        int rtHeight = 256;
        int rtWidth = Mathf.RoundToInt(rtHeight * monsterCamera.aspect);
        RenderTexture renderTexture = RenderTexture.GetTemporary(rtWidth, rtHeight, 32, RenderTextureFormat.ARGB32);

        monsterCamera.targetTexture = renderTexture;
        monsterCamera.clearFlags = CameraClearFlags.SolidColor;
        monsterCamera.backgroundColor = Color.black;

        Graphics.SetRenderTarget(renderTexture);
        GL.Clear(true, true, Color.black);

        Dictionary<Color, GameObject> colorPlayerMap = new Dictionary<Color, GameObject>();
        for (int i = 0; i < players.Count; ++i)
        {
            Color detectionColor = s_DetectionColors[i];
            colorPlayerMap[detectionColor] = players[i];

            Renderer[] playerRenderers = players[i].GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in playerRenderers)
            {
                s_MaterialPropertyBlock.SetColor(s_ShaderPropertyColor, detectionColor);
                renderer.SetPropertyBlock(s_MaterialPropertyBlock);
            }
        }

        monsterCamera.Render();

        RenderTexture.active = renderTexture;
        Texture2D texture = new Texture2D(rtWidth, rtHeight, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, rtWidth, rtHeight), 0, 0);
        texture.Apply();

        var playerPixelCounts = new Dictionary<GameObject, int>();
        Color[] pixels = texture.GetPixels();
        foreach (Color pixel in pixels)
        {
            if (colorPlayerMap.ContainsKey(pixel))
            {
                if (!playerPixelCounts.ContainsKey(colorPlayerMap[pixel]))
                {
                    playerPixelCounts[colorPlayerMap[pixel]] = 0;
                }
                playerPixelCounts[colorPlayerMap[pixel]]++;
            }
        }

        UnityEngine.Object.Destroy(texture);
        //RenderTexture.ReleaseTemporary(renderTexture);
        //monsterCamera.targetTexture = null;

        var visiblePlayers = new List<VisiblePlayer>();
        int totalPixels = rtWidth * rtWidth;
        foreach (var kvp in playerPixelCounts)
        {
            float ratio = kvp.Value / (float)totalPixels;
            if (ratio > 0)
            {
                visiblePlayers.Add(new VisiblePlayer { PlayerObject = kvp.Key, ScreenSpaceRatio = ratio });
            }
        }

        visionResult.VisiblePlayers = visiblePlayers.ToArray();
        visionResult.VisibleCount = visiblePlayers.Count;


        return visionResult.VisibleCount > 0;
    }
}