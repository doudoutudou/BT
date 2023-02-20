using System.Collections;
using System.Collections.Generic;
using AssemblyCSharp.Framework;
using UnityEngine;

namespace BTAI
{
    
    /*
     *--行为树
     *--author: yh @ 2023-02-14
      使用列子
      bt= BT.Root();
      bt.OpenBranch(
            //每个分支key不能
          BT.Board("main").OpenBranch(
              BT.Call("t1",MsgProcessor_t1),
              BT.Call("t2",MsgProcessor_t2),
              BT.Selector("selector_respin_or_freespin",this.MsgSelector_MapPickSelector,yes:"do_respin",no:"do_freespin"),
              BT.Call("do_respin",MsgProcessor_do_respin),
              BT.Waitforframes("dengdai1",1),
              BT.Call("t3",MsgProcessor_t3),
              BT.Call("do_freespin",MsgProcessor_do_freespin),
              BT.Call("t4",MsgProcessor_t4)
          ),
          BT.Board("freespin").OpenBranch(
              BT.Call("t1",Msg_t1),
              BT.Call("t2",Msg_t2),
              BT.Call("t3",Msg_t3),
              BT.Call("t4",Msg_t4)
          ),
          BT.Board("respin").OpenBranch(
              BT.Call("t1",Msg_t1),
              BT.Call("t2",Msg_t2),
              BT.Call("t3",Msg_t3),
              BT.Call("t4",Msg_t4)
          )
      );
      bt.ResetBoardRunningStack("main","t1");
     */
    public static class BT
    {
        public static Root Root() { return new Root(); }
        //木板(分支)
        public static Board Board(string name) { return new Board(name); }
        //行为消息
        public static MsgAction Call(string msg_type,System.Action handler) { return new MsgAction(msg_type,handler); }
        //选择器
        public static Selector Selector(string msg_type,System.Func<bool> selector,string yes,string no) { return new Selector(msg_type,selector,yes,no); }
        //等待一针
        public static BT_Waitforframes Waitforframes(string msg_type,int frame =1)
        {
            return new BT_Waitforframes(msg_type,frame);
        }
        
 
    }
 
    /// <summary>
    /// 节点抽象类
    /// </summary>
    public abstract class BTNode
    {
        //分支名字
        protected string node_name;
        //root的Node
        private Root _root;
        public virtual void SetRoot(Root root)
        {
            this._root = root;
        }
        public Root root
        {
            get{return this._root;}
        }
        //下一步
        public abstract void Step();

        public string GetName()
        {
            return this.node_name;
        }
    }
 
    /// <summary>
    /// 分支 包含子节点的组合 节点基类
    /// </summary>
    public abstract class Branch : BTNode
    {   

        protected int activeChild;
        protected List<BTNode> children = new List<BTNode>();
        public virtual Branch OpenBranch(params BTNode[] children)
        {
            for (var i = 0; i < children.Length; i++)
            {
                this.children.Add(children[i]);
            }

            return this;
        }
        public override void SetRoot(Root root)
        {   
            base.SetRoot(root);
            for (var i = 0; i < children.Count; i++)
            {
                children[i].SetRoot(root);
            }
        }
        public List<BTNode> Children()
        {
            return children;
        }
 
        public int ActiveChild()
        {
            return activeChild;
        }
        //重置
        public virtual void ResetChildren()
        {
            activeChild = 0;
            for (var i = 0; i < children.Count; i++)
            {
                Branch b = children[i] as Branch;
                if (b != null)
                {
                    b.ResetChildren();
                }
            }
        }

        public void SwitchProcessTo(string call_name)
        {
            BTNode board = null;
            for (int i = 0; i < children.Count; i++)
            {
                board = children[i];
                if (board != null)
                {
                    if (board.GetName() == call_name)
                    {
                        activeChild = i;
                    }
                }
            }
        }
    }
    //木板 上只有 执行消息
    public class Board : Branch
    {
        //分支名字
        public Board(string name)
        {
            this.node_name = name;
        }
        public override void Step()
        {
            if (activeChild == children.Count)
            {
                //activeChild = 0;
                return;
            }
            var current = children[activeChild];
            activeChild++;
            current.Step();
        }
        
    }
    //==================================================================
    public class Root 
    { 
        //public bool isTerminated = false;
        
        protected int activeChild;
        protected List<Branch> children = new List<Branch>();
        public virtual Root OpenBranch(params Branch[] children)
        {
            for (var i = 0; i < children.Length; i++)
            {
                this.children.Add(children[i]);
                children[i].SetRoot(this);
            }

            
            return this;
        }
       /* private void DoChildSetRoot()
        {
            
        }*/

        //下一步
        public void Step()
        {
            var current_board = children[activeChild];
            current_board.Step();
        }
        //转换木板 (分支)
        public void SwitchBoard(string board_name)
        {
            Board board = null;
            for (int i = 0; i < children.Count; i++)
            {
                board = children[i] as Board;
                if (board != null)
                {
                    if (board.GetName() == board_name)
                    {
                        activeChild = i;
                        return;
                    }
                }
            }
            var current_board = children[activeChild];
            current_board.ResetChildren();
        }
        //将进程切换到 当前分支的节点上
        public void SwitchProcessTo(string call_name)
        {
            this.__SwitchProcessTo(call_name);
            this.Step();
        }
        private void __SwitchProcessTo(string call_name)
        {
            var current = children[activeChild];
            current.SwitchProcessTo(call_name);
        }
        //重置到分支 key方法开始
        public void ResetBoardRunningStack(string board_name,string call_name)
        {
            this.SwitchBoard(board_name);
            this.__SwitchProcessTo(call_name);
            this.Step();
        }
        
    }
    public class Selector : BTNode
    {
        private System.Func<bool> selector;
        private string yes;
        private string no;

        public Selector(string msg_type,System.Func<bool> selector,string yes,string no)
        {
            UIUtil.Assert(selector!=null,"selector is null");
            this.node_name = msg_type;
            this.selector = selector;
            this.yes = yes;
            this.no = no;
        }
 
        public override void Step()
        {
            bool isbool = selector();
            if (isbool)
            {
                this.root.SwitchProcessTo(yes);
            }
            else
            {
                this.root.SwitchProcessTo(no);
            }
        }
    }
    //等待针
    public class BT_Waitforframes : BTNode
    {
        private int frame;
        public BT_Waitforframes(string msg_type,int frame)
        {
            this.node_name = msg_type;
            this.frame = frame;
        }

        public override void Step()
        {
            IEnumerator _er()
            {
                yield return SYSCoroutineUtil.waitforframes(frame);
                this.root.Step();
            }

            SYSCoroutineUtil.M_startCoroutine(_er());
        }
    }
    
    public class MsgAction : BTNode
    {
        System.Action handler;
        System.Func<IEnumerator> coroutineFactory;
        IEnumerator coroutine;
        public MsgAction(string msg_type,System.Action handler)
        {
            this.node_name = msg_type;
            this.handler = handler;
        }


        /* public Action(System.Func<IEnumerator> coroutineFactory)
         {
             this.coroutineFactory = coroutineFactory;
         }
         */
        //
        public override void Step()
        {
            if (handler != null)
            {
                handler();
            }
        }
 
        public override string ToString()
        {
            return "Action : " + handler.Method.ToString();
        }
    }
    
    
}
 