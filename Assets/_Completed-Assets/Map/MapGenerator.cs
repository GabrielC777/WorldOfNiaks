using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class MapGenerator : MonoBehaviourPun
{
    [Header("Configuración del Mapa")]
    public int width = 12;
    public int height = 12;
    public float tileSize = 5f;

    [Header("Materiales")]
    public Material groundMaterial; // Arrastra aquí la textura de arena/tierra
    public Material wallMaterial;   // Arrastra aquí la textura para los muros

    [Header("Obstáculos (Arrastra aquí tus edificios/rocas)")]
    public GameObject[] obstaclePrefabs;
    [Range(0f, 1f)]
    public float obstacleDensity = 0.3f;

    [Header("Surface y Spawner")]
    public NavMeshSurface m_NavMeshSurface;
    public EnemySpawner m_EnemySpawner;

    void Start()
    {
        transform.position = Vector3.zero;

        CreateSimpleGround();
        CreateSimpleWalls();

        if (PhotonNetwork.IsMasterClient)
        {
            int randomSeed = Random.Range(1, 999999);
            photonView.RPC("RPC_GenerateMap", RpcTarget.AllBuffered, randomSeed);
        }
    }

    [PunRPC]
    void RPC_GenerateMap(int seed)
    {
        Random.InitState(seed);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float xPos = (x - (width - 1) / 2f) * tileSize;
                float zPos = (z - (height - 1) / 2f) * tileSize;
                Vector3 spawnPos = new Vector3(xPos, 0f, zPos);

                if (Mathf.Abs(xPos) < tileSize * 1.5f && Mathf.Abs(zPos) < tileSize * 1.5f)
                    continue;

                if (Random.value < obstacleDensity)
                {
                    GameObject prefabToSpawn = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
                    Quaternion randomRotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f);

                    GameObject spawnedObstacle = Instantiate(prefabToSpawn, spawnPos, randomRotation);
                    spawnedObstacle.transform.SetParent(this.transform);

                    // Intentamos buscar si el prefab ya traía un MeshCollider de serie
                    MeshCollider meshCol = spawnedObstacle.GetComponent<MeshCollider>();

                    // Si no lo tiene (que es lo normal con modelos limpios de Blender), se lo metemos nosotros
                    if (meshCol == null)
                    {
                        meshCol = spawnedObstacle.AddComponent<MeshCollider>();
                    }
                    // FILTRO INTELIGENTE PARA DETALLES DE TERRENO
                    string nombrePrefab = prefabToSpawn.name.ToLower();

                    if (nombrePrefab.Contains("crater") || nombrePrefab.Contains("dunes") || nombrePrefab.Contains("concrete") || nombrePrefab.Contains("cow") || nombrePrefab.Contains("ruins"))
                    {
                        // Si es una duna baja o decorativa, NO le ponemos modificador obstáculo.
                        // Forzamos a que el NavMesh la trate como suelo caminable si es necesario.
                        NavMeshModifier modifierDuna = spawnedObstacle.AddComponent<NavMeshModifier>();
                        modifierDuna.overrideArea = true;
                        modifierDuna.area = NavMesh.GetAreaFromName("Walkable"); 

                        Debug.Log($"<color=green>[MAPA]</color> {spawnedObstacle.name} filtrado como SUELO transitable.");
                    }
                    else
                    {
                        // Si es un edificio, roca grande o muro, le plantamos el bloqueo total
                        NavMeshModifier modifierEdificio = spawnedObstacle.AddComponent<NavMeshModifier>();
                        modifierEdificio.overrideArea = true;
                        modifierEdificio.area = NavMesh.GetAreaFromName("Not Walkable");
                    }
                }
            }
        }
        // ---BAKE DEL NAVMESH EN TIEMPO DE EJECUCIÓN ---
        // Una vez que el bucle de los edificios ha terminado por completo, calculamos la malla
        if (m_NavMeshSurface != null)
        {
            Debug.Log("<color=yellow>[NAVMESH]</color> Horneando NavMesh procedural...");
            m_NavMeshSurface.BuildNavMesh();
        }

        if (EnemySpawner.Instance != null)
        {
            Debug.Log("<color=orange>[MAPA]</color> NavMesh listo. Avisando al Spawner para iniciar cuenta atrás...");
            EnemySpawner.Instance.IniciarSpawning();
        }
    }

    void CreateSimpleGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Suelo Liso";
        ground.transform.position = Vector3.zero;

        float scaleX = (width * tileSize) / 10f;
        float scaleZ = (height * tileSize) / 10f;
        ground.transform.localScale = new Vector3(scaleX, 1f, scaleZ);

        // Aplicamos el material y ajustamos la repetición (Tiling) para que no se estire
        if (groundMaterial != null)
        {
            MeshRenderer renderer = ground.GetComponent<MeshRenderer>();
            renderer.material = groundMaterial;
            renderer.material.mainTextureScale = new Vector2(scaleX * 2f, scaleZ * 2f);
        }

        ground.transform.SetParent(this.transform);
    }

    void CreateSimpleWalls()
    {
        float mapWidth = width * tileSize;
        float mapHeight = height * tileSize;
        float thickness = 2f;
        float wallHeight = 5f;

        CreateWall(new Vector3(0, wallHeight / 2f, mapHeight / 2f), new Vector3(mapWidth + thickness * 2, wallHeight, thickness), "Norte");
        CreateWall(new Vector3(0, wallHeight / 2f, -mapHeight / 2f), new Vector3(mapWidth + thickness * 2, wallHeight, thickness), "Sur");
        CreateWall(new Vector3(mapWidth / 2f, wallHeight / 2f, 0), new Vector3(thickness, wallHeight, mapHeight), "Este");
        CreateWall(new Vector3(-mapWidth / 2f, wallHeight / 2f, 0), new Vector3(thickness, wallHeight, mapHeight), "Oeste");
    }

    void CreateWall(Vector3 pos, Vector3 scale, string name)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Muro " + name;
        wall.transform.position = pos;
        wall.transform.localScale = scale;

        // Aplicamos el material y calculamos cómo debe repetirse en la pared
        if (wallMaterial != null)
        {
            MeshRenderer renderer = wall.GetComponent<MeshRenderer>();
            renderer.material = wallMaterial;

            // Como las paredes pueden ser a lo largo de X o a lo largo de Z, adaptamos el dibujo
            if (scale.x > scale.z)
                renderer.material.mainTextureScale = new Vector2(scale.x / 4f, scale.y / 4f);
            else
                renderer.material.mainTextureScale = new Vector2(scale.z / 4f, scale.y / 4f);
        }
        NavMeshModifier modifierMuro = wall.AddComponent<NavMeshModifier>();
        modifierMuro.overrideArea = true;
        modifierMuro.area = NavMesh.GetAreaFromName("Not Walkable");

        wall.transform.SetParent(this.transform);
    }
}