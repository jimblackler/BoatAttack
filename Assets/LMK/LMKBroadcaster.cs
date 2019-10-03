using System.Text;
using System.Globalization;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class LMKBroadcaster: MonoBehaviour
{
    // Intended to be set via the Editor
    public Text debugStatusText;
    public Text debugToggleText;

#if UNITY_ANDROID
    private AndroidJavaClass unityPlayer = null;
    private AndroidJavaObject currentActivity = null;
    private AndroidJavaClass heuristics = null;
    private AndroidJavaClass debug = null;
    private AndroidJavaObject memoryInfo = null;

    private bool oomCheckResult = false;
    private bool lowMemoryCheckResult = false;
    private bool commitLimitResult = false;
    private bool availMemCheckResult = false;
#endif

    private bool _lastBroadCastWasLow = false;
    private List<ILMKListener> _registeredObjects = new List<ILMKListener>();

    void Start()
    {
        Update();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateHeuristic();
        UpdateDebugText();
    }

    public void RegisterListener(ILMKListener listener)
    {
        _registeredObjects.Add(listener);
    }
    public void DeregisterListener(ILMKListener listener)
    {
        _registeredObjects.Remove(listener);
    }

    public void ToggleBroadCast()
    {
        if (_lastBroadCastWasLow == true) BroadCastHigh();
        else BroadCastLow();
    }

    private void BroadCastLow()
    {
        if (_lastBroadCastWasLow == true)
        {
            return;
        }

        foreach (ILMKListener listener in _registeredObjects)
        {
            listener.OnLowMemory();
        }

        _lastBroadCastWasLow = true;
    }

    private void BroadCastHigh()
    {
        if (_lastBroadCastWasLow == false)
        {
            return;
        }

        foreach (ILMKListener listener in _registeredObjects)
        {
            listener.OnHighMemory();
        }

        _lastBroadCastWasLow = false;
    }

    private string GetNativeHeap()
    {
#if UNITY_ANDROID
        long memory = debug.CallStatic<long>("getNativeHeapAllocatedSize");
        return format(memory);
#else
        return "Unknown";
#endif
    }

    private string GetAvailMem()
    {
#if UNITY_ANDROID
        long memory = memoryInfo.Get<long>("availMem");
        return format(memory);
#else
        return "Unknown";
#endif
    }

    private static string format(long memory)
    {
        float megabytes = (float)memory / (1024 * 1024);
        return megabytes.ToString("F1", CultureInfo.InvariantCulture) + " MB";
    }

    private void UpdateHeuristic()
    {
#if UNITY_ANDROID
        unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        heuristics = new AndroidJavaClass("net.jimblackler.istresser.Heuristics");
        debug = new AndroidJavaClass("android.os.Debug");

        memoryInfo = new AndroidJavaObject("android.app.ActivityManager$MemoryInfo");

        if (currentActivity != null)
        {
            using (AndroidJavaObject systemService = currentActivity.Call<AndroidJavaObject>("getSystemService", "activity"))
            {
                systemService.Call("getMemoryInfo", memoryInfo);
            }
        }

        oomCheckResult = heuristics.CallStatic<bool>("oomCheck", currentActivity);
        lowMemoryCheckResult = heuristics.CallStatic<bool>("lowMemoryCheck", currentActivity);
        commitLimitResult = heuristics.CallStatic<bool>("commitLimitCheck");
        availMemCheckResult = heuristics.CallStatic<bool>("availMemCheck", currentActivity);

        if (oomCheckResult || lowMemoryCheckResult || commitLimitResult || availMemCheckResult)
        {
            BroadCastLow();
        }
#endif
    }

    private void UpdateDebugText()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine(string.Format("Native Heap: {0}", GetNativeHeap()));
        sb.AppendLine(string.Format("Avail Mem: {0}", GetAvailMem()));
        sb.AppendLine(string.Format("oomCheck: {0}", oomCheckResult));
        sb.AppendLine(string.Format("lowMemoryCheck: {0}", lowMemoryCheckResult));
        sb.AppendLine(string.Format("commitLimitCheck: {0}", commitLimitResult));
        sb.AppendLine(string.Format("availMemCheck: {0}", availMemCheckResult));

        if (debugStatusText != null)
        {
            debugStatusText.text = sb.ToString();
        }

        if (debugToggleText != null)
        {
            debugToggleText.text = _lastBroadCastWasLow ? "Broadcast High" : "Broadcast Low";
        }
    }
}
