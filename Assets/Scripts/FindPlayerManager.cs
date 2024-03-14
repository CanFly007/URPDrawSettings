using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FindPlayerManager : MonoBehaviour
{
    public Camera camera;
    private RenderTexture outputRT;

    private readonly int m_propIdColor = Shader.PropertyToID("_FindColor");
    private Color32 playerColor = new Color32((byte)255, 0, 0, 1);
    private  Color32 otherColor = new Color32(0, 0, 0, 1);

    void Setup()
    {
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;

        outputRT = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        outputRT.autoGenerateMips = false;
        outputRT.enableRandomWrite = true;
        outputRT.Create();
    }

    void Sample()
    {
        RenderTexture rt = RenderTexture.GetTemporary(256, 256, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
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

        Texture2D readbackTexture = new Texture2D(256, 256, TextureFormat.ARGB32, false);
        readbackTexture.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
        readbackTexture.Apply();
        Color32[] pixels = readbackTexture.GetPixels32();
        Object.Destroy(readbackTexture);
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        int count = pixels.Length;
        for (int i = 0; i < count; i++)
        {
            Color32 pixel = pixels[i];
            if (pixel.r == 255 && pixel.g == 0 && pixel.b == 0 && pixel.a == 255)
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
