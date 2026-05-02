using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MapperMate
{
    [System.Serializable]
    public class TilePlacement
    {
        public int x;
        public int y;
        public int symbol;
    }

    [System.Serializable]
    public class LayerData
    {
        public string name;
        public TilePlacement[] tiles;
    }

    [System.Serializable]
    public class MapData
    {
        public string tilesetName;
        public int tileSize;
        public int tilesetCols;
        public int tilesetRows;
        public LayerData[] layers;
    }

    public class MapperMateLoader : MonoBehaviour
    {
        [Header("Data")]
        public TextAsset mapDataJson;
        public Texture2D tilesetTexture;

        [Header("Generated")]
        public Grid grid;

        private Dictionary<int, Tile> tileCache = new Dictionary<int, Tile>();

#if UNITY_EDITOR
        [ContextMenu("Load Map")]
        public void LoadMap()
        {
            if (mapDataJson == null || tilesetTexture == null)
            {
                Debug.LogError("MapperMate: Please assign mapDataJson and tilesetTexture!");
                return;
            }

            MapData mapData = JsonUtility.FromJson<MapData>(mapDataJson.text);

            // Create or get grid
            if (grid == null)
            {
                grid = GetComponent<Grid>();
                if (grid == null)
                {
                    grid = gameObject.AddComponent<Grid>();
                }
            }

            // Clear existing tilemaps
            foreach (Transform child in transform)
            {
                DestroyImmediate(child.gameObject);
            }
            tileCache.Clear();

            // Create tiles from sprites
            string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/MapperMate/Resources" });
            Sprite[] sprites = new Sprite[mapData.tilesetCols * mapData.tilesetRows];

            foreach (string guid in spriteGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (Object asset in assets)
                {
                    if (asset is Sprite sprite && sprite.texture == tilesetTexture)
                    {
                        // Extract symbol from sprite name (e.g., "tileset_1" -> 1)
                        string name = sprite.name;
                        int lastUnderscore = name.LastIndexOf('_');
                        if (lastUnderscore >= 0 && int.TryParse(name.Substring(lastUnderscore + 1), out int symbol))
                        {
                            if (symbol > 0 && symbol <= sprites.Length)
                            {
                                sprites[symbol - 1] = sprite;
                            }
                        }
                    }
                }
            }

            // Also try loading sprites directly from texture
            string texturePath = AssetDatabase.GetAssetPath(tilesetTexture);
            Object[] textureAssets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
            foreach (Object asset in textureAssets)
            {
                if (asset is Sprite sprite)
                {
                    string name = sprite.name;
                    int lastUnderscore = name.LastIndexOf('_');
                    if (lastUnderscore >= 0 && int.TryParse(name.Substring(lastUnderscore + 1), out int symbol))
                    {
                        if (symbol > 0 && symbol <= sprites.Length)
                        {
                            sprites[symbol - 1] = sprite;
                        }
                    }
                }
            }

            // Create each layer
            foreach (LayerData layerData in mapData.layers)
            {
                GameObject layerObj = new GameObject(layerData.name);
                layerObj.transform.SetParent(transform);
                layerObj.transform.localPosition = Vector3.zero;

                Tilemap tilemap = layerObj.AddComponent<Tilemap>();
                TilemapRenderer renderer = layerObj.AddComponent<TilemapRenderer>();

                foreach (TilePlacement placement in layerData.tiles)
                {
                    if (placement.symbol <= 0 || placement.symbol > sprites.Length)
                        continue;

                    Sprite sprite = sprites[placement.symbol - 1];
                    if (sprite == null)
                        continue;

                    if (!tileCache.TryGetValue(placement.symbol, out Tile tile))
                    {
                        tile = ScriptableObject.CreateInstance<Tile>();
                        tile.sprite = sprite;
                        tile.color = Color.white;
                        tileCache[placement.symbol] = tile;
                    }

                    tilemap.SetTile(new Vector3Int(placement.x, placement.y, 0), tile);
                }

                tilemap.RefreshAllTiles();
            }

            Debug.Log("MapperMate: Map loaded successfully!");
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MapperMateLoader))]
    public class MapperMateLoaderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MapperMateLoader loader = (MapperMateLoader)target;

            EditorGUILayout.Space();

            if (GUILayout.Button("Load Map", GUILayout.Height(40)))
            {
                loader.LoadMap();
            }
        }
    }
#endif
}
