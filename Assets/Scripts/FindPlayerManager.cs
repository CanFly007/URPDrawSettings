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

    private static ComputeShader s_PlayerDetectionShader;
    private static int s_KernelID;
    private static ComputeBuffer s_PlayerPixelCountsBuffer;
    private static bool s_UseGPU = true; // 切换GPU和CPU处理

    public static void Initialize(ComputeShader playerDetectionShader)
    {
        s_PlayerDetectionShader = playerDetectionShader;
        s_KernelID = s_PlayerDetectionShader.FindKernel("CSMain");
    }

    private static Color[] s_DetectionColors = new Color[]
    {
        new Color(1, 0, 0, 1),
        new Color(0, 1, 0, 1),
        new Color(0, 0, 1, 1),
        new Color(1, 1, 0, 1),
        new Color(1, 0, 1, 1),
        new Color(0, 1, 1, 1),
    };
    private static int NUM_PLAYERS = 2;

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


        if (s_UseGPU)
        {
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();

            monsterCamera.targetTexture = renderTexture;
            monsterCamera.Render();

            s_PlayerDetectionShader.SetTexture(s_KernelID, "InputTexture", renderTexture);

            // 创建计数器缓冲区
            s_PlayerPixelCountsBuffer = new ComputeBuffer(NUM_PLAYERS, sizeof(int));
            int[] playerPixelCounts2 = new int[NUM_PLAYERS];
            s_PlayerPixelCountsBuffer.SetData(playerPixelCounts2); // 初始化为0

            s_PlayerDetectionShader.SetBuffer(s_KernelID, "PlayerPixelCounts", s_PlayerPixelCountsBuffer);

            // 分派Compute Shader
            int threadGroupsX = Mathf.CeilToInt(rtWidth / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(rtHeight / 8.0f);
            s_PlayerDetectionShader.Dispatch(s_KernelID, threadGroupsX, threadGroupsY, 1);

            s_PlayerPixelCountsBuffer.GetData(playerPixelCounts2);

            s_PlayerPixelCountsBuffer.Release();
            RenderTexture.ReleaseTemporary(renderTexture);

            // 处理检测结果
            List<VisiblePlayer> visiblePlayers2 = new List<VisiblePlayer>();
            for (int i = 0; i < playerPixelCounts2.Length; i++)
            {
                if (playerPixelCounts2[i] > 0)
                {
                    float ratio = playerPixelCounts2[i] / (float)(rtWidth * rtHeight);
                    if (ratio > 0)
                    {
                        visiblePlayers2.Add(new VisiblePlayer { PlayerObject = players[i], ScreenSpaceRatio = ratio });
                    }
                }
            }

            // 构建最终结果
            visionResult.VisiblePlayers = visiblePlayers2.ToArray();
            visionResult.VisibleCount = visiblePlayers2.Count;

            return visionResult.VisibleCount > 0;
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
        RenderTexture.ReleaseTemporary(renderTexture);
        monsterCamera.targetTexture = null;

        var visiblePlayers = new List<VisiblePlayer>();
        int totalPixels = rtWidth * rtHeight;
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