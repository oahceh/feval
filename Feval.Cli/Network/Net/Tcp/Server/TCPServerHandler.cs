using System;
using System.Threading;
using System.Collections.Generic;
using System.Net.Sockets;
using Net.Common;
using Net.Tcp.Common;

namespace Net.Tcp.Server
{
    public sealed class TCPServerHandler : IHandlerServer
    {
        bool m_Closed = false;
        Dictionary<IConnection, IHandlerMessage> m_Connections = new Dictionary<IConnection, IHandlerMessage>();
        object m_Lock = new object();

        void IHandlerServer.Close()
        {
            m_Closed = true;
            bool locked = false;

            try {
                Monitor.Enter(m_Lock);
                locked = true;
                
                var tmp = m_Connections.Keys;
                m_Connections.Clear();
                
                Monitor.Exit(m_Lock);
                locked = false;

                foreach (var k in tmp)
                {
                    k.Close();
                }
            }
            finally {
                if (locked) {
                    Monitor.Exit(m_Lock);
                }
            }
        }

        void IHandlerServer.Remove(IConnection conn)
        {
            lock (m_Lock) {
                if (m_Connections.ContainsKey(conn)) {
                    m_Connections.Remove(conn);
                }
            }
        }

        void IHandlerServer.Add(IConnection conn)
        {
            if (m_Closed)
            {
                conn.Close();
                return;
            }

            lock (m_Lock) {
                if (!m_Connections.ContainsKey(conn)) {
                    m_Connections.Add(conn, conn.Handler);
                }
            }
        }
        Func<Func<IHandlerMessage,IConnection>, IConnection> HandleConnFunc = null;
        public TCPServerHandler H<T>() where T : class, IHandlerMessage, new()
        {
            HandleConnFunc = HandleConnFunction<T>;
            return this;
        }
        public TCPServerHandler H(Func<Func<IHandlerMessage, IConnection>, IConnection> handleFunc)
        {
            HandleConnFunc = handleFunc;
            return this;
        }
        IConnection HandleConnFunction<T>(Func<IHandlerMessage, IConnection> newFunc) where T : class, IHandlerMessage, new()
        {
            var handler = new T();
            var conn = newFunc.Invoke(handler);
            return conn;
        }
        IHandlerMessage IHandlerServer.HandleAcceptConnected(NewConnFunc newConnFunc)
        {
            if (m_Closed)
            {
                return null;
            }
            var conn = HandleConnFunc.Invoke((h) => newConnFunc.Invoke(h, ((IHandlerServer)this).Remove));
            ((IHandlerServer)this).Add(conn);
            conn.Initialize();
            return conn.Handler;
        }
    }
}
