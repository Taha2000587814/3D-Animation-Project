using UnityEngine;

// in C# you can add methods to existing classes: https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods
public static class Texture2DExtensionsPixels
{
    public static void SetPixelQuadrants(this Texture2D texture, int x, int y, int offsetx, int offsety, Color c)
    {
        texture.SetPixel(x + offsetx, y + offsety, c);
        texture.SetPixel(x - offsetx, y + offsety, c);
        texture.SetPixel(x + offsetx, y - offsety, c);
        texture.SetPixel(x - offsetx, y - offsety, c);
    }

    public static void SetPixelOctants(this Texture2D texture, int x, int y, int offsetx, int offsety, Color c)
    {
        texture.SetPixel(x + offsetx, y + offsety, c);
        texture.SetPixel(x - offsetx, y + offsety, c);
        texture.SetPixel(x + offsetx, y - offsety, c);
        texture.SetPixel(x - offsetx, y - offsety, c);

        texture.SetPixel(x + offsety, y + offsetx, c);
        texture.SetPixel(x - offsety, y + offsetx, c);
        texture.SetPixel(x + offsety, y - offsetx, c);
        texture.SetPixel(x - offsety, y - offsetx, c);
    }
}
