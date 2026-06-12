using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Baidu.Aip.Speech;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine.Events;  

public class BaiduASRManager : MonoBehaviour
{
    [Header("百度语音识别密钥")]
    public string APP_ID = "122659705";
    public string API_KEY = "kY3em8edZLcEq1Hxxn5duHZO";
    public string SECRET_KEY = "vDaqJbb6D6beQ4Fb61VrLQSoXIGeodoB";

    private Asr client;
    private AudioClip recordingClip;
    private bool isRecording = false;

    [Header("识别设置")]
    public int maxRecordSeconds = 10;
    public int sampleRate = 16000;

    [Header("识别完成事件")]
    public UnityEvent<string> OnRecognitionResult = new UnityEvent<string>();

    void Awake()
    {
        // 初始化客户端
        client = new Asr(APP_ID, API_KEY, SECRET_KEY);
        client.Timeout = 60000;
    }
    /// <summary>
    /// 开始录音
    /// </summary>
    public void StartRecord()
    {
        if (isRecording) return;
        isRecording = true;
        recordingClip = Microphone.Start(null, false, maxRecordSeconds, sampleRate);
    }
    /// <summary>
    /// 停止录音并识别
    /// </summary>
    public void StopRecordAndRecognize()
    {
        if (!isRecording) return;
        Microphone.End(null);
        isRecording = false;

        if (recordingClip == null) return;

        byte[] pcmData = ConvertAudioClipToPCM(recordingClip);
        StartCoroutine(RecognizeCoroutine(pcmData));
    }

    private IEnumerator RecognizeCoroutine(byte[] pcmData)
    {
        var options = new Dictionary<string, object> { { "dev_pid", 1537 } };

        var result = client.Recognize(pcmData, "pcm", sampleRate, options);

        if (result != null && result.ContainsKey("err_no") && (int)result["err_no"] == 0)
        {
            var resList = result["result"] as JArray;
            string text = resList != null && resList.Count > 0 ? resList[0].ToString() : "（无识别结果）";

            OnRecognitionResult.Invoke(text); 
        }
        else
        {
            string errMsg = result != null && result.ContainsKey("err_msg") ? result["err_msg"].ToString() : "未知错误";
        }

        yield return null;
    }

    private byte[] ConvertAudioClipToPCM(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        byte[] pcm = new byte[samples.Length * 2];
        int index = 0;
        foreach (float sample in samples)
        {
            short value = (short)(sample * 32767);
            pcm[index++] = (byte)(value & 0xFF);
            pcm[index++] = (byte)(value >> 8);
        }
        return pcm;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) StartRecord();
        if (Input.GetKeyDown(KeyCode.T)) StopRecordAndRecognize();
    }
}