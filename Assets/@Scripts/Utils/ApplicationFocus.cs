using UnityEngine;

public class ApplicationFocus : MonoBehaviour
{
    public delegate void BoolDelegate(bool hasFocus);

    private void OnApplicationFocus(bool hasFocus)
    {
        onApplicationFocus?.Invoke(hasFocus);
    }

    public static event BoolDelegate onApplicationFocus;
}