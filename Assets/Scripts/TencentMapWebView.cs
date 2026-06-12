using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using RayNeo.API;
using RayNeo;

public class TencentMapWebView : MonoBehaviour
{
    private UniWebView webView;
    private bool isMapLoaded = false;  // 地图是否已加载

    // 消息事件，用于通知 SearchController
    public event System.Action<string, string> OnNavigationMessage;

    void Start()
    {
        // 请求存储权限
        RequestStoragePermissions();
        
        // 地图默认隐藏，不自动加载
        Debug.Log("🗺️ 地图已初始化，等待关键词传入后显示");
    }
    
    // 请求存储权限
    private void RequestStoragePermissions()
    {
        #if UNITY_ANDROID
        // 检查并请求外部存储读取权限
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageRead))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.ExternalStorageRead);
            Debug.Log("📁 请求外部存储读取权限");
        }
        
        // 检查并请求外部存储写入权限
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageWrite))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.ExternalStorageWrite);
            Debug.Log("📁 请求外部存储写入权限");
        }
        
        // 对于 Android 11+，可能需要请求管理外部存储权限
        if (Application.platform == RuntimePlatform.Android && System.Environment.OSVersion.Version.Major >= 11)
        {
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.MANAGE_EXTERNAL_STORAGE"))
            {
                UnityEngine.Android.Permission.RequestUserPermission("android.permission.MANAGE_EXTERNAL_STORAGE");
                Debug.Log("📁 请求管理外部存储权限 (Android 11+)");
            }
        }
        #endif
    }

    // 加载地图（在确定导航目的地后调用）
    public void LoadMap()
    {
        LoadMapWithKeyword("");
    }

    // 加载带关键词的地图
    public void LoadMapWithKeyword(string keyword)
    {
        // 避免重复加载
        if (isMapLoaded)
        {
            ShowMap();
            return;
        }
        if (!Input.location.isEnabledByUser)
        {
            Input.location.Start();   // 触发系统定位权限弹窗
        }
        webView = gameObject.AddComponent<UniWebView>();

        // 加载本地地图，添加关键词参数
        string baseUrl = UniWebViewHelper.StreamingAssetURLForPath("map.html");
        string url = baseUrl;

        if (!string.IsNullOrEmpty(keyword))
        {
            string encodedKeyword = UnityWebRequest.EscapeURL(keyword);
            url = $"{baseUrl}?keyword={encodedKeyword}";
        }
        webView.Load(url);
        // 注册消息处理
        webView.OnMessageReceived += OnWebViewMessageReceived;
        // 屏幕中间显示
        float w = Screen.width * 0.25f;
        float h = Screen.height * 0.5f;
        webView.Frame = new Rect(
            (Screen.width - w),     // 双屏会割裂，看着来吧
            (Screen.height - h) / 7 * 6,
            w, h
        );
        // 背景透明
        webView.BackgroundColor = Color.clear;
        // 显示
        webView.Show();
        // 等待地图加载完成
        webView.OnPageFinished += (view, statusCode, urlStr) =>
        {
            if (statusCode == 200)
            {
                isMapLoaded = true;
                IPC.GPSPushCallBack += OnGPSMsgPush;
            }
        };
    }

    // 显示地图
    public void ShowMap()
    {
        if (webView != null)
        {
            webView.Show();
            Debug.Log("🗺️ 地图已显示");
        }
    }

    // 隐藏地图
    public void HideMap()
    {
        if (webView != null)
        {
            webView.Hide();
            Debug.Log("🗺️ 地图已隐藏");
        }
    }

    // 处理 WebView 消息
    private void OnWebViewMessageReceived(UniWebView webView, UniWebViewMessage message)
    {
        if (message.Path == "navigation")
        {
            string type = message.Args.ContainsKey("type") ? message.Args["type"] : "";
            string content = message.Args.ContainsKey("content") ? message.Args["content"] : "";

            // 通过事件通知 SearchController
            OnNavigationMessage?.Invoke(type, content);
        }
    }

    // RayNeo 每推送一次位置，就调用 JS 更新地图
    private void OnGPSMsgPush(long time, double latitude, double longitude, double altitude,
                              double speed, double horizontalAccuracyMeters, double AccuracyMeters)
    {
        if (webView == null) return;

        string js = $"updatePosition({latitude}, {longitude});";
        webView.EvaluateJavaScript(js, null);
    }

    void OnDestroy()
    {
        if (webView != null)
        {
            IPC.GPSPushCallBack -= OnGPSMsgPush;
            webView.Hide();
        }
    }
}
