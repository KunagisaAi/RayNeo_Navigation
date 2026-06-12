using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RayNeo.API;

public class GPS : MonoBehaviour
{
    public Text m_gpsInfo;
    private PhoneGPSResultType m_state = PhoneGPSResultType.UNKNOW;
    private string m_gpsMsg;

    // Start is called before the first frame update
    void Start()
    {
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.FineLocation))
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.FineLocation);

        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.CoarseLocation))
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.CoarseLocation);
        // PermissionUtil.TryQueryPermission(UnityEngine.Android.Permission.FineLocation);
        // PermissionUtil.TryQueryPermission(UnityEngine.Android.Permission.CoarseLocation);
        IPC.OpenPhoneGPS();
        IPC.GpsStateChagneCallBack += GPSStateChange;
        IPC.GPSPushCallBack += GPSMsgPush;
    }

    private void OnDestroy()
    {
        IPC.ClosePhoneGPS();
    }

    // Update is called once per frame
    void Update()
    {
        if (m_state == PhoneGPSResultType.UNKNOW)
        {
            m_gpsInfo.text = "No GPS Data";
        }
        else
        {
            m_gpsInfo.text = "gps state:" + m_state + "\n" + m_gpsMsg;
        }
    }

    private void GPSStateChange(PhoneGPSResultType code, string msg)
    {
        m_state = code;
    }

    public void GPSMsgPush(long time, double latitude, double longitude, double altitude, double speed,
                           double horizontalAccuracyMeters, double AccuracyMeters)
    {
        m_gpsMsg = $"GPS : \nlongitude={longitude},\naltitude={altitude},\ntime={time},\nlatitude={latitude},\nspeed={speed}"; 
        m_state = PhoneGPSResultType.PHONE_CONNECTED;
    }
}