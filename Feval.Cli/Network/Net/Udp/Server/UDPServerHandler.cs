using Net.Common;
using Net.Udp.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace Net.Udp.Server
{
    public interface IUDPHandlerServer : IHandlerServer
    {
        void Initialize(UDPServer server);
        SocketAsyncEventArgs PopSocketAsyncEventArgs();
        void OnReceive(SocketAsyncEventArgs e);
    }
    public sealed class UDPServerHandler : IUDPHandlerServer
    {
        Dictionary<string, IUDPConnection> m_Sessions;
        object m_SessionLock = new object();
        Stack<SocketAsyncEventArgs> m_SocketAsyncEventArgs;
        object m_ArgsLock = new object();
        public UDPServer Server { get; private set; }
        public bool Running { get { return Server.Running; } }
        public UDPServerHandler()
        {
            m_Sessions = new Dictionary<string, IUDPConnection>();
            m_SocketAsyncEventArgs = new Stack<SocketAsyncEventArgs>();
        }
        void IUDPHandlerServer.Initialize(UDPServer server)
        {
            Server = server;
            Task.Run(Tick);
        }
        SocketAsyncEventArgs IUDPHandlerServer.PopSocketAsyncEventArgs()
        {
            SocketAsyncEventArgs e;
            lock (m_ArgsLock) {
                if(m_SocketAsyncEventArgs.Count == 0) {
                    e = new SocketAsyncEventArgs();
                    e.Completed += new EventHandler<SocketAsyncEventArgs>(UDPServer.OnCompleted);
                }
                else {
                    e = m_SocketAsyncEventArgs.Pop();
                }
            }
            e.UserToken = Server;
            BufferManager.Alloc(e);
            return e;
        }
        void PushSocketAsyncEventArgs(SocketAsyncEventArgs e)
        {
            BufferManager.Free(e);

            lock (m_ArgsLock) {
                m_SocketAsyncEventArgs.Push(e);
            }
        }
        void OnReceived(object state)
        {
            var e = state as SocketAsyncEventArgs;
            var ep = e.RemoteEndPoint as IPEndPoint;
            var key = ep.Address + ":" + ep.Port;
            var now = DateTime.Now.Millisecond;
            IUDPConnection session;

            if (e.BytesTransferred == 4
                && e.Buffer[e.Offset + 0] == 0
                && e.Buffer[e.Offset + 1] == 0
                && e.Buffer[e.Offset + 2] == 0
                && e.Buffer[e.Offset + 3] == 0)
            {
                lock (m_SessionLock) {
                    if (m_Sessions.TryGetValue(key, out session))
                    {
                        session.Close();
                        session = null;

                        m_Sessions.Remove(key);
                    }
                }
            }
            else
            {
                lock (m_SessionLock) {
                    if (!m_Sessions.TryGetValue(key, out session))
                    {
                        var _this = e.UserToken as UDPServer;
                        session = new UDPConnection(_this.Socket, ep, Server.Pack, ((IHandlerServer)this).Remove, Server.RsaKey, (s) => ((IHandlerServer)this).HandleAcceptConnected((h, c) => s), true, Server.CCCFlag);
                        session.Initialize();
                        m_Sessions[key] = session;
                    }
                }
                session.Receive(e.Buffer, e.Offset, e.BytesTransferred);
            }
            Console.WriteLine(e.RemoteEndPoint);
            PushSocketAsyncEventArgs(e);
        }
        void IUDPHandlerServer.OnReceive(SocketAsyncEventArgs e)
        {
            ThreadPool.UnsafeQueueUserWorkItem(OnReceived, e);
        }
        void Tick()
        {
            if (!Running) return;
            Update();
            if (!Running) return;
            Task.Delay(10).ContinueWith(tsk => Tick());
        }
        void Update()
        {
            lock (m_SessionLock) {
                foreach (var kv in m_Sessions)
                {
                    kv.Value?.Update();
                }
            }
        }

        void IHandlerServer.Close()
        {
           
        }
        Func<Func<IHandlerMessage, IConnection>, IConnection> HandleConnFunc = null;
        public UDPServerHandler H<T>() where T : class, IHandlerMessage, new()
        {
            HandleConnFunc = HandleConnFunction<T>;
            return this;
        }
        public UDPServerHandler H(Func<Func<IHandlerMessage, IConnection>, IConnection> handleFunc)
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
            IHandlerMessage handler = null;
            HandleConnFunc.Invoke((h) => { handler = h; return newConnFunc.Invoke(h, ((IHandlerServer)this).Remove); });
            return handler;
        }
        void IHandlerServer.Add(IConnection conn)
        {
            throw new NotImplementedException();
        }

        void IHandlerServer.Remove(IConnection conn)
        {
            var session = conn as IUDPConnection;

            lock(m_SessionLock) {
                m_Sessions.Remove(session.Key);
            }
        }
    }
}
