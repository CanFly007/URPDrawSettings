using System.Collections.Generic;
using UnityEngine;

public struct FindResult
{
    public FindPlayer[] foundPlayers;
    public int count;
}

public struct FindPlayer
{
    public GameObject playerGameObject;
    public float ratio;
}

public static class FindPlayerManager
{
    private static readonly int m_PropIdColor = Shader.PropertyToID("_FindColor");
    private static MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();

    private static Color[] m_PresetColors = new Color[]
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
        if (players.Count > m_PresetColors.Length)
        {
            Debug.LogError("Not enough preset colors to assign to each player.");
            findResult = default(FindResult);
            return false;
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
        for (int i = 0; i < players.Count; i++)
        {
            Color detectColor = m_PresetColors[i];
            colorToPlayerMap[detectColor] = players[i];

            Renderer[] renderers = players[i].GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                m_PropertyBlock.SetColor(m_PropIdColor, detectColor);
                renderer.SetPropertyBlock(m_PropertyBlock);
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

        List<FindPlayer> detectedPlayers = new List<FindPlayer>();
        int totalPixels = textureWidth * textureHeight;
        foreach (var kvp in playerPixelCounts)
        {
            float playerRatio = kvp.Value / (float)totalPixels;
            if (playerRatio > 0)
            {
                detectedPlayers.Add(new FindPlayer { playerGameObject = kvp.Key, ratio = playerRatio });
            }
        }

        findResult.foundPlayers = detectedPlayers.ToArray();
        findResult.count = detectedPlayers.Count;

        return findResult.count > 0;
    }
}