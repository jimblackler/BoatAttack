using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AddressableAssets;

public class LMKPrefabWrapper : MonoBehaviour, ILMKListener
{
    public GameObject PrefabToInit;
    public string AddressableName;
    
    private GameObject _instantiatedObject;
    private LMKBroadcaster _broadcaster;
    private UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle _operationHandle;
    private bool _busy = false;

    // Start is called before the first frame update
    void Start()
    {
        _broadcaster = GameObject.FindObjectOfType<LMKBroadcaster>();

        if (_broadcaster)
            _broadcaster.RegisterListener(this);

        Load();
    }

    private void Stop()
    {
        if (_broadcaster)
            _broadcaster.DeregisterListener(this);
    }

    public void OnLowMemory()
    {
        Unload();
    }
    public void OnHighMemory()
    {
        Load();
    }

    public void Load()
    {
        _busy = true;
        Addressables.LoadAssetAsync<GameObject>(AddressableName).Completed += OnLoadDone;
    }

    private void OnLoadDone(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> obj)
    {
        _operationHandle = obj;
        _instantiatedObject = Instantiate(obj.Result, transform);
        _busy = false;
    }

    public void Unload()
    {
        Destroy(_instantiatedObject);
        Addressables.Release(_operationHandle);

        _instantiatedObject = null;
    }

}
