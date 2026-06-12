using UnityEngine;
using Kogarasi.WebView;

public class MyWebViewCallback : MonoBehaviour, IWebViewCallback
{
    public void onLoadStart(string url)
    {
        Debug.Log("开始加载: " + url);
    }

    public void onLoadFinish(string url)
    {
        Debug.Log("加载完成: " + url);
    }

    public void onLoadFail(string url)
    {
        Debug.Log("加载失败: " + url);
    }
}
