using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]

public class BGScrollData
{
    public Renderer renderForScroll;
    public float speed;
    public float offSetX;
}


public class BGScroller : MonoBehaviour
{
    [SerializeField]
    BGScrollData [] scrollDatas;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateScroll();
    }

    void UpdateScroll()
    {
        for(int i = 0; i < scrollDatas.Length; i++)
        {
            SetTextureOffset(scrollDatas[i]);
        }
    }

    void SetTextureOffset(BGScrollData scrollData)
    {
        scrollData.offSetX += (float)(scrollData.speed) * Time.deltaTime;
        if (scrollData.offSetX > 1)
            scrollData.offSetX = scrollData.offSetX % 1.0f;

        Vector2 offset = new Vector2(scrollData.offSetX, 0);

        scrollData.renderForScroll.material.SetTextureOffset("_MainTex", offset);
    }
}
