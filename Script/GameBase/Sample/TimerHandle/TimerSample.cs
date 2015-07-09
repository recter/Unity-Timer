//*************************************************************************
//	创建日期:	2015-7-8
//	文件名称:	TimerSample.cs
//  创 建 人:   邓观福 Rect 	
//	版权所有:	MIT
//	说    明:	
//*************************************************************************

//-------------------------------------------------------------------------

using UnityEngine;
using System.Collections;
using CoreLib.Timer;

public class TimerSample : MonoBehaviour 
{
    public UILabel m_DesLabel;

    private CTimerSystem m_TimerSystem = null;
	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{
        if (null != m_TimerSystem)
	    {
            m_TimerSystem.UpdateTimer();
	    }
	    
	}
    void OnDestroy()
    {
        if (null != m_TimerSystem)
        {
            m_TimerSystem.Destroy();
        }
    }
    //-------------------------------------------------------------------------
    void LaunchProjectile()
    {
        if (null != m_DesLabel)
        {
            m_DesLabel.text += "\n执行:LaunchProjectile - " + Time.time;
            Debug.Log("LaunchProjectile - " + Time.time);
        }
        
    }
    //-------------------------------------------------------------------------
    public void OnTimer1()
    {
        CancelInvoke();
        if (null != m_DesLabel && !IsInvoking("LaunchProjectile"))
        {
            m_DesLabel.text = "2秒后执行,开始:OnTimer1 - " + Time.time;
            Debug.Log("OnTimer1 - " + Time.time);
            Invoke("LaunchProjectile", 2);
        }
    }
    //-------------------------------------------------------------------------
    public void OnTimer2()
    {
        CancelInvoke();
        if (null != m_DesLabel && !IsInvoking("LaunchProjectile"))
        {
            m_DesLabel.text = "1秒后每隔一秒执行一次,开始:OnTimer2 - " + Time.time;
            Debug.Log("OnTimer2 - " + Time.time);
            InvokeRepeating("LaunchProjectile", 1, 1F);  
        }
    }
    //-------------------------------------------------------------------------
    IEnumerator __TimerHandle()
    {
        yield return new WaitForSeconds(1);
        m_DesLabel.text += "\n执行:__TimerHandle - " + Time.time;
    }
    //-------------------------------------------------------------------------
    public void OnTimer3()
    {
        CancelInvoke();
        StopAllCoroutines();
        if (null != m_DesLabel)
        {
            m_DesLabel.text = "开始:OnTimer3 - " + Time.time;
            Debug.Log("OnTimer3 - " + Time.time);
            StartCoroutine("__TimerHandle");
        }
    }
    //-------------------------------------------------------------------------
    public void OnCreateTimer4()
    {
        CancelInvoke();
        if (null != m_DesLabel)
        {
            m_DesLabel.text = "开始:OnCreateTimer4 - " + Time.time * 1000;
            Debug.Log("OnCreateTimer4 - " + Time.time * 1000);
        }

        if (null == m_TimerSystem)
        {
            m_TimerSystem = new CTimerSystem();
            m_TimerSystem.Create();
            
        }

        m_TimerSystem.CreateTimer(1, 1000, OnTimeTest1);
        m_TimerSystem.CreateTimer(2, 1200, OnTimeTest1);

        m_TimerSystem.CreateTimer(1, 1000, OnTimeTest2);
        
    }
    //-------------------------------------------------------------------------
    public void OnDistoryTimer()
    {
        if (null != m_TimerSystem)
        {
            m_TimerSystem.DestroyTimer(1, OnTimeTest1);
            m_TimerSystem.DestroyTimer(2, OnTimeTest1);

            m_TimerSystem.DestroyTimer(1, OnTimeTest2);
        }
    }
    //-------------------------------------------------------------------------
    public void OnTimeTest1(uint nTimeID)
    {
        Debug.Log("OnTimeTest1 - " + Time.time * 1000);
        m_DesLabel.text += "\n执行:OnTimeTest1 - " + Time.time * 1000 + " TimeID = " + nTimeID;
    }
    public void OnTimeTest2(uint nTimeID)
    {
        Debug.Log("OnTimeTest2 - " + Time.time * 1000);
        m_DesLabel.text += "\n执行:OnTimeTest2 - " + Time.time * 1000 + " TimeID = " + nTimeID;
    }
}
