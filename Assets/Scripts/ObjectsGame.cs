using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ObjectsGame : PersistableObject
{
    public KeyCode createKey = KeyCode.C;
    public KeyCode newGameKey = KeyCode.N;
    public KeyCode saveKey = KeyCode.S;
    public KeyCode loadKey = KeyCode.L;
    public KeyCode destroyKey = KeyCode.X;

    public int levelCount;
    public PersistentStorage storage;

    [SerializeField]
    ShapeFactory shapeFactory;

    /*
I've only shown the change for shapeFactory, but do the same for the key configuration fields, storage, and level count.
Usually attributes are placed above whatever they apply to, but because there are a lot of fields I've put them on the same line in this case.
    */

    public SpawnZone SpawnZoneOfLevel { get; set; }

    List<Shape> shapes;

    float creationProgress,
        destructionProgress;

    const int saveVersion = 1;

    int loadedLevelBuildIndex;

    public float CreationSpeed { get; set; }

    public float DestructionSpeed { get; set; }

    public static ObjectsGame Instance { get; private set; } // not part of unity game state; not persisted between plays

    IEnumerator LoadLevel(int levelBuildIndex)
    {
        enabled = false; // stop Update() calls
        if (loadedLevelBuildIndex > 0)
        {
            yield return SceneManager.UnloadSceneAsync(loadedLevelBuildIndex);
        }
        yield return SceneManager.LoadSceneAsync(levelBuildIndex, LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(levelBuildIndex)); // needs to wait for async load

        loadedLevelBuildIndex = levelBuildIndex;

        enabled = true;
    }

    void OnEnable()
    {
        Instance = this;
    }

    void Start() // later than Awake
    {
        Instance = this;

        shapes = new List<Shape>();

        if (Application.isEditor)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loadedScene = SceneManager.GetSceneAt(i);
                if (loadedScene.name.Contains("Level "))
                {
                    SceneManager.SetActiveScene(loadedScene);
                    loadedLevelBuildIndex = loadedScene.buildIndex;

                    return;
                }
            }
        }

        StartCoroutine(LoadLevel(1));
    }

    /*
How can we deal with many levels?
If a game has many levels, then it is more practical to put them in separate asset bundles, which could possibly be downloaded on demand.
 This also makes it possible to update or add levels to the game later. Asset bundles are not covered by this tutorial.
    */

    void Update()
    {
        if (Input.GetKeyDown(createKey))
        {
            CreateShape();
        }
        else if (Input.GetKey(newGameKey))
        {
            BeginNewGame();
        }
        else if (Input.GetKeyDown(saveKey))
        {
            storage.Save(this, saveVersion);
        }
        else if (Input.GetKeyDown(loadKey))
        {
            BeginNewGame();
            storage.Load(this);
        }
        else if (Input.GetKeyDown(destroyKey))
        {
            DestroyShape();
        }
        else
        {
            for (int i = 1; i <= levelCount; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    BeginNewGame();

                    StartCoroutine(LoadLevel(i));
                    return;
                }
            }
        }

        creationProgress += Time.deltaTime * CreationSpeed;
        while (creationProgress >= 1f)
        {
            creationProgress -= 1f;
            CreateShape();
        }

        destructionProgress += Time.deltaTime * DestructionSpeed;
        while (destructionProgress >= 1f)
        {
            destructionProgress -= 1f;
            DestroyShape();
        }
    }

    void BeginNewGame()
    {
        for (int i = 0; i < shapes.Count; i++)
        {
            shapeFactory.Reclaim(shapes[i]);
        }
        shapes.Clear();
    }

    void CreateShape()
    {
        Shape instance = shapeFactory.GetRandom();
        Transform t = instance.transform;
        t.localPosition = SpawnZoneOfLevel.SpawnPoint;
        t.localRotation = Random.rotation;
        t.localScale = Vector3.one * Random.Range(0.1f, 1f);
        instance.SetColor(
            Random.ColorHSV(
                hueMin: 0f,
                hueMax: 1f,
                saturationMin: 0.5f,
                saturationMax: 1f,
                valueMin: 0.25f,
                valueMax: 1f,
                alphaMin: 1f,
                alphaMax: 1f
            )
        );
        shapes.Add(instance);
    }

    void DestroyShape()
    {
        if (shapes.Count > 0)
        {
            int index = Random.Range(0, shapes.Count);
            shapeFactory.Reclaim(shapes[index]);
            int lastIndex = shapes.Count - 1;
            shapes[index] = shapes[lastIndex];
            shapes.RemoveAt(lastIndex);
        }
    }

    public override void Save(GameDataWriter writer)
    {
        writer.Write(shapes.Count);
        writer.Write(loadedLevelBuildIndex);

        for (int i = 0; i < shapes.Count; i++)
        {
            writer.Write(shapes[i].ShapeId);
            writer.Write(shapes[i].MaterialId);

            shapes[i].Save(writer);
        }
    }

    public override void Load(GameDataReader reader)
    {
        int version = reader.Version;
        if (version > saveVersion)
        {
            Debug.LogError("Unsupported future save version " + version);
            return;
        }
        int count = version <= 0 ? -version : reader.ReadInt();
        StartCoroutine(LoadLevel(version < 2 ? 1 : reader.ReadInt())); // save game approach?

        for (int i = 0; i < count; i++)
        {
            int shapeId = version > 0 ? reader.ReadInt() : 0;
            int materialId = version > 0 ? reader.ReadInt() : 0;
            Shape instance = shapeFactory.Get(shapeId, materialId);
            instance.Load(reader);
            shapes.Add(instance);
        }
    }
}
