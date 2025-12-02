using UnityEngine;

public class NoDestroy : MonoBehaviour
{
    private void Awake()
    {
        
        var objs = FindObjectsOfType<NoDestroy>();

        foreach (var obj in objs)
        {

            if (obj != this && obj.name == this.name)
            {

                Destroy(gameObject);
                return; 
            }
        }
        DontDestroyOnLoad(gameObject);
    }
}