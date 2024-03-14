using UnityEngine;

public class FindPlayerManager : MonoBehaviour
{
    public Camera detectionCamera;

    private static readonly int PropIdColor = Shader.PropertyToID("_FindColor");
    private static readonly Color PlayerColor = new Color(1, 0, 0, 1);
    private static readonly Color NonPlayerColor = new Color(0, 0, 0, 1);

    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            DetectPlayers();
        }
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
            SetRendererColor(renderer, renderer.gameObject.layer == LayerMask.NameToLayer("Player") ? PlayerColor : NonPlayerColor);
        }

        detectionCamera.Render();

        RenderTexture.active = renderTexture;
        bool playerDetected = CheckForPlayerColor(textureWidth, textureHeight);

        if (playerDetected)
        {
            Debug.Log("I see you");
        }

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexture);
        detectionCamera.targetTexture = null;
    }

    private void SetRendererColor(Renderer renderer, Color color)
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