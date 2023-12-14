using System;
using UnityEngine;

public class Toast
{
    private static readonly Lazy<Toast> LazyLoader = new(() => new Toast());
    private AndroidJavaObject currentActivity;
    private string toastString;
    private static Toast _instance => LazyLoader.Value;

    public static void Show(string toastString)
    {
#if UNITY_EDITOR
        Debug.Log($"Showed Android Toast: \"{toastString}\"");
#else
        AndroidJavaClass UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        _instance.currentActivity = UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        _instance.toastString = toastString;
        _instance.currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(_instance.ShowToast));
#endif
    }

    private void ShowToast()
    {
        var context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
        var Toast = new AndroidJavaClass("android.widget.Toast");
        var javaString = new AndroidJavaObject("java.lang.String", toastString);

        var toast = Toast.CallStatic<AndroidJavaObject>("makeText", context, javaString, Toast.GetStatic<int>("LENGTH_LONG"));
        toast.Call("show");
    }
}