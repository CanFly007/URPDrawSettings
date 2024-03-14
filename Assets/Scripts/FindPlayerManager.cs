using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FindPlayerManager : MonoBehaviour
{
    public Camera camera;

    private readonly int m_propIdColor = Shader.PropertyToID("_FindColor");
    private Color playerColor = new Color(1, 0, 0, 1);
    private  Color otherColor = new Color(0, 0, 0, 1);

    void Sample()
    {
        int textureHeight = 256;
        float cameraAspectRatio = camera.aspect;
        int textureWidth = Mathf.RoundToInt(textureHeight * cameraAspectRatio);


        RenderTexture rt = RenderTexture.GetTemporary(textureWidth, textureHeight, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        camera.targetTexture = rt;
        rt.filterMode = FilterMode.Point;
        rt.Create();
        RenderTexture.active = rt;

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.green;

        var renderers = FindObjectsOfType<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            CreateBakingRenderer(renderers[i]);
        }

        camera.Render();

        Texture2D readbackTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false);
        readbackTexture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
        readbackTexture.Apply();
        Color[] pixels = readbackTexture.GetPixels();
        Object.Destroy(readbackTexture);
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        int count = pixels.Length;
        for (int i = 0; i < count; i++)
        {
            Color pixel = pixels[i];
            if (pixel.r == 1 && pixel.g == 0 && pixel.b == 0 && pixel.a == 1)
            {
                Debug.Log("i see you");
                break;
            }
        }

    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            //Setup();
            Sample();


        }
    }

    void CreateBakingRenderer(Renderer renderer)
    {
        if (renderer.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Material[] allMaterials = renderer.sharedMaterials;
            for (int i = 0; i < allMaterials.Length; i++)
            {
                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                mpb.SetColor(m_propIdColor, playerColor);
                renderer.SetPropertyBlock(mpb, i);
            }
        }
        else
        {
            Material[] allMaterials = renderer.sharedMaterials;
            for (int i = 0; i < allMaterials.Length; i++)
            {
                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                mpb.SetColor(m_propIdColor, otherColor);
                renderer.SetPropertyBlock(mpb, i);
            }
        }
    }
}
