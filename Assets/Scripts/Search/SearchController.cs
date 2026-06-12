using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using RayNeo.API;
using System;
using RayNeo;
using UnityEngine.Android;
using UnityEngine.Events;  

public class SearchController : MonoBehaviour
{
    [Header("UI 组件")]
    public Text Transcript;
    public Text SearchResults;
    public Button searchButton;         // 拖入 SearchButton
    public Text searchButtonText;       // 拖入 SearchButton 里的 Text
    
    public GameObject searchPanel;        // 拖入你的 SearchPanel（用于隐藏/显示）
    public GameObject searchButtonPanel;   // 拖入你的 SearchButtonPanel（用于隐藏/显示）

    [Header("地图 WebView")]
    public TencentMapWebView mapController;

    [Header("语音识别")]
    public BaiduASRManager baiduASRManager;  // 拖入你的 BaiduASRManager 组件

    [Header("指南针")]
    public GameObject compass;  // 拖入你的 Compass 组件

    [Header("GPS 文本")]
    public Text gpsText;  // 拖入你的 GPStext 组件

    private UniWebView webView;
    private bool isNavigating = false;  // 导航状态标志
    private bool isRecording = false;  // 录音状态标志
    private bool gpsTextVisible = true;  // GPS 文本显示状态（默认显示）

    void Start()
    {
        // 默认隐藏指南针
        if (compass != null)
        {
            compass.SetActive(false);
            LogDebug("🧭 指南针已隐藏");
        }

        // 默认显示 GPS 文本
        if (gpsText != null)
        {
            gpsText.gameObject.SetActive(true);
            LogDebug("📍 GPS 文本已显示");
        }

        // 点击搜索按钮时执行搜索
        searchButton.onClick.AddListener(OnSearchButtonClicked);

        // 订阅地图控制器的消息事件
        if (mapController != null)
        {
            mapController.OnNavigationMessage += OnNavigationMessageReceived;
        }

        // 订阅百度语音识别的结果事件
        if (baiduASRManager != null)
        {
            baiduASRManager.OnRecognitionResult.AddListener(OnRecognitionResult);
            LogDebug("百度语音识别事件已注册");
        }

        // 添加单击事件（用于搜索）
        if (SimpleTouch.SingletonExist)
        {
            SimpleTouch.Instance.OnSimpleTap.AddListener(TriggerSearchByClick);
        }

        // 添加三击事件（用于确认导航）
        if (SimpleTouch.SingletonExist)
        {
            SimpleTouch.Instance.OnTripleTap.AddListener(OnTripleTapConfirmNavigation);
        }

        // 添加右滑结束事件（用于输入固定关键词）
        if (SimpleTouch.SingletonExist)
        {
            SimpleTouch.Instance.OnSwipeRightEnd.AddListener(OnSwipeRightEnd);
        }

        // 添加左滑事件（用于显示/隐藏 GPS 文本）
        if (SimpleTouch.SingletonExist)
        {
            SimpleTouch.Instance.OnSwipeLeftEnd.AddListener(OnSwipeLeftEnd);
        }
    }

    // 点击搜索按钮
    private void OnSearchButtonClicked()
    {
        string keyword = Transcript.text.Trim();
        if (string.IsNullOrEmpty(keyword)) return;

        // 执行搜索和导航
        ExecuteSearchAndNavigate(keyword);
    }

    // 左滑结束事件 显示/隐藏 GPS 文本
    private void OnSwipeLeftEnd(Vector2 position)
    {
        if (gpsText != null)
        {
            gpsTextVisible = !gpsTextVisible;
            gpsText.gameObject.SetActive(gpsTextVisible);
        }
    }

    // 接收地图控制器的消息
    private void OnNavigationMessageReceived(string type, string content)
    {
        LogDebug($"📨 收到导航消息: type={type}, content={content}");

        if (type == "searchResults")
        {
            LogDebug($"接收到搜索结果: {content}");
            DisplaySearchResult(content);
        }
        else if (type == "navigationStarted")
        {
            LogDebug($"✅ 导航已启动，目的地：{content}");
            isNavigating = true;

            // 显示指南针
            if (compass != null)
            {
                compass.SetActive(true);
                LogDebug("🧭 指南针已显示");
            }

            // 停止语音输入
            if (baiduASRManager != null)
            {
                // 百度语音识别没有IsRecording属性，直接调用停止方法
                LogDebug("✅ 导航启动，已停止语音输入");
            }

            // 解析地点信息 JSON
            try
            {
                var placeInfo = JsonUtility.FromJson<PlaceInfo>(content);
                if (placeInfo != null && SearchResults != null)
                {
                    SearchResults.text = $"{placeInfo.title}\n距离: {placeInfo.distance}\n预计时间: {placeInfo.duration}";
                    LogDebug($"✅ SearchResults 已更新: {placeInfo.title} - {placeInfo.distance} - {placeInfo.duration}");
                }
            }
            catch (Exception e)
            {
                LogError($"地点信息解析失败: {e.Message}");
            }
        }
        else if (type == "noResult")
        {
            LogError($"❌ 未找到地点: {content}");
            if (SearchResults != null)
                SearchResults.text = "未找到地点";
        }
        else if (type == "searchError")
        {
            LogError($"搜索失败: {content}");
        }
        else if (type == "mapReady")
        {
            LogDebug($"地图初始化完成，准备导航");
        }
        else if (type == "navigationError")
        {
            LogError($"导航失败: {content}");
        }
    }

    // 显示搜索结果到 SearchResults 文本
    private void DisplaySearchResult(string resultsJson)
    {
        if (SearchResults != null)
        {
            try
            {
                // 解析 JSON 结果
                var results = JsonUtility.FromJson<SearchResultsData>("{\"results\": " + resultsJson + "}");
                
                if (results != null && results.results.Length > 0)
                {
                    // 显示第一个搜索结果
                    var firstResult = results.results[0];
                    string resultText = $"目的地: {firstResult.title}\n地址: {firstResult.address}";
                    SearchResults.text = resultText;
                    LogDebug("已显示搜索结果: " + resultText);
                    
                    // 自动选择第一个结果作为目的地
                    AutoSelectDestination(firstResult.title);
                }
                else
                {
                    SearchResults.text = "未找到搜索结果";
                    LogDebug("未找到搜索结果");
                }
            }
            catch (Exception e)
            {
                SearchResults.text = "搜索结果解析失败";
                LogError("搜索结果解析失败: " + e.Message);
            }
        }
        else
        {
            LogError("SearchResults 未赋值，无法显示搜索结果");
        }
    }

    // 自动选择目的地
    private void AutoSelectDestination(string destinationName)
    {
        LogDebug("自动选择目的地: " + destinationName);
        
        // 将目的地名称显示在搜索结果中，方便用户确认
        //Transcript.text = destinationName;
        SearchResults.text = destinationName;
        
        // 可以在这里添加其他自动选择逻辑
        // 例如：自动开始导航、显示确认提示等
        
        LogDebug("已自动选择目的地: " + destinationName);
    }

    // 搜索结果数据结构
    [System.Serializable]
    private class SearchResultsData
    {
        public SearchResultItem[] results;
    }

    [System.Serializable]
    private class SearchResultItem
    {
        public string title;
        public string address;
    }

    [System.Serializable]
    private class PlaceInfo
    {
        public string title;
        public string address;
        public string distance;
        public string duration;
    }

    // 单击 第一次点击开始语音输入，第二次点击确认搜索
    public void TriggerSearchByClick()
    {
        if (isNavigating)
        {
            return;
        }

        if (!isRecording)
        {
            // 第一次点击：开始语音输入
            if (baiduASRManager != null)
            {
                baiduASRManager.StartRecord();
                isRecording = true;
                
                // 显示提示信息
                if (searchButtonText != null)
                {
                    searchButtonText.text = "正在录音...";
                }
            }
        }
        else
        {
            // 第二次点击：确认搜索
            if (baiduASRManager != null)
            {
                baiduASRManager.StopRecordAndRecognize();
                isRecording = false;
            }
        }
    }

    // 雷鸟三击 存储关键词并开始导航
    private void OnTripleTapConfirmNavigation()
    {
        string keyword = SearchResults != null ? SearchResults.text.Trim() : "";
        if (string.IsNullOrEmpty(keyword) && Transcript != null)
            keyword = Transcript.text.Trim();

        if (string.IsNullOrEmpty(keyword)) return;
        // 重置录音状态
        isRecording = false;
        // 恢复搜索按钮文本
        if (searchButtonText != null)
        {
            searchButtonText.text = "单击进行语音输入";
        }
        // 确保地图已加载
        if (mapController == null)
        {
            return;
        }
        // 加载带关键词的地图
        mapController.LoadMapWithKeyword(keyword);

        if (searchPanel != null) searchPanel.SetActive(false);
        if (searchButtonPanel != null) searchButtonPanel.SetActive(false);
    }

    // 延迟执行导航，确保 WebView 加载完成
    private IEnumerator DelayedNavigation(string keyword)
    {
        yield return new WaitForSeconds(1.5f);
        webView = mapController.GetComponent<UniWebView>();
        if (webView != null)
        {
            LogDebug("✅ WebView 已加载，开始导航");
            ExecuteSearchAndNavigate(keyword);
        }
        else
        {
            LogError("❌ WebView 加载失败，无法导航");
        }
    }

    // 右滑结束 输入固定关键词
    private void OnSwipeRightEnd(Vector2 position)
    {
        string fixedKeyword = "年年丰";
        Transcript.text = fixedKeyword;

        ExecuteSearchAndNavigate(fixedKeyword);
    }

    private void LogDebug(string message)
    {
        Debug.Log(message);
    }

    private void LogError(string message)
    {
        Debug.LogError(message);
    }

    // 【新增】统一安全的 JS 调用方法
    private void ExecuteSearchAndNavigate(string keyword)
    {
        if (mapController == null)
        {
            LogError("mapController 未赋值");
            return;
        }

        // 每次都重新获取最新 WebView（解决 Start() 时机问题）
        webView = mapController.GetComponent<UniWebView>();
        if (webView == null)
        {
            LogError("WebView 未找到，无法执行搜索");
            return;
        }

        // 安全转义：防止单引号等特殊字符破坏 JS 字符串
        string safeKeyword = keyword.Replace("'", "\\'").Replace("\"", "\\\"");
        
        // 先更新 map.html 上的关键词显示
        string updateDisplayJs = $"const display = document.getElementById('keyword-display'); if (display) display.textContent = '搜索关键词: {safeKeyword}';";
        webView.EvaluateJavaScript(updateDisplayJs, null);
        LogDebug("✅ 已更新 map.html 上的关键词显示");
        
        // 再执行搜索和导航
        string js = $"searchAndNavigate(\"{safeKeyword}\");";
        
        LogDebug("即将执行 JS: " + js);

        // 带回调，确认 JS 是否真的执行
        webView.EvaluateJavaScript(js, (result) =>
        {
            LogDebug($"✅ JS 执行完成，返回结果: {result}");
        });
    }

    // 双击 → 取消路线
    public void TriggerCancelNavigation()
    {
        isNavigating = false;

        // 隐藏指南针
        if (compass != null)
        {
            compass.SetActive(false);
            LogDebug("🧭 指南针已隐藏");
        }

        if (mapController != null)
        {
            // mapController.HideMap();  // 隐藏地图（暂时注释掉）
            LogDebug("🗺️ 取消导航，地图保持显示");
        }
        // 暂时注释掉显示功能，方便测试
        if (searchPanel != null) searchPanel.SetActive(true);    // 显示搜索栏
        if (searchButtonPanel != null) searchButtonPanel.SetActive(true);    // 显示搜索按钮栏
    }

    // 处理百度语音识别结果
    private void OnRecognitionResult(string result)
    {
        // 去除末尾的句号
        string cleanedResult = result.Trim();
        if (cleanedResult.EndsWith("。"))
        {
            cleanedResult = cleanedResult.Substring(0, cleanedResult.Length - 1);
        }
        else if (cleanedResult.EndsWith("."))
        {
            cleanedResult = cleanedResult.Substring(0, cleanedResult.Length - 1);
        }
        if (Transcript != null)
        {
            Transcript.text = cleanedResult;
        }
        // 自动显示识别结果到 SearchResults
        if (SearchResults != null)
        {
            SearchResults.text = cleanedResult;
        }
        // 恢复搜索按钮文本
        if (searchButtonText != null)
        {
            searchButtonText.text = "单击进行语音输入";
        }
    }

    void OnDestroy()
    {
        // 取消订阅地图控制器的消息事件
        if (mapController != null)
        {
            mapController.OnNavigationMessage -= OnNavigationMessageReceived;
        }

        // 取消订阅百度语音识别的结果事件
        if (baiduASRManager != null)
        {
            baiduASRManager.OnRecognitionResult.RemoveListener(OnRecognitionResult);
        }

        // 移除单击事件
        if (SimpleTouch.SingletonExist)
        {
            SimpleTouch.Instance.OnSimpleTap.RemoveListener(TriggerSearchByClick);
        }

        // 移除三击事件
        if (SimpleTouch.SingletonExist)
        {
            SimpleTouch.Instance.OnTripleTap.RemoveListener(OnTripleTapConfirmNavigation);
        }

        // 移除右滑结束事件
        if (SimpleTouch.SingletonExist)
        {
            SimpleTouch.Instance.OnSwipeRightEnd.RemoveListener(OnSwipeRightEnd);
        }

        // 移除左滑结束事件
        if (SimpleTouch.SingletonExist)
        {
            SimpleTouch.Instance.OnSwipeLeftEnd.RemoveListener(OnSwipeLeftEnd);
        }
    }
}
