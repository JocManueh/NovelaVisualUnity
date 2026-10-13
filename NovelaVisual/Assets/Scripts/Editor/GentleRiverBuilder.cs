using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CaliNature.Editor
{
    /// Construye un escenario basico con rio para el capitulo de inundacion,
    /// usando el tileset "Gentle Sheets" (Ansimuz).
    ///
    /// TODO EL CONTENIDO SE GENERA DESDE CODIGO. No hay que pintar nada a mano.
    ///
    /// ESCALA
    ///   El tileset usa tiles de 16x16 px. Se importa a 32 px/unidad, de modo que
    ///   1 tile = 0.5 unidades, que es exactamente el paso del jugador
    ///   (PlayerGridMovement.cellSize). Cada casilla dibujada es un paso posible.
    ///
    /// CAPAS (de fondo hacia frente)
    ///   Suelo      orden -20   hierba, tierra y caminos   (sin colision)
    ///   Agua       orden -10   rio y orillas             (CON colision: no se entra)
    ///   Decoracion orden  -5   arbustos                  (sin colision)
    ///
    /// LA REJILLA SE GENERA EN BuildLayout: un rio vertical en las columnas 26-28,
    /// orillas en 25 y 29, y un puente en las filas 13-14 para poder cruzarlo.
    public static class GentleRiverBuilder
    {
        const string TexturePath = "Assets/gentle sheets/gentle forest v01.png";
        const string TreeTexturePath = "Assets/gentle sheets/gentle trees 80x96 v01.png";
        const string TilesFolder = "Assets/Tiles/Gentle";
        const string SpriteFolder = "Assets/Tiles/Gentle/Sprites";
        const string SceneName = "05_RiverFloodChapter";

        const int TilesPerRow = 16;      // el PNG es 256x256 con tiles de 16px
        const float PixelsPerUnit = 32f;
        // Los árboles miden 80x96 px. A 32 px/unidad salrían de 2.5x3 unidades, un tercio
        // del alto del mapa, y taparían el río. A 64 ocupan 1.25x1.5: tamaño de árbol.
        const float TreePixelsPerUnit = 64f; // 16px -> 0.5 unidades
        const float CellSize = 0.5f;

        // ── Índices de tile dentro del tileset ──────────────────────────────
        // Elegidos midiendo el color medio de cada tile del PNG, no a ojo:
        //   pasto limpio -> verde dominante
        //   camino       -> marrón dominante
        // Antes se usaban 33/34 como pasto y son marrón: el mapa salía todo tierra.
        static readonly int[] GrassFill = { 0, 3, 48, 51 };
        static readonly int[] DirtFill = { 17, 18 };
        static readonly int[] WaterFill = { 145, 146, 161, 164 };
        static readonly int[] BankWest = { 162, 163, 165 }; // tierra a la izquierda
        static readonly int[] BankEast = { 147, 148, 149 }; // tierra a la derecha
        static readonly int[] BankNorth = { 178, 179 };    // tierra arriba
        static readonly int[] BankSouth = { 155, 160 };    // tierra abajo
        static readonly int[] Bushes = { 8, 9, 12, 13, 24, 26 };
        static readonly int[] Rocks = { 59, 62, 63 };

        // ── Geometria del escenario (en casillas) ───────────────────────────
        const int MapW = 40, MapH = 30;
        const int RiverX0 = 26, RiverX1 = 28;      // columnas del cauce
        const int BridgeY0 = 13, BridgeY1 = 14;    // filas del puente

        [MenuItem("Cali/Construir escenario del rio (Gentle)")]
        public static void Build()
        {
            if (!EditorSceneManagerPrompt())
            {
                Debug.LogError("Cali: cancelado, no habia ninguna escena abierta.");
                return;
            }

            ConfigureTexture();
            var byIndex = LoadSpritesByTileIndex();
            Debug.Log("Cali: sub-sprites indexados -> " + byIndex.Count);
            if (byIndex.Count == 0) { Debug.LogError("Cali: la textura no genero sub-sprites. Revisar la importacion."); return; }

            var tiles = new Dictionary<string, TileBase[]>();
            tiles["grass"] = MakeTileSet(byIndex, "Grass", GrassFill, "G");
            tiles["dirt"] = MakeTileSet(byIndex, "Dirt", DirtFill, "D");
            tiles["water"] = MakeTileSet(byIndex, "Water", WaterFill, "W");
            tiles["bankW"] = MakeTileSet(byIndex, "BankWest", BankWest, "BW");
            tiles["bankE"] = MakeTileSet(byIndex, "BankEast", BankEast, "BE");
            tiles["bankN"] = MakeTileSet(byIndex, "BankNorth", BankNorth, "BN");
            tiles["bankS"] = MakeTileSet(byIndex, "BankSouth", BankSouth, "BS");
            tiles["bush"] = MakeTileSet(byIndex, "Bush", Bushes, "B");
            tiles["rock"] = MakeTileSet(byIndex, "Rock", Rocks, "R");

            HideOldScene();

            var root = NewRoot("GentleMap");
            BuildGround(root, tiles);
            BuildWater(root, tiles);
            BuildDecor(root, tiles);
            BuildTrees(root);
            Debug.Log("Cali: capas creadas -> " + root.transform.childCount);

            PlaceActors();

            EditorSceneManagerMarkDirty();
            Debug.Log("Cali: escenario del rio construido. Rejilla " + MapW + "x" + MapH
                + " casillas (" + (MapW * CellSize) + "x" + (MapH * CellSize) + " unidades).");
        }

        // ── Importacion del PNG ─────────────────────────────────────────────
        static void ConfigureTexture()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            if (!importer) { Debug.LogError("Cali: no se encontro " + TexturePath); return; }
            importer.textureType = TextureImporterType.Sprite;
            // Single a proposito: los tiles los crea LoadSpritesByTileIndex con
            // Sprite.Create, porque Unity 6 no expone el corte por rejilla a codigo.
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;          // pixel art: sin blurring
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            // Anclaje abajo-izquierda: el tile se apoya justo en la esquina de su casilla.
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomLeft;
            settings.spritePivot = Vector2.zero;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        // Los árboles vienen en su propia hoja: 2 tiles de 80x96 px (160x96 en total).
        // Van en otra capa porque son mucho más altos que una casilla y deben quedar
        // por detrás del jugador sin bloquear el paso.
        static Sprite[] LoadTreeSprites()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TreeTexturePath);
            if (!texture) { Debug.LogWarning("Cali: no se encontro " + TreeTexturePath); return new Sprite[0]; }
            Directory.CreateDirectory(SpriteFolder);

            var list = new List<Sprite>();
            for (int i = 0; i < 2; i++)
            {
                string path = SpriteFolder + "/Tree" + i + ".asset";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (!sprite)
                {
                    sprite = Sprite.Create(texture,
                        new Rect(i * 80f, 0f, 80f, 96f),
                        new Vector2(0.5f, 0.5f),
                        TreePixelsPerUnit, 0, SpriteMeshType.FullRect);
                    sprite.name = "Tree" + i;
                    AssetDatabase.CreateAsset(sprite, path);   // persistido, igual que el bosque
                }
                list.Add(sprite);
            }
            AssetDatabase.SaveAssets();
            return list.ToArray();
        }

        // Localiza (o crea) los sprites de tile y los PERSISTE como assets propios.
        //
        // POR QUÉ IMPORTA: un sprite creado con Sprite.Create solo existe en memoria.
        // Si se asigna a un Tile y se guarda la escena, la referencia queda apuntando a
        // un objeto sin GUID: al recargar el dominio (compilar, correr tests, abrir el
        // proyecto) el sprite desaparece y los Tile quedan con la referencia en null.
        // Eso era exactamente lo que hacía que el mapa no se dibujara en la ventana.
        //
        // Se guardan como archivos .asset individuales y NO como sub-assets del PNG:
        // añadirlos al PNG obliga a reimportar la textura en el mismo momento y deja el
        // asset en error ("Unknown error occurred while loading").
        static Dictionary<int, Sprite> LoadSpritesByTileIndex()
        {
            var result = new Dictionary<int, Sprite>();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (!texture) { Debug.LogError("Cali: no se pudo cargar " + TexturePath); return result; }
            Directory.CreateDirectory(SpriteFolder);

            int rows = Mathf.RoundToInt(texture.height / 16f);
            float texelHeight = texture.height / (float)rows;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < TilesPerRow; col++)
                {
                    int index = row * TilesPerRow + col;
                    string path = SpriteFolder + "/G" + index + ".asset";

                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (!sprite)
                    {
                        // Unity mide desde abajo; el tileset se numera desde arriba.
                        float y = texture.height - (row + 1) * texelHeight;
                        sprite = Sprite.Create(texture,
                            new Rect(col * 16f, y, 16f, 16f),
                            new Vector2(0.5f, 0.5f),   // centrado en su casilla
                            PixelsPerUnit, 0, SpriteMeshType.FullRect);
                        sprite.name = "G" + index;
                        AssetDatabase.CreateAsset(sprite, path);   // <- persistido
                    }
                    result[index] = sprite;
                }
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        static TileBase[] MakeTileSet(Dictionary<int, Sprite> sprites, string name, int[] indices, string prefix)
        {
            Directory.CreateDirectory(TilesFolder);
            var set = new List<TileBase>();
            for (int i = 0; i < indices.Length; i++)
            {
                if (!sprites.TryGetValue(indices[i], out var sprite))
                {
                    Debug.LogWarning("Cali: el tileset no tiene el tile " + indices[i] + " (" + name + ").");
                    continue;
                }
                string path = TilesFolder + "/" + prefix + name + i + ".asset";
                var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
                if (!tile)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, path);
                }
                ((Tile)tile).sprite = sprite;
                ((Tile)tile).colliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
                set.Add(tile);
            }
            AssetDatabase.SaveAssets();
            return set.ToArray();
        }

        // ── Capas ───────────────────────────────────────────────────────────
        static GameObject NewRoot(string name)
        {
            var existing = GameObject.Find(name);
            if (existing) Object.DestroyImmediate(existing);
            var go = new GameObject(name);
            // El Grid fija el tamaño de casilla de todos los Tilemap hijos.
            // 0.5 unidades = exactamente un paso del jugador.
            var grid = go.AddComponent<Grid>();
            grid.cellSize = new Vector3(CellSize, CellSize, 1f);
            return go;
        }

        static void BuildTrees(GameObject root)
        {
            var sprites = LoadTreeSprites();
            if (sprites.Length == 0) return;
            Directory.CreateDirectory(TilesFolder);
            var tiles = new List<TileBase>();
            for (int i = 0; i < sprites.Length; i++)
            {
                string path = TilesFolder + "/Tree" + i + ".asset";
                var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
                if (!tile) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
                ((Tile)tile).sprite = sprites[i];
                ((Tile)tile).colliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
                tiles.Add(tile);
            }
            AssetDatabase.SaveAssets();

            var map = NewLayer(root.name, "Gentle_Arboles", -4, false); // sin colision: se cruza debajo
            uint seed = 55555u;
            for (int y = 2; y < MapH - 1; y++)
            {
                for (int x = 2; x < MapW - 1; x++)
                {
                    seed = seed * 1664525u + 1013904223u;
                    float r = (seed >> 8) % 1000u / 1000f;
                    if (r > 0.025f) continue;                      // arbolado disperso
                    if (IsRoad(x, y) || IsBridge(x, y)) continue;  // nunca sobre el camino
                    // El cauce y sus orillas se dejan despejados para que el río se lea.
                    if (x >= RiverX0 - 1 && x <= RiverX1 + 1) continue;
                    map.SetTile(new Vector3Int(x, y, 0), tiles[(int)(seed >> 16) % tiles.Count]);
                }
            }
        }

        static Tilemap NewLayer(string root, string layerName, int order, bool collider)
        {
            var go = new GameObject(layerName);
            go.transform.SetParent(GameObject.Find(root).transform, false);
            var map = go.AddComponent<Tilemap>();
            // En Unity 6 cellSize viene del componente Grid padre, no del Tilemap.
            map.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            if (collider) go.AddComponent<TilemapCollider2D>();
            return map;
        }

        static void BuildGround(GameObject root, Dictionary<string, TileBase[]> tiles)
        {
            var map = NewLayer(root.name, "Gentle_Suelo", -20, false);
            var grass = tiles["grass"]; var dirt = tiles["dirt"];
            uint seed = 12345;
            for (int y = 0; y < MapH; y++)
            {
                for (int x = 0; x < MapW; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    bool road = IsRoad(x, y);
                    var set = road ? dirt : grass;
                    if (set.Length == 0) continue;
                    // Barajado determinista: mismo mapa en cada ejecucion.
                    seed = seed * 1664525u + 1013904223u;
                    map.SetTile(cell, set[(int)(seed >> 16) % set.Length]);
                }
            }
        }

        static void BuildWater(GameObject root, Dictionary<string, TileBase[]> tiles)
        {
            var map = NewLayer(root.name, "Gentle_Agua", -10, true); // con colision
            var water = tiles["water"];
            for (int y = 0; y < MapH; y++)
            {
                for (int x = RiverX0; x <= RiverX1; x++)
                {
                    if (IsBridge(x, y)) continue;          // aqui no hay agua: se puede cruzar
                    var cell = new Vector3Int(x, y, 0);
                    var tile = PickBankOrWater(x, y, tiles);
                    if (tile) map.SetTile(cell, tile);
                }
            }
        }

        static TileBase PickBankOrWater(int x, int y, Dictionary<string, TileBase[]> tiles)
        {
            bool west = x == RiverX0, east = x == RiverX1;
            bool north = y == 0, south = y == MapH - 1;
            if (north) return First(tiles["bankN"]);
            if (south) return First(tiles["bankS"]);
            if (west) return First(tiles["bankW"]);      // orilla izquierda
            if (east) return First(tiles["bankE"]);      // orilla derecha
            return First(tiles["water"]);
        }

        static TileBase First(TileBase[] set) => set != null && set.Length > 0 ? set[0] : null;

        static void BuildDecor(GameObject root, Dictionary<string, TileBase[]> tiles)
        {
            var map = NewLayer(root.name, "Gentle_Decoracion", -5, false); // sin colision
            var bush = tiles["bush"]; var rock = tiles["rock"];
            uint seed = 987654321u;
            for (int y = 1; y < MapH - 1; y++)
            {
                for (int x = 1; x < MapW - 1; x++)
                {
                    seed = seed * 1664525u + 1013904223u;
                    float r = (seed >> 8) % 1000u / 1000f;
                    if (r > 0.11f) continue;               // densidad ~11%
                    if (IsRoad(x, y) || IsBridge(x, y)) continue;
                    bool inRiver = x > RiverX0 - 2 && x < RiverX1 + 2;
                    var set = inRiver ? rock : bush;
                    if (set.Length == 0) continue;
                    map.SetTile(new Vector3Int(x, y, 0), set[(int)(seed >> 16) % set.Length]);
                }
            }
        }

        // ── Trazado ─────────────────────────────────────────────────────────
        static bool IsRoad(int x, int y)
        {
            // Carretera horizontal que cruza el puente.
            if (y >= BridgeY0 && y <= BridgeY1 && x >= 2 && x <= MapW - 3) return true;
            // Camino vertical principal.
            if (x >= 9 && x <= 10 && y >= 4 && y <= 25) return true;
            //.Sendero corto hacia la orilla este.
            if (x >= 18 && x <= 23 && y >= 9 && y <= 10) return true;
            return false;
        }

        static bool IsBridge(int x, int y)
            => y >= BridgeY0 && y <= BridgeY1 && x >= RiverX0 && x <= RiverX1;

        // ── Reposicion de los actores ───────────────────────────────────────
        // Las posiciones antiguas quedaban en un terreno que ya no existe.
        // Se colocan sobre hierba transitable, cerca de sus rutas.
        static void PlaceActors()
        {
            Put("MainEntry", 4, 26);
            Put("NPC_Lucia", 8, 21);
            Put("NPC_Mateo", 8, 6);
            Put("NPC_Ines", 20, 22);
            Put("Pista_1", 15, 17);
            Put("Pista_2", 6, 12);
            Put("Pista_3", 33, 20);   // al otro lado del rio: obliga a usar el puente
        }

        static void Put(string name, int cx, int cy)
        {
            // GameObject.Find NO encuentra objetos inactivos. Las pistas cuelgan de
            // "SCENE", que HideOldScene acaba de desactivar, así que hay que buscarlas
            // entre todos los objetos cargados.
            var go = GameObject.Find(name);
            if (!go)
            {
                foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
                    if (candidate.name == name && candidate.scene.IsValid()) { go = candidate; break; }
            }
            if (!go) { Debug.LogWarning("Cali: no se encontro " + name + " en la escena."); return; }

            // Si sigue colgando de la jerarquía desactivada hay que sacarlo: si no,
            // el jugador no podría verlo ni alcanzarlo.
            if (!go.activeInHierarchy)
            {
                go.transform.SetParent(null, true);
                go.SetActive(true);
                Debug.Log("Cali: " + name + " estaba bajo la jerarquía desactivada; se movió a la raíz.");
            }
            go.transform.position = new Vector3(cx * CellSize, cy * CellSize, 0f);
        }

        // ── Apaga el terreno anterior ────────────────────────────────────────
        // No se borra: se desactiva para poder revertirlo desde el Inspector.
        static void HideOldScene()
        {
            var old = GameObject.Find("SCENE");
            if (old) { old.SetActive(false); Debug.Log("Cali: terreno anterior (SCENE) desactivado."); }
        }

        // ── Utilidades de escena ────────────────────────────────────────────
        static bool EditorSceneManagerPrompt()
            => UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name == SceneName;

        static void EditorSceneManagerMarkDirty()
            => UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                   UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}