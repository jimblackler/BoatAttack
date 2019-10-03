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

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaClass unityPlayer = null;
    private AndroidJavaObject currentActivity = null;
    private AndroidJavaClass heuristics = null;
    private AndroidJavaClass debug = null;
    private AndroidJavaObject memoryInfo = null;

    private LMKHeuristicWrapper _oomCheckWrapper = new LMKHeuristicWrapper();
    private LMKHeuristicWrapper _lowMemoryCheckWrapper = new LMKHeuristicWrapper();
    private LMKHeuristicWrapper _commitLimitWrapper = new LMKHeuristicWrapper();
    private LMKHeuristicWrapper _availMemCheckWrapper = new LMKHeuristicWrapper();
#endif

    private bool _lastBroadCastWasLow = false;
    private List<ILMKListener> _registeredObjects = new List<ILMKListener>();

    void Start()
    {
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
#if UNITY_ANDROID && !UNITY_EDITOR
        long memory = debug.CallStatic<long>("getNativeHeapAllocatedSize");
        return format(memory);
#else
        return "Unknown";
#endif
    }

    private string GetAvailMem()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
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
#if UNITY_ANDROID && !UNITY_EDITOR
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

        bool broadCastLow = false;
        _oomCheckWrapper.Update(() =>
        {
            bool result = heuristics.CallStatic<bool>("oomCheck", currentActivity);
            if (result) broadCastLow = true;
            return result;
        });

        _lowMemoryCheckWrapper.Update(() =>
        {
            bool result = heuristics.CallStatic<bool>("lowMemoryCheck", currentActivity);
            if (result) broadCastLow = true;
            return result;
        });

        _commitLimitWrapper.Update(() =>
        {
            bool result = heuristics.CallStatic<bool>("commitLimitCheck");
            if (result) broadCastLow = true;
            return result;
        });

        _availMemCheckWrapper.Update(() =>
        {
            bool result = heuristics.CallStatic<bool>("availMemCheck", currentActivity);
            if (result) broadCastLow = true;
            return result;
        });

        if (broadCastLow)
        {
            BroadCastLow();
        }
#endif
    }

    private void UpdateDebugText()
    {
        StringBuilder sb = new StringBuilder();

#if UNITY_ANDROID && !UNITY_EDITOR
        string nativeHeap = GetNativeHeap();
        string availMem = GetAvailMem();

        sb.AppendLine(string.Format("Native Heap: {0}", nativeHeap));
        sb.AppendLine(string.Format("Avail Mem: {0}", availMem));

        _oomCheckWrapper.AppendDebugText("oomCheck", sb);
        _lowMemoryCheckWrapper.AppendDebugText("lowMemoryCheck", sb);
        _commitLimitWrapper.AppendDebugText("commitLimitCheck", sb);
        _availMemCheckWrapper.AppendDebugText("availMemCheck", sb);
        Debug.Log(string.Format("{0}, {1}", nativeHeap, availMem));
#endif

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
