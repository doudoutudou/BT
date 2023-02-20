using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AssemblyCSharp.Framework
{
    /*
     * --[[
    -- added by yh @ 2023-01-13
    -- SYSCoroutineUtil：重写 携程 SYS开头好记点
    -- 注意：
    -- 1、资源预加载放各个场景类中自行控制
    -- 2、场景loading的UI窗口这里统一管理，由于这个窗口很简单，更新进度数据时直接写Model层
    --]]
     */
    public class SYSCoroutineUtil
    {
        private static GameObject gameObject;
        private static MonoCoroutine mono;
        //模块携程栈
        private static Dictionary<string, Dictionary<string, IEnumerator>> module_stack_list =
            new Dictionary<string, Dictionary<string, IEnumerator>>();
        
        
        public static void M_config(GameObject _gameObject)
        {
            gameObject = _gameObject;
            mono = gameObject.AddComponent<MonoCoroutine>();
        }
        /*
          use reference
         *  IEnumerator er = CoDownloadAssetBundle(path,progress_callback,finish_callback);
            SYSCoroutineUtil.M_startCoroutine(er);
         */
        public static Coroutine M_startCoroutine(IEnumerator _er)
        {
            return mono.StartCoroutine(_er);
        }
        /*
         *携程栈
         *功能描述：目的是解决模块携程的 整体 干掉
         * 注意: 使用StartCoroutine 必须要与StopCoroutine 成对 否则导致一直被引用
         */
        public static void StartCoroutine(string module_name,string key,IEnumerator _er)
        {
            UIUtil.Assert(!string.IsNullOrEmpty(module_name),"StartCoroutine module_name is null");
            Dictionary<string, IEnumerator> module_stack = null;
            if(!module_stack_list.TryGetValue(module_name,out module_stack))
            {
                module_stack = new Dictionary<string, IEnumerator>();//new ModuleCoroutineStack();
                module_stack_list.Add(module_name,module_stack);
            }
            
            if (string.IsNullOrEmpty(key))
            {
                key = "Default_" + _er.GetHashCode();
            }

            IEnumerator obj = null;
            if (module_stack.TryGetValue(key, out obj))
            {
                Debug.LogError("StartCoroutine module_name="+module_name+" key="+key +" repeat !!! 已经有一个一样的");
                return;
            }
            
            module_stack.Add(key,_er);
            mono.StartCoroutine(_er);

        }

        public static void StopCoroutine(string module_name, string key="")
        {
            UIUtil.Assert(!string.IsNullOrEmpty(module_name),"StopCoroutine module_name is null");
            Dictionary<string, IEnumerator> module_stack = null;
            if(module_stack_list.TryGetValue(module_name,out module_stack))
            {
                if (!string.IsNullOrEmpty(key))
                {
                    IEnumerator _er = null;
                    if (module_stack.TryGetValue(key, out _er))
                    {
                        mono.StopCoroutine(_er);
                        module_stack.Remove(key);
                    }
                    
                    if (module_stack.Count == 0)
                    {
                        module_stack_list.Remove(module_name);
                        module_stack = null;
                    }
                }
                else
                {
                    //全部删掉
                    bool wait_condition = false;
                    do{
                        if (module_stack.Count == 0)
                        {
                            wait_condition =true;// 跳出循环 
                        }
                        else
                        {
                            var t = module_stack.First();
                            mono.StopCoroutine(t.Value);
                            module_stack.Remove(t.Key);
                        }

                    } while (!wait_condition);
                    
                    module_stack_list.Remove(module_name);
                    module_stack = null;
                    
                }
            }
            
            
        }

        /*
        private static MonoBehaviour M_get(byte _coroutine_id = 0, bool _create_if_null = true)
        {
        MonoBehaviour mono;
        if (!dict_mono.TryGetValue(_coroutine_id, out mono))
        {
        if (_create_if_null)
        {
            mono = gameObject.AddComponent<MonoCoroutine>();
            dict_mono.Add(_coroutine_id, mono);
        }
        }
        return mono;
        }*/
        //
        public static IEnumerator waitforframes(int _frame)
        {
            while (_frame-- > 0)
            {
                yield return 1;
            }
        }
        //等待0.8f秒，一段指定的时间延迟之后继续执行，在所有的Update函数完成调用的那一帧之后（这里的时间会受到Time.timeScale的影响）;
        //会受到Time.timeScale的影响
        public static IEnumerator WaitForSeconds(float _time)
        {
            yield return new WaitForSeconds(_time);
        }
        //等待0.3秒，一段指定的时间延迟之后继续执行，在所有的Update函数完成调用的那一帧之后（这里的时间不受到Time.timeScale的影响）;
        //不受到Time.timeScale的影响
        public static IEnumerator WaitForSecondsRealtime(float _time)
        {
            yield return new WaitForSecondsRealtime(_time);
        }
        //等待帧结束,等待直到所有的摄像机和GUI被渲染完成后，在该帧显示在屏幕之前执行
        public static IEnumerator WaitForEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
        }

        //等待下一次FixedUpdate开始时再执行后续代码
        public static IEnumerator WaitForFixedUpdate()
        { 
            yield return new WaitForFixedUpdate();
        }
        // isbool = true;跳出循环 将协同执行直到 当输入的参数（或者委托）为true的时候....如:yield return new WaitUntil(() => frame >= 18);
        //辉哥提醒 说人话就是 isbool = true 跳出循环 
        public static IEnumerator WaitUntil(Func<bool> callback)
        {
            bool isbool = false;
            yield return new WaitUntil(() =>
            {
                isbool = callback();
                return isbool;
            });
        }
        //将协同执行直到 当输入的参数（或者委托）为false的时候.... 如:yield return new WaitWhile(() => frame < 18);
        //辉哥提醒 说人话就是 isbool = false 跳出循环 
        public static IEnumerator WaitWhile(Func<bool> callback)
        {
            bool isbool = false;
            yield return new WaitWhile(() =>
            {
                isbool = callback();
                return isbool;
            });
        }
        
        /*
        yield return new WaitForEndOfFrame();//等待帧结束,等待直到所有的摄像机和GUI被渲染完成后，在该帧显示在屏幕之前执行
        yield return new WaitForSeconds(0.8f);//等待0.8秒，一段指定的时间延迟之后继续执行，在所有的Update函数完成调用的那一帧之后（这里的时间会受到Time.timeScale的影响）;
        yield return new WaitForSecondsRealtime(0.8f);//等待0.3秒，一段指定的时间延迟之后继续执行，在所有的Update函数完成调用的那一帧之后（这里的时间不受到Time.timeScale的影响）;
        yield return WaitForFixedUpdate();//等待下一次FixedUpdate开始时再执行后续代码
        yield return new WaitUntil()//将协同执行直到 当输入的参数（或者委托）为true的时候....如:yield return new WaitUntil(() => frame >= 18);
        yield return new WaitWhile()//将协同执行直到 当输入的参数（或者委托）为false的时候.... 如:yield return new WaitWhile(() => frame < 18);
         */
        
        public static void DoUpdate(int updateInterval, Action downloadUpdater)
        {
            IEnumerator CoDoUpdate()
            {
                bool wait_condition = false;
                do
                {
                    yield return SYSCoroutineUtil.WaitForSeconds(1);
                    downloadUpdater();
                    
                } while (!wait_condition);
            }

            IEnumerator er = CoDoUpdate();
            SYSCoroutineUtil.M_startCoroutine(er);
        }


        
    }
    public class MonoCoroutine : MonoBehaviour
    {
    }

}