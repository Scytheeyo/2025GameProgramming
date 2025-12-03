using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAnim : MonoBehaviour
{
    public float delay = 2.0f; // 사라지는 시간 (애니메이션 길이보다 약간 길게)

    void Start()
    {
        Destroy(gameObject, delay);
    }
}
