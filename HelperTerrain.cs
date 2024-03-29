using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class HelperTerrain : MonoBehaviour
{
    /*
        Purpose
        =======
        - modify the height of a terrain, under a gameobject (the procedurally made road meshes)
        - modify the texture (alphamap) layer opacities under a game object
    
        Terrain Stuff
        =============
        - heightmap[x, y]  
            x and y limits are the Terrain Resolution > Control Texture Resolution  ie. 512 x 512
            when taking a 3d world coord to 2d heightmap coord
            set heightmap x <==> Y world
            set heightmap y <==> X world
        - heightmap values 
            values are range between 0 and 1 (normalized, percentages of total height possible)
            total height possible is Mesh Resolution > Terrain Height  ie. 50
    */

    public const int layer_ground = 0;
    public const int layer_asphalt = 1;

    #region --- CONVERT ---
    public static Vector3 Convert_HeightMapCoord_to_WorldCoord(int heightmapX, int heightmapZ, Terrain terrain)
    {
        float resolution = terrain.terrainData.heightmapResolution - 1;

        //step 1. normalize 0.0 to 1.0 (heightmap position percentage)
        float normalizedX = heightmapX / resolution;
        float normalizedZ = heightmapZ / resolution;

        //step 2. convert normalized heightmapcoord to terraincoord
        float terrainZ = normalizedX * terrain.terrainData.size.x; //NOTE: weird but !!!important!!!  flip the X,Z
        float terrainX = normalizedZ * terrain.terrainData.size.z;

        //step 3. convert terraincoord to worldcoord (get Y, account for terrain scene position)
        Vector3 XZplane_worldcoord = terrain.GetPosition() + new Vector3(terrainZ, 0f, terrainX); //I purposely flipped the x and z!
        float terrainY = terrain.SampleHeight(XZplane_worldcoord);
        Vector3 worldcoord = terrain.GetPosition() + new Vector3(terrainX, terrainY, terrainZ);

        return worldcoord;
    }
    public static Vector3 Convert_AlphaMapCoord_to_WorldCoord(int alphamapX, int alphamapZ, Terrain terrain)
    {
        float resolution = terrain.terrainData.alphamapResolution - 1;

        //step 1. normalize 0.0 to 1.0 (alphamap position percentage)
        float normalizedX = alphamapX / resolution;
        float normalizedZ = alphamapZ / resolution;

        //step 2. convert normalized alphamapcoord to terraincoord
        float terrainZ = normalizedX * terrain.terrainData.size.x; //NOTE: weird but !!!important!!!  flip the X,Z
        float terrainX = normalizedZ * terrain.terrainData.size.z;

        //step 3. convert terraincoord to worldcoord (get y, account for terrain scene position)
        Vector3 XZplane_worldcoord = terrain.GetPosition() + new Vector3(terrainZ, 0f, terrainX); //I purposely flipped the x and z!
        float terrainY = terrain.SampleHeight(XZplane_worldcoord);
        Vector3 worldcoord = terrain.GetPosition() + new Vector3(terrainX, terrainY, terrainZ);

        return worldcoord;
    }
    public static float Convert_Hit_to_HeightMapValue(Vector3 hitpoint, Terrain terrain)
    {
        //step 1. hitpoint is going to be above the terrain
        float heightaboveterrain = hitpoint.y - terrain.GetPosition().y;

        //step 2. normalize (heighmap values are normalizeed values)
        float normalized_heightaboveterrain = heightaboveterrain / terrain.terrainData.size.y;

        return normalized_heightaboveterrain;
    }
    #endregion

    #region --- MODIFY --
    public static void Modify_TerrainHeight_to_GameObject(string gameobjectName, Terrain terrain)
    {
        //Note:
        //  - heightmap values 0 to 1 (they are normalized percentages of total height possible)
        //  - heightmapResolution is the dimension of the heightmap coords (always rect)
        //  - set heightmap x <=> Y world coord  Weird!?! it's rotated!!!
        //  - set heightmap y <=> X world coord
        //  - you cannot raycast through terrain
        //  - raycast hits are in order of distance from ray origin
        //  - raycast colliders only with rendered side (think quads)

        int resolution = terrain.terrainData.heightmapResolution - 1;
        float rayOriginAboveTerrain = terrain.terrainData.size.y;

        //step 1. get the current heightmap
        float[,] heightmap = terrain.terrainData.GetHeights(0, 0, resolution, resolution);

        //step 2. loop, scan, change height
        for (int heightX = 0; heightX < resolution; heightX++)
        {
            for (int heightZ = 0; heightZ < resolution; heightZ++)
            {
                //step 2a - need worldcoord to raycast
                Vector3 worldcoord = Convert_HeightMapCoord_to_WorldCoord(heightX, heightZ, terrain);

                //step 2b - raycast down toward gameobjects (down toward renderable sides to detect ray collistion)
                Vector3 rayorigin = new Vector3(worldcoord.x, terrain.GetPosition().y + rayOriginAboveTerrain, worldcoord.z);
                Ray raydown = new Ray(rayorigin, Vector3.down);
                RaycastHit[] hits = Physics.RaycastAll(raydown);

                //step 2c - hits are returned in order they occur along ray
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider.gameObject.name == gameobjectName)
                    {
                        heightmap[heightX, heightZ] = Convert_Hit_to_HeightMapValue(hit.point, terrain);
                    }
                }
            }
        }

        //step 3. apply modified heightmap
        terrain.terrainData.SetHeights(0, 0, heightmap);
        terrain.Flush();
    }
    public static void Modify_TerrainAlphamap_to_GameObject(string gameobjectName, int LayerOn, Terrain terrain)
    {
        int resolution = terrain.terrainData.alphamapResolution - 1;
        float rayOriginAboveTerrain = terrain.terrainData.size.y;

        //step 1. get current alphamap
        float[,,] alphamap = terrain.terrainData.GetAlphamaps(0, 0, terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight);

        //step 2. loop, scan, change alpha
        for (int alphaX = 0; alphaX <= resolution; alphaX++)
        {
            for (int alphaZ = 0; alphaZ <= resolution; alphaZ++)
            {
                //step 2a. need worldcoord to raycast
                Vector3 worldcoord = Convert_AlphaMapCoord_to_WorldCoord(alphaX, alphaZ, terrain);

                //step 2b - raycast down toward gameobjects (down toward renderable sides to detect ray collistion)
                Vector3 rayorigin = new Vector3(worldcoord.x, terrain.GetPosition().y + rayOriginAboveTerrain, worldcoord.z);
                Ray raydown = new Ray(rayorigin, Vector3.down);
                RaycastHit[] hits = Physics.RaycastAll(raydown);

                //step 2c - hits are returned in order they occur along ray
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider.gameObject.name == gameobjectName)
                    {
                        for (int idx = 0; idx < terrain.terrainData.alphamapLayers; idx++)
                        {
                            if (idx == LayerOn)
                            {
                                alphamap[(int)alphaX, (int)alphaZ, LayerOn] = 1.0f;
                            }
                            else
                            {
                                alphamap[(int)alphaX, (int)alphaZ, idx] = 0.0f;
                            }
                        }
                    }
                }
            }
        }

        //step 3. apply modified alphamap
        terrain.terrainData.SetAlphamaps(0, 0, alphamap);
        terrain.Flush();
    }
    public static void RenderPeriodicSphere_at_worldcoord(Vector3 worldcoord, float period, float size)
    {
        if (worldcoord.x % period == 0 && worldcoord.z % period == 0)
        {
            GameObject gSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gSphere.transform.localPosition = worldcoord;
            gSphere.transform.localScale = new Vector3(size, size, size);
        }
    }
    public static void SaveHeightmapToFile(string filename, Terrain terrain)
    {
        //the data
        float[,] heightmapData = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);

        //convert to binary
        byte[] heightmapBytes = null;
        BinaryFormatter formatter = new BinaryFormatter();
        using (MemoryStream stream = new MemoryStream())
        {
            formatter.Serialize(stream, heightmapData);
            heightmapBytes = stream.ToArray();
        }

        //write to binary file
        string fullpath = Path.Combine(Application.dataPath, filename);
        try
        {
            File.WriteAllBytes(fullpath, heightmapBytes);
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format("Failed to save {0}. Error: {1}", fullpath, ex.Message));
        }
    }
    public static void LoadHeightmapToFile(string filename, Terrain terrain)
    {
        byte[] heightmapBytes = null;
        float[,] heightmapData = null;

        //load bytes from file
        string fullpath = Path.Combine(Application.dataPath, filename);
        try
        {
            heightmapBytes = File.ReadAllBytes(fullpath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format("Failed to load {0}. Error: {1}", fullpath, ex.Message));
        }

        //convert bytes to data
        if (heightmapBytes != null)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream(heightmapBytes))
            {
                heightmapData = (float[,])formatter.Deserialize(stream);
            }
        }

        //set terrain heightmap
        terrain.terrainData.SetHeights(0, 0, heightmapData);
    }
    public static void SaveAlphamapToFile(string filename, Terrain terrain)
    {
        //get data
        float[,,] alphamapData = terrain.terrainData.GetAlphamaps(0, 0, terrain.terrainData.alphamapResolution, terrain.terrainData.alphamapResolution);

        //convert to bytes
        byte[] alphamapBytes = null;
        BinaryFormatter formatter = new BinaryFormatter();
        using (MemoryStream stream = new MemoryStream())
        {
            formatter.Serialize(stream, alphamapData);
            alphamapBytes = stream.ToArray();
        }

        //save
        string fullpath = Path.Combine(Application.dataPath, filename);
        try
        {
            File.WriteAllBytes(fullpath, alphamapBytes);
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format("Failed to save {0}. Error: {1}", fullpath, ex.Message));
        }
    }
    public static void LoadAlphamapToFile(string filename, Terrain terrain)
    {
        byte[] alphamapBytes = null;

        //load bytes
        string fullpath = Path.Combine(Application.dataPath, filename);
        try
        {
            alphamapBytes = File.ReadAllBytes(fullpath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format("Failed to load {0}. Error: {1}", fullpath, ex.Message));
        }

        //convert to data
        if (alphamapBytes != null)
        {
            float[,,] alphamapData = null;
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream(alphamapBytes))
            {
                alphamapData = (float[,,])formatter.Deserialize(stream);
            }
            terrain.terrainData.SetAlphamaps(0, 0, alphamapData);
        }

    }
    #endregion
}
