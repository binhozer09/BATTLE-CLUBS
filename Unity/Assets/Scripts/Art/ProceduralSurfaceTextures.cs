using UnityEngine;

namespace BattleClubs.Art
{
    /// <summary>
    /// Generates reusable, tileable surface maps for the first art-production pass.
    /// These are authored in code so the project has original materials without
    /// depending on unlicensed texture downloads.
    /// </summary>
    public static class ProceduralSurfaceTextures
    {
        public enum Surface { Plaster, Brick, Concrete, Asphalt, Court, Wood }

        private static Texture2D plaster, brick, concrete, asphalt, court, wood;

        public static Texture2D Get(Surface surface)
        {
            switch (surface)
            {
                case Surface.Brick: return brick ?? (brick = BuildBrick());
                case Surface.Concrete: return concrete ?? (concrete = BuildNoise("Concrete", 256, 256, 0.48f, 0.13f, 0.055f));
                case Surface.Asphalt: return asphalt ?? (asphalt = BuildNoise("Asphalt", 256, 256, 0.22f, 0.075f, 0.09f));
                case Surface.Court: return court ?? (court = BuildCourt());
                case Surface.Wood: return wood ?? (wood = BuildWood());
                default: return plaster ?? (plaster = BuildNoise("Painted_Plaster", 256, 256, 0.72f, 0.09f, 0.035f));
            }
        }

        private static Texture2D NewTexture(string textureName, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true, true);
            texture.name = textureName;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 4;
            return texture;
        }

        private static Texture2D BuildNoise(string name, int width, int height, float baseTone, float variation, float grain)
        {
            var texture = NewTexture(name, width, height);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)width, v = y / (float)height;
                float broad = Mathf.PerlinNoise(u * 13f + 2.1f, v * 13f + 7.4f) - 0.5f;
                float fine = Mathf.PerlinNoise(u * 91f + 4.7f, v * 91f + 1.8f) - 0.5f;
                float value = Mathf.Clamp01(baseTone + broad * variation + fine * grain);
                byte tone = (byte)Mathf.RoundToInt(value * 255f);
                pixels[y * width + x] = new Color32(tone, tone, tone, 255);
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        private static Texture2D BuildBrick()
        {
            const int size = 256;
            var texture = NewTexture("BC_Original_Running_Bond_Brick", size, size);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int row = y / 32;
                int shiftedX = (x + ((row & 1) == 0 ? 0 : 32)) % size;
                int localX = shiftedX % 64, localY = y % 32;
                bool mortar = localX < 3 || localY < 3;
                float noise = Mathf.PerlinNoise(x * 0.075f, y * 0.075f) * 0.18f
                    + Mathf.PerlinNoise(x * 0.31f + 8f, y * 0.31f) * 0.07f;
                Color color = mortar
                    ? new Color(0.34f, 0.31f, 0.27f)
                    : new Color(0.48f, 0.22f, 0.15f) + Color.white * noise;
                pixels[y * size + x] = color;
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        private static Texture2D BuildWood()
        {
            const int width = 256;
            const int height = 256;
            var texture = NewTexture("BC_Original_Worn_Wood_Planks", width, height);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int plank = y / 64;
                float seam = (y % 64 < 3) ? 0.48f : 1f;
                float grain = Mathf.PerlinNoise(x * 0.035f + plank * 3.7f, y * 0.11f)
                    * 0.18f + Mathf.PerlinNoise(x * 0.19f, y * 0.035f + 9f) * 0.07f;
                float knots = Mathf.PerlinNoise(x * 0.018f + 11f, y * 0.09f + plank) * 0.08f;
                Color baseColor = new Color(0.34f, 0.20f, 0.11f);
                Color color = (baseColor + Color.white * (grain + knots)) * seam;
                pixels[y * width + x] = color;
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        private static Texture2D BuildCourt()
        {
            const int size = 256;
            var texture = NewTexture("BC_Original_Worn_Court_Paint", size, size);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.045f, y * 0.045f) * 0.12f
                    + Mathf.PerlinNoise(x * 0.22f + 3f, y * 0.22f) * 0.035f;
                Color c = new Color(0.13f, 0.29f, 0.20f) + Color.white * n;
                if (x % 128 < 2 || y % 128 < 2) c = new Color(0.78f, 0.75f, 0.64f);
                pixels[y * size + x] = c;
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }
    }
}
