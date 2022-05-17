using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyFactory : MonoBehaviour
{
    public const string enemyPath = "Prefabs/Enemy";

    Dictionary<string, GameObject> enemyFileCache = new Dictionary<string, GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public GameObject Load(string resourcePath)
    {
        GameObject go = null;

        if (enemyFileCache.ContainsKey(resourcePath)) // 캐시 확인
        {
            go = enemyFileCache[resourcePath];
        }
        else
        {
            // 캐시에 없으므로 로드
            go = Resources.Load<GameObject>(resourcePath);
            if (!go)
            {
                Debug.LogError("Load error! path = " + resourcePath);
                return null;
            }
            // 로드 후 캐시에 적재
            enemyFileCache.Add(resourcePath, go);          
        }     
        return go;
    }
}
