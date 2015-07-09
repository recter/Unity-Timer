//*************************************************************************
//	创建日期:	2015-7-8
//	文件名称:	TimerSystem.cs
//  创 建 人:    Rect 	
//	版权所有:	MIT
//	说    明:	定时器系统 (Unity具有定时器功能的实现有好几种,请斟酌后使用)
//  关于这种定时器设计的分析文章: http://shadowkong.com/archives/1758
//*************************************************************************

//-------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CoreLib.Timer
{
    public delegate void DelITimerHander(uint uTimeID);  // 某个Timer的单个委托

    class CTimerHandle
    {
        
        #region Member variables
        //-------------------------------------------------------------------------
        public DelITimerHander m_Handle;   // 委托的集合,简称事件
        public int m_nHandleHashCode;       // 委托函数的哈希值
        public uint m_unTimerID;				// 用户自定义定时器ID
        public uint m_unIntervalTime;			// 触发间隔时间
        public uint m_unCallTimes;				// 调用次数

        public uint m_unLastCallTick;			// 最后一次被调用的时间
        public uint m_unTimerGridIndex;			// 所在的时间刻度
        public string m_strDebugInfo;           // 描述信息
        //-------------------------------------------------------------------------
        #endregion
        
        
        #region Public Method
        //-------------------------------------------------------------------------
        public CTimerHandle()
        {
            m_Handle = null;
            m_nHandleHashCode = 0;
            m_unTimerID = 0;
            m_unIntervalTime = 0;
            m_unCallTimes = 0;
            m_unLastCallTick = 0;
            m_unTimerGridIndex = 0;
            m_strDebugInfo = null;
        }
        //-------------------------------------------------------------------------
        #endregion
        
        
    }

    public class CTimerSystem
    {
        
        #region Member variables
        //-------------------------------------------------------------------------
        private Dictionary<uint, List<CTimerHandle>> m_TimerAxis; // 611 byte
        private Dictionary<int, List<uint>> m_TimerDict;

	    private uint	  m_unLastCheckTick;			    // 最后一次检查的时间
        private uint      m_unInitializeTime;			    // 时间轴初始时间
        private uint      m_unTimerAxisSize;                // 时间桶的总大小

        private const int gs_MAX_TIME_AXIS_LENGTH = 720000; // 最大时间轴长度
        private const int gs_DEFAULT_CHECK_FREQUENCY = 16;  // 默认检查频率 16ms
        private const int gs_DEFAULT_TIME_GRID = 64;        // 默认时间轴刻度
        //-------------------------------------------------------------------------
        #endregion
        
        #region Public Method
        //-------------------------------------------------------------------------
        public void Create()
        {
            m_unTimerAxisSize = (gs_MAX_TIME_AXIS_LENGTH / gs_DEFAULT_TIME_GRID - 1) / gs_DEFAULT_TIME_GRID;
            m_TimerAxis = new Dictionary<uint, List<CTimerHandle>>();
            m_TimerDict = new Dictionary<int, List<uint>>();
            m_unInitializeTime = this.__GetTickTime();
            m_unLastCheckTick = m_unInitializeTime;
        }
        //-------------------------------------------------------------------------
        public void Destroy()
        {
            m_TimerDict.Clear();
            m_TimerAxis.Clear();
        }
        //-------------------------------------------------------------------------
        /// <summary>
        /// 创建Timer
        /// </summary>
        /// <param name="nTimerID"></param>
        /// <param name="nIntervalTime"></param>
        /// <param name="del"></param>
        /// <param name="nCallTime"></param>
        public void CreateTimer(uint uTimerID, uint uIntervalTime, DelITimerHander del, uint uCallTime = 0xffffffff, string strInfo = "")
        {
            if (null == del)
            {
                return;
            }

            if( uIntervalTime == 0 )
		        uIntervalTime = 1;

            Debug.Log("CTimerSystem::CreateTimer del.GetHashCode() = " + del.GetHashCode());

            int nHashCode = del.GetHashCode();
            List<uint> singleList = null;
            if (m_TimerDict.TryGetValue(nHashCode,out singleList))
            {
                bool isExists = singleList.Exists(delegate(uint p) { return p == uTimerID; });
                if (isExists)
                {
                    return;
                }
                singleList.Add(uTimerID);
            }
            else
            {
                singleList = new List<uint>();
                singleList.Add(uTimerID);
                m_TimerDict.Add(nHashCode, singleList);
            }

            CTimerHandle sTimerHandle = new CTimerHandle();
            sTimerHandle.m_Handle = del;
            sTimerHandle.m_unTimerID = uTimerID;
            sTimerHandle.m_unCallTimes = uCallTime;
            sTimerHandle.m_unIntervalTime = uIntervalTime;
            sTimerHandle.m_unLastCallTick = m_unLastCheckTick;
            sTimerHandle.m_nHandleHashCode = nHashCode;
            if (!string.IsNullOrEmpty(strInfo))
            {
                sTimerHandle.m_strDebugInfo = strInfo;
            }
            sTimerHandle.m_unTimerGridIndex =
                ((sTimerHandle.m_unLastCallTick + sTimerHandle.m_unIntervalTime - m_unInitializeTime) / gs_DEFAULT_TIME_GRID) % m_unTimerAxisSize;

            List<CTimerHandle> axisList = null;
            if (!m_TimerAxis.TryGetValue(sTimerHandle.m_unTimerGridIndex,out axisList))
            {
                axisList = new List<CTimerHandle>();
                m_TimerAxis.Add(sTimerHandle.m_unTimerGridIndex, axisList);
            }
            if (null != axisList)
            {
                axisList.Add(sTimerHandle);
            }
            
        }
        //-------------------------------------------------------------------------
        /// <summary>
        /// 消除Timer
        /// </summary>
        /// <param name="nTimerID"></param>
        /// <param name="del"></param>
        public void DestroyTimer(uint uTimerID, DelITimerHander del)
        {
            if (null == del)
            {
                return;
            }

            Debug.Log("CTimerSystem::DestroyTimer del.GetHashCode() = " + del.GetHashCode());

            int nHashCode = del.GetHashCode();
            List<uint> singleList = null;
            if (m_TimerDict.TryGetValue(nHashCode, out singleList))
            {
                bool isExists = singleList.Exists(delegate(uint p) { return p == uTimerID; });
                if (!isExists)
                {
                    return;
                }
                
            }

            if (null == singleList)
            {
                return;
            }

            singleList.Remove(uTimerID);

            if (0 == singleList.Count)
            {
                m_TimerDict.Remove(nHashCode);
            }


        }
        //-------------------------------------------------------------------------
        /// <summary>
        /// 刷新
        /// </summary>
        public void UpdateTimer()
        {
            uint unCurTick = this.__GetTickTime();

            // 超过指定检查频率之后才检查
            if (unCurTick - m_unLastCheckTick < gs_DEFAULT_CHECK_FREQUENCY)
            {
                return;
            }

            uint unStartGrid = ((m_unLastCheckTick - m_unInitializeTime) / gs_DEFAULT_TIME_GRID) % m_unTimerAxisSize;
            uint unCurGrid = ((unCurTick - m_unInitializeTime) / gs_DEFAULT_TIME_GRID) % m_unTimerAxisSize;

	        // 记录当前Check时间
	        m_unLastCheckTick = unCurTick;

            uint i = unStartGrid;

            do 
            {
                __UpdateByKey(i, unCurTick);
                
                // 递进到下一个刻度
                if (i == unCurGrid)
                {
                    break;
                }
                    
                i = (i + 1) % m_unTimerAxisSize;

            } while (i != unCurGrid);
                
        }
        //-------------------------------------------------------------------------
        #endregion
        
        #region private Method
        //-------------------------------------------------------------------------
        /// <summary>
        /// 获取从应用启动到此刻的 毫秒数
        /// </summary>
        /// <returns></returns>
        private uint __GetTickTime()
        {
            return (uint)(1000 * Time.time);
        }
        //-------------------------------------------------------------------------
        /// <summary>
        /// 根据时间标识更新
        /// </summary>
        /// <param name="uKey"></param>
        /// <param name="unCurTick"></param>
        private void __UpdateByKey(uint uKey, uint unCurTick)
        {
            List<CTimerHandle> axisList = null;
            if (m_TimerAxis.TryGetValue(uKey, out axisList))
            {
                if (null == axisList || 0 == axisList.Count)
                {
                    m_TimerAxis.Remove(uKey);
                    axisList = null;
                }
            }
            if (null == axisList)
            {
                return;
            }

            CTimerHandle sTimerTemp = null;
            for (int nIndex = axisList.Count - 1; nIndex >= 0; --nIndex )
            {
                sTimerTemp = axisList[nIndex];
                if (!__CheckByHashCode(sTimerTemp.m_nHandleHashCode, sTimerTemp.m_unTimerID))
                {
                    axisList.RemoveAt(nIndex);
                    continue;
                }

                if (m_unLastCheckTick - sTimerTemp.m_unLastCallTick >= sTimerTemp.m_unIntervalTime)
                {
                    uint dwTick = __GetTickTime();
                    if (null != sTimerTemp.m_Handle)
                    {
                        sTimerTemp.m_Handle(sTimerTemp.m_unTimerID);
                    }

                    if (sTimerTemp == axisList[nIndex])
                    {
                        uint nCostTime = __GetTickTime() - dwTick;
                        if (nCostTime > 64 && nCostTime > gs_DEFAULT_TIME_GRID)
                        {
                            Debug.Log("CTimerSystem::__UpdateByKey - 定时器频率过低: ID = " + sTimerTemp.m_unTimerID);
                        }

                        sTimerTemp.m_unLastCallTick = unCurTick;
                        sTimerTemp.m_unCallTimes -= 1;

                        if (0 == sTimerTemp.m_unCallTimes)
                        {
                            DestroyTimer(sTimerTemp.m_unTimerID, sTimerTemp.m_Handle);
                        }
                        else
                        {
                            uint unNewGrid =
                            ((sTimerTemp.m_unLastCallTick + sTimerTemp.m_unIntervalTime - m_unInitializeTime) / gs_DEFAULT_TIME_GRID) % m_unTimerAxisSize;

                            if (sTimerTemp.m_unTimerGridIndex == unNewGrid)
                            {
                                continue;
                            }
                            sTimerTemp.m_unTimerGridIndex = unNewGrid;

                            axisList.RemoveAt(nIndex);

                            List<CTimerHandle> axisListNew = null;
                            if (!m_TimerAxis.TryGetValue(sTimerTemp.m_unTimerGridIndex, out axisListNew))
                            {
                                axisListNew = new List<CTimerHandle>();
                                m_TimerAxis.Add(sTimerTemp.m_unTimerGridIndex, axisListNew);
                            }
                            if (null != axisListNew)
                            {
                                axisListNew.Add(sTimerTemp);
                            }
                        }

                    }
                    else
                    {
                        axisList.RemoveAt(nIndex);
                        continue;
                    }
                }
            }

        }
        //-------------------------------------------------------------------------
        /// <summary>
        /// 根据函数哈希值判断此时间ID是否存在
        /// </summary>
        /// <param name="nHashCode"></param>
        /// <param name="uTimerID"></param>
        /// <returns></returns>
        private bool __CheckByHashCode(int nHashCode, uint uTimerID)
        {
            List<uint> singleList = null;
            if (m_TimerDict.TryGetValue(nHashCode, out singleList))
            {
                bool isExists = singleList.Exists(delegate(uint p) { return p == uTimerID; });
                if (isExists)
                {
                    return true;
                }
            }
            return false;
        }
        //-------------------------------------------------------------------------
        #endregion
    }
}
