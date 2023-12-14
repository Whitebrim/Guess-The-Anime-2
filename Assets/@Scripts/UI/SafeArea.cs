using UnityEngine;

public class SafeArea : MonoBehaviour
{
    private void Awake()
    {
        UpdateSafeArea();
    }

    private void UpdateSafeArea()
    {
        Rect safeArea = Screen.safeArea;
        var myRectTransform = GetComponent<RectTransform>();

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        myRectTransform.anchorMin = anchorMin;
        myRectTransform.anchorMax = anchorMax;
    }
}