using UnityEngine;

public class LifeTime : MonoBehaviour
{
    public float Life = 0.1f;

    private void Start()
    {
        Destroy(gameObject, Life);
    }
}