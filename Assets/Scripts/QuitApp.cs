using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RayNeo;

public class QuitApp : MonoBehaviour
{
    /// <summary>
    /// 退出应用
    /// </summary>
    public void ToQuitApp()
    {
        Application.Quit();
    }

    void Start()
    {
        // 添加双击事件
        SimpleTouch.Instance.OnDoubleTap.AddListener(ToQuitApp);
    }

    private void OnDestroy()
    {
        if (SimpleTouch.SingletonExist)
        {
            // 移除双击事件
            SimpleTouch.Instance.OnDoubleTap.RemoveListener(ToQuitApp);
        }
    }
}