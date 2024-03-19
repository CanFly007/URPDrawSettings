using System.Collections.Generic;
using UnityEngine;

namespace YahahaGraphics
{
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

    public class MonsterVisionProcessor
    {
        private static readonly int s_ShaderPropertyColor = Shader.PropertyToID("_PlayerColor");
        private static MaterialPropertyBlock s_MaterialPropertyBlock = new MaterialPropertyBlock();

        private static bool s_UseGPU = false;
        private static ComputeShader s_PlayerDetectionShader;
        private static int s_KernelID;
        private static ComputeBuffer s_PlayerPixelCountsBuffer;

        public static void Initialize(ComputeShader playerDetectionShader, bool useGPU = false)
        {
            s_PlayerDetectionShader = playerDetectionShader;
            s_KernelID = s_PlayerDetectionShader.FindKernel("CSMain");
            s_UseGPU = useGPU;
        }

        public static void Cleanup()
        {
            if (s_PlayerPixelCountsBuffer != null)
            {
                s_PlayerPixelCountsBuffer.Release();
                s_PlayerPixelCountsBuffer = null;
            }
        }

        private static Color32[] s_DetectionColors = new Color32[]
        {
            new Color32(255, 0, 0, 255),
            new Color32(0, 255, 0, 255),
            new Color32(0, 0, 255, 255),
            new Color32(255, 255, 0, 255),
            new Color32(255, 0, 255, 255),
            new Color32(0, 255, 255, 255),
            new Color32(128, 0, 0, 255),
            new Color32(0, 128, 0, 255),
            new Color32(0, 0, 128, 255),
            new Color32(128, 128, 0, 255),
            new Color32(128, 0, 128, 255),
            new Color32(0, 128, 128, 255),
        };

        public static bool TryDetectPlayers(List<GameObject> players, Camera monsterCamera, out VisionResult visionResult)
        {
            int maxPlayers = s_DetectionColors.Length;
            if (players.Count > s_DetectionColors.Length)
            {
                Debug.LogWarning("MonsterVisionProcessor: Insufficient detection colors for the number of players.");
            }
            int numPlayersToProcess = Mathf.Min(players.Count, maxPlayers);
            List<GameObject> playersToProcess = players.GetRange(0, numPlayersToProcess);

            int rtHeight = 256;
            int rtWidth = Mathf.RoundToInt(rtHeight * monsterCamera.aspect);
            RenderTexture renderTexture = RenderTexture.GetTemporary(rtWidth, rtHeight, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();

            monsterCamera.targetTexture = renderTexture;
            monsterCamera.clearFlags = CameraClearFlags.SolidColor;
            monsterCamera.backgroundColor = Color.black;

            Graphics.SetRenderTarget(renderTexture);
            GL.Clear(true, true, Color.black);

            var colorPlayerMap = new Dictionary<Color32, GameObject>();
            for (int i = 0; i < numPlayersToProcess; ++i)
            {
                Color32 detectionColor = s_DetectionColors[i];
                colorPlayerMap[detectionColor] = playersToProcess[i];

                Renderer[] playerRenderers = playersToProcess[i].GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in playerRenderers)
                {
                    s_MaterialPropertyBlock.SetColor(s_ShaderPropertyColor, detectionColor);
                    renderer.SetPropertyBlock(s_MaterialPropertyBlock);
                }
            }

            monsterCamera.targetTexture = renderTexture;
            monsterCamera.Render();

            visionResult = new VisionResult();
            List<VisiblePlayer> visiblePlayers = new List<VisiblePlayer>();
            int totalPixels = rtWidth * rtHeight;

            if (s_UseGPU && s_PlayerDetectionShader != null)
            {
                s_PlayerDetectionShader.SetTexture(s_KernelID, "InputTexture", renderTexture);

                ComputeBuffer s_PlayerPixelCountsBuffer = new ComputeBuffer(numPlayersToProcess, sizeof(int));
                int[] playerPixelCounts = new int[numPlayersToProcess];
                s_PlayerPixelCountsBuffer.SetData(playerPixelCounts);

                s_PlayerDetectionShader.SetBuffer(s_KernelID, "PlayerPixelCounts", s_PlayerPixelCountsBuffer);
                int threadGroupsX = Mathf.CeilToInt(rtWidth / 8.0f);
                int threadGroupsY = Mathf.CeilToInt(rtHeight / 8.0f);
                s_PlayerDetectionShader.Dispatch(s_KernelID, threadGroupsX, threadGroupsY, 1);

                s_PlayerPixelCountsBuffer.GetData(playerPixelCounts);

                s_PlayerPixelCountsBuffer.Release();

                for (int i = 0; i < playerPixelCounts.Length; i++)
                {
                    if (playerPixelCounts[i] > 0)
                    {
                        float ratio = playerPixelCounts[i] / (float)totalPixels;
                        if (ratio > 0)
                        {
                            visiblePlayers.Add(new VisiblePlayer { PlayerObject = playersToProcess[i], ScreenSpaceRatio = ratio });
                        }
                    }
                }
            }
            else
            {
                RenderTexture.active = renderTexture;
                Texture2D texture = new Texture2D(rtWidth, rtHeight, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, rtWidth, rtHeight), 0, 0);
                texture.Apply();

                var playerPixelCounts = new Dictionary<GameObject, int>();
                Color[] pixels = texture.GetPixels();
                for (int i = 0; i < pixels.Length; ++i)
                {
                    Color32 pixel = pixels[i];
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

                foreach (var kvp in playerPixelCounts)
                {
                    float ratio = kvp.Value / (float)totalPixels;
                    if (ratio > 0)
                    {
                        visiblePlayers.Add(new VisiblePlayer { PlayerObject = kvp.Key, ScreenSpaceRatio = ratio });
                    }
                }
            }

            visionResult.VisiblePlayers = visiblePlayers.ToArray();
            visionResult.VisibleCount = visiblePlayers.Count;

            monsterCamera.targetTexture = null;
            RenderTexture.ReleaseTemporary(renderTexture);
            return visionResult.VisibleCount > 0;
        }
    }
}