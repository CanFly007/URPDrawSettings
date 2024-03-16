using System.Collections.Generic;
using UnityEngine;

public struct FindResult
{
    public FindPlayer[] findPlayers;
    public int count;
}

public struct FindPlayer
{
    public GameObject playerGo;
    public float ratio;
}

public static class FindPlayerManager
{
    private static readonly int PropIdColor = Shader.PropertyToID("_FindColor");
    private static MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

    private static Color[] presetColors = new Color[]
    {
        new Color(1,0,0,1),
        new Color(0,1,0,1),
        new Color(0,0,1,1),
        new Color(1,1,0,1),
        new Color(1,0,1,1),
        new Color(0,1,1,1),
    };

    public static bool DetectPlayers(List<GameObject> players, Camera camera, out FindResult findResult)
    {
        if (players.Count > presetColors.Length)
        {
            Debug.LogError("没有足够的预设颜色为每个玩家分配！");
            //return false; // 早期返回以避免错误
        }

        int textureHeight = 256;
        int textureWidth = Mathf.RoundToInt(textureHeight * camera.aspect);
        RenderTexture renderTexture = RenderTexture.GetTemporary(textureWidth, textureHeight, 32, RenderTextureFormat.ARGB32);

        camera.targetTexture = renderTexture;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;

        Graphics.SetRenderTarget(renderTexture);
        GL.Clear(true, true, Color.black);

        Dictionary<Color, GameObject> colorToPlayerMap = new Dictionary<Color, GameObject>();
        List<Color> detectionColors = new List<Color>();
        for (int i = 0; i < players.Count; i++)
        {
            Color detectColor = presetColors[i];
            detectionColors.Add(detectColor);
            colorToPlayerMap[detectColor] = players[i];

            Renderer[] renderers = players[i].GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                propertyBlock.SetColor(PropIdColor, detectColor);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        camera.Render();

        RenderTexture.active = renderTexture;
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
        texture.Apply();

        Dictionary<GameObject, int> playerPixelCounts = new Dictionary<GameObject, int>();
        Color[] pixels = texture.GetPixels();

        foreach (Color pixel in pixels)
        {
            if (colorToPlayerMap.ContainsKey(pixel))
            {
                if (!playerPixelCounts.ContainsKey(colorToPlayerMap[pixel]))
                {
                    playerPixelCounts[colorToPlayerMap[pixel]] = 0;
                }
                playerPixelCounts[colorToPlayerMap[pixel]]++;
            }
        }

        // Clean up
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexture);
        camera.targetTexture = null;
        UnityEngine.Object.Destroy(texture);

        List<FindPlayer> foundPlayers = new List<FindPlayer>();
        int texturePixels = textureWidth * textureHeight;
        foreach (var kvp in playerPixelCounts)
        {
            float ratio = kvp.Value / (float)texturePixels;
            if (ratio > 0)
            {
                foundPlayers.Add(new FindPlayer { playerGo = kvp.Key, ratio = ratio });
            }
        }

        findResult.findPlayers = foundPlayers.ToArray();
        findResult.count = foundPlayers.Count;

        return findResult.count > 0;
    }
}