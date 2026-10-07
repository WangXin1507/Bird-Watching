using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public Vector3 birdPosition = Vector3.zero;
    public List<string> unlockedTranscriptIds = new List<string>();
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;
    
    public SaveData data;
    private string savePath;

    [SerializeField] private float autosaveTimer = 60f;
    [SerializeField] private bool autosaveOnStart = true;
    [SerializeField] private bool loadOnStart = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        savePath = Path.Combine(Application.persistentDataPath, "playerSave.json");
        if (autosaveOnStart)
            StartCoroutine(AutoSave(autosaveTimer));
        if (loadOnStart)
            Load();
    }

    public void Save()
    {
        data.birdPosition = PlayerID.playerMovement.transform.position;
        
        string jsonText = JsonUtility.ToJson(data, true);
        
        File.WriteAllText(savePath, jsonText);
    }

    public void Load()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogWarning("Save file not found!");
            return;
        }

        string jsonText = File.ReadAllText(savePath);

        SaveData loadedData = JsonUtility.FromJson<SaveData>(jsonText);
        data = loadedData;
    }

    [ContextMenu("Reset Save")]
    public void ResetSave()
    {
        data = new SaveData();
        Save();
    }

    private IEnumerator AutoSave(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Save();
        StartCoroutine(AutoSave(autosaveTimer));
    }
}
