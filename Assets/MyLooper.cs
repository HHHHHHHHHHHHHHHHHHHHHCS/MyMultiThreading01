using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Timers; //注意命名空间
using UnityEngine;
using UnityEngine.Analytics;

public class MyLooper : MonoBehaviour
{
    private const long c_oneSecond = 10000000; //1e+7 ticks的一秒比例
    private const int c_maxCount = 1000000000; //1e+9 looper的循环量
    private const long c_destroyTime = 5 * c_oneSecond; //自我删除倒计时
    private const double c_waiteDeltaTime = 50d; //每一次Loop 等待的倒计时 0.05s  单位是微秒

    private class LoopAction
    {
        /// <summary>
        /// 循环的ID  最后要取余c_macCount
        /// </summary>
        public static int AutoActId { get; private set; } = 0;

        public int actId;
        public long endTime;
        public Action act;

        public bool IsSucceed => nowTime >= endTime;

        public static int AddAutoActId()
        {
            AutoActId = (AutoActId + 1) % c_maxCount;
            return AutoActId;
        }

        public LoopAction(float _time, Action _act)
        {
            actId = AutoActId;
            endTime = nowTime + (long) (_time * c_oneSecond);
            act = _act;
        }
    }

    private static MyLooper inst;
    private static bool isInit => inst != null;

    public static long nowTime => DateTime.Now.Ticks; //用的是本地时间 可以被修改(作弊)  而且不受Unity时间影响等

    private GameObject instGo;
    private long destroySelfTime; //自毁倒计时
    private ConcurrentDictionary<int, LoopAction> actDict; //线程安全的字典
    private List<int> removeList; //这里不用是因为删除都在Unity住线程里面
    private Timer timer; //不用Unity主线程的原因是  Update可能存在检测不稳定 FixedUpdate 受到fixedTime 影响  但是最后还是要回归的
    private bool isFirst; //是否是第一轮 即没有老的数据
    private bool haveTimeOver; //是否有时间到了
    private bool needDestroy; //是否需要销毁了

    private static void InitGo()
    {
        if (!isInit)
        {
            new GameObject("MyLoooper").AddComponent<MyLooper>().Init();
        }
    }

    private void Init()
    {
        if (!isInit)
        {
            inst = this;
            instGo = gameObject;
            DontDestroyOnLoad(instGo);
            actDict = new ConcurrentDictionary<int, LoopAction>();
            removeList = new List<int>();
            isFirst = true;
            destroySelfTime = nowTime + c_destroyTime;
            timer = new Timer(c_waiteDeltaTime);
            timer.Elapsed += UpdateTimer;
            timer.Start();
        }
    }

    private void Update()
    {
        DoUpdate();
    }

    private void FixedUpdate()
    {
        DoUpdate();
    }

    private void UpdateTimer(object sender, ElapsedEventArgs e)
    {
        if (needDestroy)
        {
            return;
        }

        if (actDict.Count > 0)
        {
            destroySelfTime = nowTime + c_destroyTime;

            foreach (var item in actDict)
            {
                if (item.Value.IsSucceed)
                {
                    haveTimeOver = true;
                    return;
                }
            }
        }
        else
        {
            if (!isFirst)
            {
                //没有数据了 设置成第一轮  但是不重置Id  因为可能存在 别的地方想要删除他
                isFirst = true;
            }

            if (nowTime >= destroySelfTime)
            {
                timer.Stop();
                needDestroy = true;
            }
        }
    }

    private void DoUpdate()
    {
        if (needDestroy)
        {
            DoDestroy();
            return;
        }

        if (!haveTimeOver)
        {
            return;
        }

        haveTimeOver = false;

        foreach (var item in actDict)
        {
            if (item.Value.IsSucceed)
            {
                removeList.Add(item.Key);
                item.Value.act();
            }
        }

        foreach (var item in removeList)
        {
            actDict.TryRemove(item, out var _);
        }

        removeList.Clear();
    }

    /// <summary>
    /// 怕额外情况删除
    /// </summary>
    private void OnDestroy()
    {
        if (inst)
        {
            DoDestroy(true);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="isUnity">Unity自己OnDestroy释放的</param>
    private void DoDestroy(bool isUnity = false)
    {
        timer?.Close();
        timer = null;
        removeList?.Clear();
        removeList = null;
        actDict?.Clear();
        actDict = null;
        
        //如果不是Unity 删除的则要自己删除
        if (!isUnity)
        {
            DestroyImmediate(instGo);
        }

        instGo = null;
        inst = null;
    }

    /// <summary>
    /// 计算轮次
    /// </summary>
    private void AutoAddId()
    {
        var key = LoopAction.AddAutoActId();

        if (key == 0)
        {//刚进入第二轮
            isFirst = false;
            return;
        }

        if (isFirst) return;//还在第一轮
        //这里不同while是避免死循环
        for (int i = 0; i < c_maxCount; i++)
        {
            if (actDict.ContainsKey(key))
            {
                key = LoopAction.AddAutoActId();
            }
            else
            {
                break;
            }
        }
    }

    public static int Call(float time, Action act)
    {
        InitGo();
        inst.AutoAddId();
        var loopAct = new LoopAction(time,act);
        inst.actDict.TryAdd(loopAct.actId, loopAct);
        if (time <= 0)
        {//如果设置的时间小于等于零 默认是下一帧数执行
            inst.haveTimeOver = true;
        }

        return loopAct.actId;
    }

    public static bool Remove(int actId)
    {
        return !inst || inst.actDict.TryRemove(actId, out var _);
    }
}