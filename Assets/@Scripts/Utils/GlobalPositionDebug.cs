using UnityEngine;

[RequireComponent(typeof(Transform))]
[ExecuteAlways]
public class GlobalPositionDebug : MonoBehaviour
{
    [Header("Global")] [SerializeField] private Vector3 globalPosition;

    [SerializeField] private Quaternion globalRotation;
    [SerializeField] private Vector3 lossyScale;

    [Header("Local")] [SerializeField] private Vector3 localPosition;

    [SerializeField] private Quaternion localRotation;
    [SerializeField] private Vector3 localScale;

    private void Update()
    {
        globalPosition = transform.position;
        globalRotation = transform.rotation;
        lossyScale = transform.lossyScale;

        localPosition = transform.localPosition;
        localRotation = transform.localRotation;
        localScale = transform.localScale;
    }
}