using System;
using UnityEngine;
using UnityEngine.Rendering;

public class Tutorial1 : MonoBehaviour
{
    Texture2D texture;
    Color[] background;

    // Awake is called when the game object is initialised
    // This is perhaps not the best way to setup a full-screen quad but it is sufficient for our purposes
    private void Awake()
    {
        // set up the camera to look perfectly at the quad (which we are about to make)
        Camera.main.orthographic = true;
        Camera.main.orthographicSize = 360;
        Camera.main.rect = new Rect(0, 0, 1, 1);

        // create and postion a quad
        GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
        surface.transform.localPosition = new Vector3(0, 0, 0);
        surface.transform.localRotation = Quaternion.identity;
        surface.transform.localScale = new Vector3(1280, 720, 1);

        // create a texture to display on the quad (that we can also draw into directly)
        texture = new Texture2D(1280, 720);
        texture.filterMode = FilterMode.Point;
        surface.GetComponent<Renderer>().material.shader = Shader.Find("Unlit/Texture");
        surface.GetComponent<Renderer>().material.mainTexture = texture;
        background = texture.GetPixels();
    }

    // Start is called before the first frame update
    void Start()
    {
        // ******************************************************
        // Write your Tutorial1 code here

        texture.DrawDDALine(100, 100, 500, 300, Color.black);

        // ******************************************************

        // this needs to happen _after_ your code
        texture.Apply(); // DO NOT MOVE OR DELETE
    }

    // Update is called once per frame
    void Update()
    {
        // nothing to update
    }
}

// in C# you can add methods to existing classes: https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods
public static class Texture2DExtensionsPrimitives
{
    // incomplete line drawing function, only handles a limited number of directions and line slopes
    public static void DrawDDALine(this Texture2D texture, int x1, int y1, int x2, int y2, Color c)
    {
        int deltay = (y2 - y1);
        int deltax = (x2 - x1);
        float slope = (float)deltay / deltax;
        float y = y1;
        for (int x = x1; x <= x2; x++)
        {
            texture.SetPixel(x, Mathf.RoundToInt(y), c);
            y = y + slope;
        }
    }
}
