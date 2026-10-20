using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>격리된 Unity 프로젝트의 -executeMethod 진입점. 현재 사용자 씬을 변경하지 않는다.</summary>
public static class PlanningBatchValidation
{
    [MenuItem("Haedal/Validation/Planning specification checks")]
    public static void Run()
    {
        if (!Application.isBatchMode && (EditorApplication.isPlayingOrWillChangePlaymode || GameSession.IsActive))
        { Debug.LogWarning("검증은 플레이 모드를 종료한 후 실행하세요."); return; }
        var results=new List<string>();int failed=0,passed=0;
        foreach(var type in new[]{typeof(ProgressionTests),typeof(PlanningSpecificationTests)})
        foreach(var method in type.GetMethods())
        {
            var cases=method.GetCustomAttributes<TestCaseAttribute>().ToArray();
            if(cases.Length==0 && method.GetCustomAttribute<TestAttribute>()==null) continue;
            var args=cases.Length==0 ? new[]{Array.Empty<object>()} : cases.Select(c=>c.Arguments).ToArray();
            foreach(var parameters in args)
            {
                object instance=Activator.CreateInstance(type); string name=type.Name+"."+method.Name+"("+string.Join(",",parameters)+")";
                try
                {
                    foreach(var setup in type.GetMethods().Where(m=>m.GetCustomAttribute<SetUpAttribute>()!=null)) setup.Invoke(instance,null);
                    method.Invoke(instance,parameters);results.Add("PASS "+name);passed++;
                }
                catch(Exception e) { failed++;results.Add("FAIL "+name+"\n"+(e.InnerException??e)); }
                finally { foreach(var teardown in type.GetMethods().Where(m=>m.GetCustomAttribute<TearDownAttribute>()!=null)) teardown.Invoke(instance,null); }
            }
        }
        results.Add($"Passed: {passed}, Failed: {failed}");
        Directory.CreateDirectory("Temp/PlanningAudit");
        File.WriteAllLines("Temp/PlanningAudit/planning-validation.txt",results);
        Debug.Log(results.Last()); if (Application.isBatchMode) EditorApplication.Exit(failed==0?0:1);
    }
}
