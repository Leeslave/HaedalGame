using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TaskLogger : MonoBehaviour
{
    public static TaskLogger Instance;

    private readonly List<string> servingLogs = new List<string>();
    private readonly List<string> cookingLogs = new List<string>();

    public string servingFilePath;
    public string cookingFilePath;


    void Awake()
    {
        Instance = this;
        string dir = Application.persistentDataPath;
        servingFilePath = Path.Combine(dir, $"serving_logs_{DateTime.Now:HH_MM}.txt");
        cookingFilePath = Path.Combine(dir, $"cooking_logs_{DateTime.Now:HH_MM}.txt");
    }

    public void LogServing(string message)
    {
        string entry = $"{DateTime.Now:HH:MM} {message}";
        File.AppendAllText(servingFilePath, entry + "\n");
        servingLogs.Add(entry);
    }

    public void LogCooking(string message)
    {
        string entry = $"{DateTime.Now:HH:MM} {message}";
        File.AppendAllText(cookingFilePath, entry + "\n");
        cookingLogs.Add(entry);
    }

    public List<string> GetServingLogs() { return servingLogs; }
    public List<string> GetCookingLogs() { return cookingLogs; }



    
}