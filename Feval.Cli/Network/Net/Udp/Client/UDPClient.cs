using Crypto;
using Helper;
using Net.Common;
using Net.Udp.Common;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace Net.Udp.Client
{
    public sealed class UDPClient : IClient
    {
        static readonly string DefaultRsaPub = "<RSAKeyValue><Modulus>3xzU8e+jSKtePBcKoZjqfAlU3OAYmJhaCrm3WRmibuiGXNOIW/QnsFu/2wCSii556fT/kNcvcCKu8TEZ9MbVdOJ0B+4SpLcy1akLvu5qEPtZvOftei1lxiPYbjg0l5Akos7t5gpF6uxflIN18kBcE2QPLZ/o7JuLwYvgH7lHyNE=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        public class SocketArgs
        {
            public Socket m_Socket;
            public IUDPConnection m_Connection;
            public bool IsConnected;
            public Semaphore m_Semaphore;
            public UDPClient m_NetClient;
            public void Close()
            {
                if (!IsConnected) return;
                IsConnected = false;
                m_Semaphore?.Release();
                m_Connection?.Close();
                m_Connection = null;
                m_Semaphore = null;
                CloseSocket();
                m_Socket = null;
            }

            void CloseSocket()
            {
                if (m_Socket == null) return;
                try
                {
                    m_Socket.Shutdown(SocketShutdown.Both);
                }
                catch { }
                try
                {
                    m_Socket.Close();
                }
                catch { }
            }
            public void OnConnected()
            {
                IsConnected = true;
                Task.Run(Tick);
            }
            void Tick()
            {
                if (!IsConnected || !m_NetClient.Running) return;
                if (m_Connection != null) m_Connection.Update();
                if (!IsConnected || !m_NetClient.Running) return;
                Task.Delay(10).ContinueWith(tsk => Tick());
            }
        }
       
        SocketArgs m_SocketArgs;
        public IHandlerMessage Handler { get; private set; }
        public string RsaPub { get; private set; }
        public byte CCCFlag { get; private set; }
        public bool Running
        {
            get {
                return m_SocketArgs != null;
            }
        }
        
        public bool Connected
        {
            get
            {
                if(m_SocketArgs == null)
                {
                    return false;
                }
                return m_SocketArgs.IsConnected;
            }
        }

        public IPEndPoint RemoteEndPoint { get; private set; }
        public IPack Pack { get; private set; } = new CPack();
        private UDPClient(IHandlerMessage handler)
        {
            Handler = handler;
        }

        public static UDPClient Create<T>() where T : class, IHandlerMessage, new()
        {
            var handler = Activator.CreateInstance<T>();
            return new UDPClient(handler);
        }

        public static UDPClient Create(IHandlerMessage handler)
        {
            return new UDPClient(handler);
        }
        public void SetPack(IPack pack)
        {
            if (Running) { Console.WriteLine("Please set before connect."); return; }
            Pack = pack;
        }
        public Semaphore Connect(string addr, int port, int timeout = 0)
        {
            return Connect(addr, port, null, timeout);
        }
        public Semaphore Connect(string addr, int port, string pub, int timeout = 0)
        {
            return Connect(addr, port, pub, true, NetHelper.CCC_Compress | NetHelper.CCC_Crypto | NetHelper.CCC_Crc, timeout);
        }
        public Semaphore Connect(string addr, int port, string pub, bool async, byte cccflag, int timeout = 0)
        {
            var ip = NetHelper.ParseIpAddressV6(addr);
            if (ip == null)
            {
                throw new Exception("Unknown addr = " + addr);
            }
            Close();
            if (string.IsNullOrEmpty(pub)) pub = DefaultRsaPub;
            if (!Rsa.CheckIsPub(pub))
            {
                throw new Exception(string.Format("RsaPub error {0}", pub));
            }
            RsaPub = pub;
            CCCFlag = cccflag;
            RemoteEndPoint = new IPEndPoint(ip, port);

            m_SocketArgs = new SocketArgs();
            m_SocketArgs.m_NetClient = this;
            var semaphore = new Semaphore(0, 1024);
            m_SocketArgs.m_Socket = new Socket(ip.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            m_SocketArgs.m_Semaphore = semaphore;
            
            Console.WriteLine(string.Format("NetClient Connect {0}:{1}", ip, port));
            m_SocketArgs.m_Socket.Connect(RemoteEndPoint);
            OnConnected(m_SocketArgs);
            return semaphore;
        }

        void OnConnected(SocketArgs args)
        {
            args.m_Semaphore?.Release();
            if (m_SocketArgs != args) return;
            args.m_Socket.Send(new byte[] { 0, 0, 0, 0 });
            args.m_Connection = new UDPConnection(args.m_Socket, args.m_NetClient.RemoteEndPoint, args.m_NetClient.Pack, ConnectionClose, args.m_NetClient.RsaPub, (s) => args.m_NetClient.Handler, false, args.m_NetClient.CCCFlag);
            args.m_Connection.Initialize();
            args.OnConnected();
            var e = new SocketAsyncEventArgs();
            e.Completed += new EventHandler<SocketAsyncEventArgs>(OnCompleted);
            e.UserToken = args;
            BufferManager.Alloc(e);
            BeginReceive(e);
        }
        void ConnectionClose(IConnection conn)
        {
            if (conn == m_SocketArgs?.m_Connection)
            {
                Close();
            }
        }
        internal static void OnCompleted(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.ReceiveFrom:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.SendTo:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }
        static void BeginReceive(SocketAsyncEventArgs e)
        {
            Console.WriteLine("BeginReceive");
            var _this = e.UserToken as SocketArgs;
            if (!_this.IsConnected) return;
            e.RemoteEndPoint = _this.m_NetClient.RemoteEndPoint;
            if (!(_this.m_Socket.ReceiveFromAsync(e)))
            {
                ProcessReceive(e);
            }
        }
        static void ProcessReceive(SocketAsyncEventArgs e)
        {
            Console.WriteLine("ProcessReceive");
            var _this = e.UserToken as SocketArgs;
            if (!_this.IsConnected) return;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                _this.m_Connection.Receive(e.Buffer, e.Offset, e.BytesTransferred);
                BeginReceive(e);
            }
            else
            {
                _this.Close();
            }
        }
        static void ProcessSend(SocketAsyncEventArgs e)
        {
            Console.WriteLine("ProcessSend");
            if (e.SocketError == SocketError.Success) return;
            var _this = e.UserToken as SocketArgs;
            if (!_this.IsConnected) return;
            Console.WriteLine(e.SocketError);
            _this.Close();
        }

        public void Close()
        {
            if (m_SocketArgs != null)
            {
                m_SocketArgs.Close();
                m_SocketArgs = null;
            }
        }
    }
}
