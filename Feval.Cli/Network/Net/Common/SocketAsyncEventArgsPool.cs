using System.Collections.Generic;
using System.Net.Sockets;

namespace Net.Common
{
    internal class SocketAsyncEventArgsPool
    {
        #region Interface

        public static SocketAsyncEventArgs Get()
        {
            SocketAsyncEventArgs e;
            lock (m_PoolLock)
            {
                e = m_SocketAsyncEventArgsPool.Count == 0
                    ? new SocketAsyncEventArgs()
                    : m_SocketAsyncEventArgsPool.Pop();
            }

            BufferManager.Alloc(e);
            e.UserToken = null;
            return e;
        }

        public static void Release(SocketAsyncEventArgs e)
        {
            e.UserToken = null;
            e.SocketError = SocketError.Success;
            BufferManager.Free(e);
            lock (m_PoolLock)
            {
                m_SocketAsyncEventArgsPool.Push(e);
            }
        }

        #endregion

        #region Field

        private static Stack<SocketAsyncEventArgs> m_SocketAsyncEventArgsPool = new Stack<SocketAsyncEventArgs>();

        private static object m_PoolLock = new object();

        #endregion
    }
}