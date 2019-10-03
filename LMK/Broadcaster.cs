using System.Text;
using System.Globalization;

using UnityEngine;
using UnityEngine.AddressableAssets;

public class LMKHeuristic : MonoBehaviour
{
    // Intended to be set via the Editor
    public GameObject parent;
    public GameObject statusTextObject;
    public GameObject loadTextObject;

    private GameObject instantiateObject;
    private bool loaded = false;
    private bool busy = false;
    private UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle operationHandle;

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

    public void OnButtonPress()
    {
        /*
        if (loaded) UnLoadBench();
        else LoadBench();
        */
        Application.SimulateLowMemory();
    }

    private void LoadBench()
    {
        if (busy || loaded) return;

        busy = true;
        Addressables.LoadAssetAsync<GameObject>("LMKMain").Completed += OnLoadDone;
    }

    private void OnLoadDone(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> obj)
    {
        operationHandle = obj;
        instantiateObject = Instantiate(obj.Result, parent.transform);
        loaded = true;
        busy = false;
    }

    private void UnLoadBench()
    {
        if (busy || loaded == false) return;

        busy = true;
        Destroy(instantiateObject);
        Addressables.Release(operationHandle);

        instantiateObject = null;
        loaded = false;
        busy = false;
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
            UnLoadBench();
        }
#endif
    }

    private void UpdateDebugText()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine(loaded ? "Loaded" : "UnLoaded");
        sb.AppendLine(busy ? "Busy" : "Idle");
        sb.AppendLine(string.Format("Native Heap: {0}", GetNativeHeap()));
        sb.AppendLine(string.Format("Avail Mem: {0}", GetAvailMem()));
        sb.AppendLine(string.Format("oomCheck: {0}", oomCheckResult));
        sb.AppendLine(string.Format("lowMemoryCheck: {0}", lowMemoryCheckResult));
        sb.AppendLine(string.Format("commitLimitCheck: {0}", commitLimitResult));
        sb.AppendLine(string.Format("availMemCheck: {0}", availMemCheckResult));

        statusTextObject.GetComponent<UnityEngine.UI.Text>().text = sb.ToString();
        loadTextObject.GetComponent<UnityEngine.UI.Text>().text = loaded ? "Unload Bench" : "Load Bench";
    }
}
