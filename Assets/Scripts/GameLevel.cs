using UnityEngine;

public class GameLevel : MonoBehaviour
{
    [SerializeField]
    SpawnZone spawnZone;

    void Start()
    {
        ObjectsGame.Instance.SpawnZoneOfLevel = spawnZone;
    }
}
