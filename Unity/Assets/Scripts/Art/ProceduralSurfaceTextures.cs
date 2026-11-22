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
        public enum Surface { Plaster, Brick, ConcreteBlock, Concrete, Asphalt, Court, Wood, CorrugatedMetal, Foliage, HillsideDirt, PaintedMetal, Plastic, PVC }

        private static Texture2D plaster, brick, concreteBlock, concrete, asphalt, court, wood, corrugatedMetal, foliage, hillsideDirt, paintedMetal, plastic, pvc;

        public static Texture2D Get(Surface surface)
        {
            // Prefer scanned CC0 PBR albedo maps shipped in Resources/BC_PBR.
            Texture2D scanned = Resources.Load<Texture2D>("BC_PBR/" + surface + "_Color");
            if (scanned != null) return scanned;

            switch (surface)
            {
                case Surface.Brick: return brick ?? (brick = LoadBrickAsset() ?? BuildBrick());
                case Surface.ConcreteBlock: return concreteBlock ?? (concreteBlock = BuildConcreteBlock());
                case Surface.CorrugatedMetal: return corrugatedMetal ?? (corrugatedMetal = BuildCorrugatedMetal());
                case Surface.Foliage: return foliage ?? (foliage = BuildFoliage());
                case Surface.HillsideDirt: return hillsideDirt ?? (hillsideDirt = BuildHillsideDirt());
                case Surface.PaintedMetal: return paintedMetal ?? (paintedMetal = BuildPaintedMetal());
                case Surface.Plastic: return plastic ?? (plastic = BuildPlastic());
                case Surface.PVC: return pvc ?? (pvc = BuildPVC());
                case Surface.Concrete: return concrete ?? (concrete = BuildNoise("Concrete", 256, 256, 0.48f, 0.13f, 0.055f));
                case Surface.Asphalt: return asphalt ?? (asphalt = BuildAsphalt());
                case Surface.Court: return court ?? (court = BuildCourt());
                case Surface.Wood: return wood ?? (wood = BuildWood());
                default: return plaster ?? (plaster = BuildNoise("Painted_Plaster", 256, 256, 0.72f, 0.09f, 0.035f));
            }
        }

        public static Texture2D GetNormal(Surface surface)
        {
            return Resources.Load<Texture2D>("BC_PBR/" + surface + "_NormalGL");
        }

        public static Texture2D GetRoughness(Surface surface)
        {
            return Resources.Load<Texture2D>("BC_PBR/" + surface + "_Roughness");
        }

        private static Texture2D LoadBrickAsset()
        {
            Texture2D imported = Resources.Load<Texture2D>("BC_Textures/BC_Brick_Albedo");
            if (imported == null) return null;
            imported.wrapMode = TextureWrapMode.Repeat;
            imported.filterMode = FilterMode.Bilinear;
            imported.anisoLevel = 4;
            return imported;
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

        // Common Brazilian hollow concrete masonry blocks with recessed mortar joints,
        // chipped edges, pores and subtle cement variation. This is deliberately NOT
        // a red clay-brick pattern.
        private static Texture2D BuildConcreteBlock()
        {
            const int width = 512, height = 512;
            const int blockW = 128, blockH = 64, mortar = 5;
            var texture = NewTexture("BC_Concrete_Hollow_Block_Masonry", width, height);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int row = y / blockH;
                int shiftedX = (x + ((row & 1) == 0 ? 0 : blockW / 2)) % width;
                int lx = shiftedX % blockW, ly = y % blockH;
                bool joint = lx < mortar || ly < mortar;
                float broad = Mathf.PerlinNoise(x * 0.018f + 2.3f, y * 0.018f + 8.1f) - 0.5f;
                float grain = Mathf.PerlinNoise(x * 0.19f + 7.2f, y * 0.21f + 1.4f);
                float pits = Mathf.PerlinNoise(x * 0.43f + 13f, y * 0.39f + 4f);
                float shade = 0.61f + broad * 0.13f + (grain - 0.5f) * 0.10f;
                // A few darker pores and chipped, irregular mortar edges.
                bool pore = !joint && pits > 0.79f && grain < 0.47f;
                Color c = joint
                    ? new Color(0.34f, 0.35f, 0.34f) + Color.white * (broad * 0.08f)
                    : new Color(shade, shade * 1.015f, shade * 1.02f);
                if (pore) c *= 0.68f;
                if (!joint && (lx < mortar + 1 || ly < mortar + 1)) c *= 0.88f;
                pixels[y * width + x] = c;
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        // Galvanized corrugated sheet with alternating raised ribs and oxidation.
        private static Texture2D BuildCorrugatedMetal()
        {
            const int width = 256, height = 256;
            var texture = NewTexture("BC_Worn_Corrugated_Galvanized_Sheet", width, height);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float rib = 0.5f + 0.5f * Mathf.Cos((x / 18f) * Mathf.PI * 2f);
                float rust = Mathf.PerlinNoise(x * 0.035f + 12f, y * 0.028f + 3f);
                float scratches = Mathf.PerlinNoise(x * 0.31f, y * 0.045f + 7f);
                float metal = 0.34f + rib * 0.22f + scratches * 0.07f;
                Color c = new Color(metal * 0.92f, metal, metal * 1.02f);
                if (rust > 0.73f) c = Color.Lerp(c, new Color(0.34f, 0.22f, 0.14f), (rust - 0.73f) * 1.8f);
                pixels[y * width + x] = c;
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



        private static Texture2D BuildAsphalt()
        {
            const int size = 512;
            var texture = NewTexture("BC_Worn_Asphalt_Patches", size, size);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float broad = Mathf.PerlinNoise(x * 0.012f + 4f, y * 0.012f + 8f);
                float grain = Mathf.PerlinNoise(x * 0.24f + 3f, y * 0.27f + 11f);
                float patch = Mathf.PerlinNoise(x * 0.027f + 18f, y * 0.025f + 2f);
                float value = Mathf.Clamp(0.12f + broad * 0.075f + grain * 0.045f + (patch > 0.69f ? 0.035f : 0f), 0.06f, 0.34f);
                bool crack = Mathf.Abs(Mathf.PerlinNoise(x * 0.035f + 2f, y * 0.035f + 7f) -
                    Mathf.PerlinNoise(x * 0.035f + 2.025f, y * 0.035f + 7.025f)) < 0.0011f && grain < 0.57f;
                Color c = new Color(value, value * 1.015f, value * 0.99f);
                if (crack) c *= 0.42f;
                pixels[y * size + x] = c;
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        private static Texture2D BuildHillsideDirt()
        {
            const int size = 256;
            var texture = NewTexture("BC_Hillside_Earth_And_Grass", size, size);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float soil = Mathf.PerlinNoise(x * 0.025f + 7f, y * 0.025f + 3f);
                float grass = Mathf.PerlinNoise(x * 0.055f + 21f, y * 0.055f + 13f);
                float grit = Mathf.PerlinNoise(x * 0.31f, y * 0.29f + 5f);
                Color earth = Color.Lerp(new Color(0.25f, 0.19f, 0.12f), new Color(0.43f, 0.34f, 0.21f), soil);
                Color vegetation = new Color(0.17f, 0.25f, 0.12f);
                Color c = Color.Lerp(earth, vegetation, Mathf.Clamp01((grass - 0.57f) * 1.25f));
                c *= 0.88f + grit * 0.20f;
                pixels[y * size + x] = c;
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        // Varied leaf canopy texture with broad color variation and fine foliage grain.
        private static Texture2D BuildFoliage()
        {
            const int size = 256;
            var texture = NewTexture("BC_Natural_Foliage", size, size);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float broad = Mathf.PerlinNoise(x * 0.018f + 3f, y * 0.018f + 9f);
                float fine = Mathf.PerlinNoise(x * 0.13f + 17f, y * 0.16f + 2f);
                float shade = Mathf.Clamp01(0.20f + broad * 0.30f + fine * 0.12f);
                Color leaf = Color.Lerp(new Color(0.055f, 0.16f, 0.055f),
                    new Color(0.30f, 0.43f, 0.13f), shade);
                pixels[y * size + x] = leaf;
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }


        private static Texture2D BuildPaintedMetal()
        {
            const int size = 256;
            var texture = NewTexture("BC_Painted_Worn_Metal", size, size);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float broad = Mathf.PerlinNoise(x * 0.018f + 6f, y * 0.021f + 12f);
                float scratches = Mathf.PerlinNoise(x * 0.19f + 3f, y * 0.035f + 8f);
                float rust = Mathf.PerlinNoise(x * 0.047f + 15f, y * 0.052f + 1f);
                float value = 0.31f + broad * 0.19f + scratches * 0.08f;
                Color paint = new Color(value * 0.86f, value * 0.91f, value * 0.89f);
                if (rust > 0.77f)
                    paint = Color.Lerp(paint, new Color(0.34f, 0.20f, 0.12f), (rust - 0.77f) * 2.8f);
                pixels[y * size + x] = paint;
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }


        private static Texture2D BuildPlastic()
        {
            const int size = 256;
            var texture = NewTexture("BC_Aged_Blue_Water_Tank_Plastic", size, size);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float broad = Mathf.PerlinNoise(x * 0.022f + 4f, y * 0.025f + 9f);
                float grain = Mathf.PerlinNoise(x * 0.24f + 8f, y * 0.21f + 2f);
                float shade = 0.78f + broad * 0.12f + grain * 0.035f;
                Color c = new Color(0.075f, 0.16f, 0.19f) * shade;
                pixels[y * size + x] = c;
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        // Aged off-white PVC used for exposed drain and service pipes.
        private static Texture2D BuildPVC()
        {
            const int size = 256;
            var texture = NewTexture("BC_Aged_PVC_Pipe", size, size);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dirt = Mathf.PerlinNoise(x * 0.035f + 8f, y * 0.04f + 2f);
                float grain = Mathf.PerlinNoise(x * 0.22f + 3f, y * 0.19f + 11f);
                float shade = 0.76f + dirt * 0.10f + grain * 0.035f;
                Color c = new Color(shade, shade * 0.99f, shade * 0.92f);
                if (y % 64 < 3) c *= 0.82f;
                pixels[y * size + x] = c;
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
