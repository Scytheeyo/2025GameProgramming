using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    public float delay = 0.5f; // 사라질 시간 (초)

    void Start()
    {
        // 태어나자마자 'delay'초 뒤에 죽을 운명 예약
        Destroy(gameObject, delay);
    }
}