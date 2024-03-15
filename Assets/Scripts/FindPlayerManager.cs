using System.Collections.Generic;
using UnityEngine;

public class FindPlayerManager : MonoBehaviour
{
    public Camera detectionCamera;

    private static readonly int PropIdColor = Shader.PropertyToID("_FindColor");
    private static readonly Color PlayerColor = new Color(1, 0, 0, 1);
    private static readonly Color NonPlayerColor = new Color(0, 0, 0, 1);

    private MaterialPropertyBlock propertyBlock;

    // 定义一组预设颜色
    Color[] presetColors = new Color[]
    {
    new Color(1, 0, 1, 1), // 紫色
    new Color(0, 1, 1, 1), // 青色
    new Color(1, 1, 0, 1), // 黄色
                           // ... 添加更多不常用的纯色
    };


    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            //DetectPlayers();


            List<GameObject> players = new List<GameObject>();
            FindePlayers(players);
            FindResult findResult;
            bool findme = DetectPlayers(players, detectionCamera, out findResult);
            if (findme)
            {
                Debug.Log("I see you: " + findResult.count);
            }
            else
            {
                Debug.Log("Can not see you");
            }
        }
    }


    //struct Result
    //{

    //    ScreenRatio[] players;
    //    int count;

    //}
    //struct ScreenRatio
    //{
    //    GameObject
    //        float
    //}

    //void Find(List<GameObject> plyaers, ref Camera camera, out Result)
    //{

    //}

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


    struct FindResult
    {
        public FindPlayer[] findPlayers;
        public int count;
    }

    struct FindPlayer
    {
        public GameObject playerGo;
        public float ratio;
    }

    private bool DetectPlayers(List<GameObject> players, Camera camera, out FindResult findResult)
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
        camera.backgroundColor = NonPlayerColor;

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
        //RenderTexture.ReleaseTemporary(renderTexture);
        //camera.targetTexture = null;
        Destroy(texture);

        // Prepare the results
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


    private void DetectPlayers()
    {
        int textureHeight = 256;
        int textureWidth = Mathf.RoundToInt(textureHeight * detectionCamera.aspect);
        RenderTexture renderTexture = RenderTexture.GetTemporary(textureWidth, textureHeight, 32, RenderTextureFormat.ARGB32);

        detectionCamera.targetTexture = renderTexture;
        detectionCamera.clearFlags = CameraClearFlags.SolidColor;
        detectionCamera.backgroundColor = NonPlayerColor;

        Renderer[] renderers = FindObjectsOfType<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            PaintDetectColor(renderer, renderer.gameObject.layer == LayerMask.NameToLayer("Player") ? PlayerColor : NonPlayerColor);
        }

        detectionCamera.Render();

        RenderTexture.active = renderTexture;
        bool playerDetected = CheckForPlayerColor(textureWidth, textureHeight);

        if (playerDetected)
        {
            Debug.Log("I see you");
        }

        RenderTexture.active = null;
        //RenderTexture.ReleaseTemporary(renderTexture);
        //detectionCamera.targetTexture = null;
    }

    private void PaintDetectColor(Renderer renderer, Color color)
    {
        propertyBlock.SetColor(PropIdColor, color);
        renderer.SetPropertyBlock(propertyBlock);
    }

    private bool CheckForPlayerColor(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();

        Color[] pixels = texture.GetPixels();
        foreach (var pixel in pixels)
        {
            if (pixel == PlayerColor)
            {
                Destroy(texture);
                return true;
            }
        }

        Destroy(texture);
        return false;
    }
}