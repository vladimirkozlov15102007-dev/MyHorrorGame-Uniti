#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace OldAmberFactory.EditorTools
{
    /// <summary>
    /// Editor tool that generates a block-out of the Old Amber Factory (5 zones).
    /// Use: "Old Amber Factory/Build Blockout Level".
    ///
    /// The result is a coarse playable layout - primitive boxes with tags and markers
    /// for patrol points, cover points, ambush points, security cameras, key, truck, exit.
    /// Replace primitives with high-poly meshes / photogrammetry props from Megascans.
    /// </summary>
    public static class LevelGenerator
    {
        [MenuItem("Old Amber Factory/Build Blockout Level")]
        public static void Build()
        {
            var root = GameObject.Find("Level_Blockout");
            if (root != null) Object.DestroyImmediate(root);
            root = new GameObject("Level_Blockout");

            // Zone 1 - Admin block (tight corridors, small rooms).
            BuildZone(root.transform, "Zone1_Admin", new Vector3(-40, 0, -20), new Vector3(20, 4, 18), roomCount: 6, withCCTV: true);
            // Zone 2 - Main production hall (huge open space, gantries).
            BuildZone(root.transform, "Zone2_MainHall", new Vector3(0, 0, 0), new Vector3(45, 12, 45), roomCount: 0, withGantry: true);
            // Zone 3 - Warehouse (tall shelves).
            BuildZone(root.transform, "Zone3_Warehouse", new Vector3(40, 0, 5), new Vector3(30, 10, 30), roomCount: 0, withShelves: true);
            // Zone 4 - Tunnels & vents.
            BuildZone(root.transform, "Zone4_Tunnels", new Vector3(20, -5, -25), new Vector3(25, 3, 20), roomCount: 0, tunnels: true);
            // Zone 5 - Outdoor (truck).
            BuildOutdoor(root.transform, new Vector3(80, 0, 0));

            // Spawn: player start, skeletons, truck, key, power switch.
            SpawnGameplay(root.transform);

            Debug.Log("Old Amber Factory blockout built. Bake NavMesh and set up HDRP volumes next.");
            Selection.activeObject = root;
        }

        static Transform BuildZone(Transform parent, string name, Vector3 pos, Vector3 size,
            int roomCount = 0, bool withGantry = false, bool withShelves = false, bool tunnels = false, bool withCCTV = false)
        {
            var z = new GameObject(name).transform;
            z.SetParent(parent);
            z.position = pos;

            // Floor.
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(z);
            floor.transform.localPosition = new Vector3(0, -0.5f, 0);
            floor.transform.localScale = new Vector3(size.x, 1, size.z);
            floor.tag = "Untagged";

            // 4 walls.
            CreateWall(z, new Vector3(0, size.y * 0.5f, size.z * 0.5f), new Vector3(size.x, size.y, 0.5f), "WallN");
            CreateWall(z, new Vector3(0, size.y * 0.5f, -size.z * 0.5f), new Vector3(size.x, size.y, 0.5f), "WallS");
            CreateWall(z, new Vector3(size.x * 0.5f, size.y * 0.5f, 0), new Vector3(0.5f, size.y, size.z), "WallE");
            CreateWall(z, new Vector3(-size.x * 0.5f, size.y * 0.5f, 0), new Vector3(0.5f, size.y, size.z), "WallW");

            // Rooms inside admin.
            for (int i = 0; i < roomCount; i++)
            {
                var room = new GameObject($"Room_{i}").transform;
                room.SetParent(z);
                room.localPosition = new Vector3(
                    Random.Range(-size.x * 0.3f, size.x * 0.3f), 0,
                    Random.Range(-size.z * 0.3f, size.z * 0.3f));
                CreatePatrolPoint(room, Vector3.zero);
                if (i % 2 == 0) CreateCoverPoint(room, new Vector3(1, 0, 0));
            }

            // Gantry / cranes in main hall.
            if (withGantry)
            {
                for (int i = 0; i < 3; i++)
                {
                    var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    beam.name = $"Gantry_{i}";
                    beam.transform.SetParent(z);
                    beam.transform.localPosition = new Vector3(-size.x * 0.4f + i * size.x * 0.4f, size.y - 1f, 0);
                    beam.transform.localScale = new Vector3(0.5f, 0.5f, size.z * 0.9f);
                }
                // Conveyors + covers.
                for (int i = 0; i < 6; i++)
                {
                    var conv = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    conv.name = $"Conveyor_{i}";
                    conv.transform.SetParent(z);
                    conv.transform.localPosition = new Vector3(Random.Range(-size.x * 0.35f, size.x * 0.35f), 1f,
                                                               Random.Range(-size.z * 0.35f, size.z * 0.35f));
                    conv.transform.localScale = new Vector3(1.5f, 1f, 6f);
                    conv.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 180f), 0);
                    CreateCoverPoint(z, conv.transform.localPosition + Vector3.forward * 1.5f);
                }
            }

            // Shelves.
            if (withShelves)
            {
                for (int i = 0; i < 8; i++)
                {
                    var shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    shelf.name = $"Shelf_{i}";
                    shelf.transform.SetParent(z);
                    shelf.transform.localPosition = new Vector3(-size.x * 0.4f + (i % 4) * size.x * 0.25f, size.y * 0.45f,
                                                                -size.z * 0.3f + (i / 4) * size.z * 0.6f);
                    shelf.transform.localScale = new Vector3(2f, size.y * 0.9f, 1f);
                    CreateCoverPoint(z, shelf.transform.localPosition + Vector3.forward * 1.5f);
                    CreateCoverPoint(z, shelf.transform.localPosition - Vector3.forward * 1.5f);
                    if (i % 3 == 0) CreateAmbushPoint(z, shelf.transform.localPosition + Vector3.right * 3f);
                }
            }

            // Tunnels: narrow shafts with cover points.
            if (tunnels)
            {
                for (int i = 0; i < 4; i++)
                {
                    var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    p.name = $"Pipe_{i}";
                    p.transform.SetParent(z);
                    p.transform.localPosition = new Vector3(-size.x * 0.4f + i * size.x * 0.25f, 1.5f, 0);
                    p.transform.localScale = new Vector3(0.6f, 0.6f, size.z);
                    CreateAmbushPoint(z, p.transform.localPosition + Vector3.right * 2.5f);
                }
            }

            if (withCCTV)
            {
                var sec = new GameObject("SecurityRoom_Anchor");
                sec.transform.SetParent(z);
                sec.transform.localPosition = new Vector3(size.x * 0.4f, 1f, size.z * 0.4f);
                sec.tag = "Interactable";
            }

            return z;
        }

        static void BuildOutdoor(Transform parent, Vector3 pos)
        {
            var z = new GameObject("Zone5_Outdoor").transform;
            z.SetParent(parent);
            z.position = pos;

            // Ground - terrain stub.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(z);
            ground.transform.localScale = new Vector3(8, 1, 8);
            ground.tag = "Untagged";

            // Truck.
            var truck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            truck.name = "YellowTruck";
            truck.transform.SetParent(z);
            truck.transform.localPosition = new Vector3(0, 1, 0);
            truck.transform.localScale = new Vector3(3, 2, 6);
            truck.AddComponent<OldAmberFactory.Environment.YellowTruck>();
            truck.tag = "Truck";

            // Exit waypoint.
            var exit = new GameObject("ExitWaypoint");
            exit.transform.SetParent(z);
            exit.transform.localPosition = new Vector3(60, 1, 0);
        }

        static void SpawnGameplay(Transform parent)
        {
            // Player spawn.
            var player = new GameObject("PlayerStart");
            player.transform.SetParent(parent);
            player.transform.position = new Vector3(-40, 1, -20);
            player.tag = "Respawn";

            // Key & power switch.
            var key = new GameObject("TruckKey");
            key.transform.SetParent(parent);
            key.transform.position = new Vector3(40, 1, 5);
            key.tag = "TruckKey";
            key.AddComponent<SphereCollider>().isTrigger = true;
            key.AddComponent<OldAmberFactory.Environment.TruckKey>();

            var sw = new GameObject("PowerSwitch");
            sw.transform.SetParent(parent);
            sw.transform.position = new Vector3(20, 1, -25);
            sw.AddComponent<BoxCollider>().isTrigger = true;
            sw.AddComponent<OldAmberFactory.Environment.PowerSwitch>();

            // Skeleton spawn markers.
            for (int i = 0; i < 10; i++)
            {
                var s = new GameObject($"SkeletonSpawn_{i}");
                s.transform.SetParent(parent);
                s.transform.position = new Vector3(
                    Random.Range(-20f, 60f),
                    0f,
                    Random.Range(-25f, 25f));
                s.tag = "Skeleton";
            }
        }

        static void CreateWall(Transform parent, Vector3 localPos, Vector3 localScale, string name)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = name;
            w.transform.SetParent(parent);
            w.transform.localPosition = localPos;
            w.transform.localScale = localScale;
        }

        static void CreatePatrolPoint(Transform parent, Vector3 localPos)
        {
            var p = new GameObject("Patrol");
            p.transform.SetParent(parent);
            p.transform.localPosition = localPos;
        }

        static void CreateCoverPoint(Transform parent, Vector3 localPos)
        {
            var p = new GameObject("Cover");
            p.transform.SetParent(parent);
            p.transform.localPosition = localPos;
            p.AddComponent<OldAmberFactory.AI.CoverPoint>();
        }

        static void CreateAmbushPoint(Transform parent, Vector3 localPos)
        {
            var p = new GameObject("Ambush");
            p.transform.SetParent(parent);
            p.transform.localPosition = localPos;
            var cp = p.AddComponent<OldAmberFactory.AI.CoverPoint>();
            cp.isAmbush = true;
        }
    }
}
#endif
