using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Net.Common
{
    /// <summary>
    /// MemoryStream对象池(不会自动检测数据，需手动释放清理数据)
    /// </summary>
    public static class MemoryStreamPool
    {
        private static Queue<PooledMemoryStream> poolQueue = new Queue<PooledMemoryStream>();
        private static readonly object s_LockHelper = new object();

        public static PooledMemoryStream Get()
        {
            lock (s_LockHelper)
            {
                if (poolQueue.Count > 0)
                {
                    var result = poolQueue.Dequeue();
                    result.IsIdle = false;
                    return result;
                }
            }

            return new PooledMemoryStream();
        }

        public static void Release(PooledMemoryStream ms)
        {
            if (ms.IsIdle) return;

            lock (s_LockHelper)
            {
                ms.Position = 0;
                ms.SetLength(0);
                ms.IsIdle = true;
                poolQueue.Enqueue(ms);
            }
        }
    }

    /// <summary>
    /// 由对象池管理的MemoryStream
    /// </summary>
    public sealed class PooledMemoryStream : MemoryStream
    {
        internal bool IsIdle;

        public override void Close()
        {
#if UNITY_EDITOR
            UnityEngine.Console.WriteLine("[PooledMemoryStream] PooledMemoryStream should never be closed. Skipping.");
#endif

            //base.Close();
        }
    }
}