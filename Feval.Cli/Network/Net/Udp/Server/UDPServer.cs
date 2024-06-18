using System;
using System.Net;
using System.Net.Sockets;
using Crypto;
using Helper;
using Net.Common;


namespace Net.Udp.Server
{
    public sealed class UDPServer : IServer
    {
        static readonly string DefaultRsaKey = "<RSAKeyValue><Modulus>3xzU8e+jSKtePBcKoZjqfAlU3OAYmJhaCrm3WRmibuiGXNOIW/QnsFu/2wCSii556fT/kNcvcCKu8TEZ9MbVdOJ0B+4SpLcy1akLvu5qEPtZvOftei1lxiPYbjg0l5Akos7t5gpF6uxflIN18kBcE2QPLZ/o7JuLwYvgH7lHyNE=</Modulus><Exponent>AQAB</Exponent><P>/nGIgM2rMV5RBCGSiQLndFRImAHlqLlcg1LCtc96X02flUwMo8DPHLLsTyc5Bl4iYl9nbliei+k06esPA1LkXw==</P><Q>4Ho7wzOrPLbcPZzQgoEzemtXGuWs70ye1M5Ef0C2WmyhvtAkDo3HVifP2FKjzu+sO8msfueGBqwZWNi/hVLgzw==</Q><DP>DVAZaUvZijK6IHI1PY/2VkLWrYVj48kXCxP4dhTN/VCNaf1Zp/O9om3GKXoO5MNmHymIuuBOI1nnV9nhpjXfFw==</DP><DQ>cabRbTJSx0mZ1oP3uatqgdeo4VBZr0quu/W3DmqYKM4JUk+VgdzciM1dWRv2HcaRADBKanIUFHq71pTe2sSsVw==</DQ><InverseQ>V7fK2dLpnBlpKADfpwO/wYSpG9eMgS27ExkoAg0JZa1gBa+bCJifk3t6XpAkv8B/CK2gHJcz1fk3qKNwGr4MoQ==</InverseQ><D>zKvOTQLgb1GFaOpaPlPhB1goGVcaOSHJt/0WTQ5PDB8S4yTJ+lDH9+iy31xvEYQBIrY1m9FLGzs18NxySzH7rT6YSjpBFefM5Seet6Q2ALkex5xUhlVpQsgOvtkLE6uuey4IIqtHoJ65VyFK5vsGY+CHbRuP6bEPJEL1TyWl+mE=</D></RSAKeyValue>";
        //static readonly string DefaultRsaPub = "<RSAKeyValue><Modulus>3xzU8e+jSKtePBcKoZjqfAlU3OAYmJhaCrm3WRmibuiGXNOIW/QnsFu/2wCSii556fT/kNcvcCKu8TEZ9MbVdOJ0B+4SpLcy1akLvu5qEPtZvOftei1lxiPYbjg0l5Akos7t5gpF6uxflIN18kBcE2QPLZ/o7JuLwYvgH7lHyNE=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        public Socket Socket { get; private set; }
        public IHandlerServer Handler { get; private set; }
        public bool Running { get; private set; }
        public IPEndPoint EndPoint { get; private set; }
        public IPack Pack { get; private set; } = new CPack();
        public byte CCCFlag { get; private set; }
        public string RsaKey { get; private set; }

        public static UDPServer Create<T>(int listenPort, string rsaKey = null) where T : class, IHandlerMessage, new()
        {
            var handler = new UDPServerHandler().H<T>();
            return new UDPServer(handler, listenPort, rsaKey);
        }

        public static UDPServer Create<T>(IPAddress localIPAddress, int listenPort, string rsaKey = null) where T : class, IHandlerMessage, new()
        {
            var handler = new UDPServerHandler().H<T>();
            return new UDPServer(handler, localIPAddress, listenPort, rsaKey);
        }

        public static UDPServer Create(IUDPHandlerServer handler, int listenPort, string rsaKey = null)
        {
            return new UDPServer(handler, listenPort, rsaKey);
        }

        public static UDPServer Create(IUDPHandlerServer handler, IPAddress localIPAddress, int listenPort, string rsaKey = null)
        {
            return new UDPServer(handler, localIPAddress, listenPort, rsaKey);
        }

        private UDPServer(IUDPHandlerServer handler, int listenPort, string rsaKey = null)
            : this(handler, IPAddress.Any, listenPort, rsaKey)
        {
        }

        private UDPServer(IUDPHandlerServer handler, IPEndPoint localEP, string rsaKey = null)
            : this(handler, localEP.Address, localEP.Port, rsaKey)
        {
        }

        IUDPHandlerServer m_SessionMgr;
        private UDPServer(IUDPHandlerServer handler, IPAddress localIPAddress, int listenPort, string rsaKey = null)
        {
            rsaKey = string.IsNullOrEmpty(rsaKey) ? DefaultRsaKey : rsaKey;
            Handler = handler;
            m_SessionMgr = handler;
            EndPoint = new IPEndPoint(localIPAddress, listenPort);
            RsaKey = rsaKey;
            CCCFlag = NetHelper.CCC_Compress | NetHelper.CCC_Crypto | NetHelper.CCC_Crc;
            if (!Rsa.CheckIsKey(rsaKey))
            {
                throw new Exception(string.Format("RasKey error {0}", rsaKey));
            }
            Socket = new Socket(EndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            m_SessionMgr.Initialize(this);
        }
        public void SetPack(IPack pack)
        {
            if (Running) { Console.WriteLine("Please set before start."); return; }
            Pack = pack;
        }
        public void Start()
        {
            Start(1024, NetHelper.CCC_Compress | NetHelper.CCC_Crypto | NetHelper.CCC_Crc);
        }
        public void Start(int backlog)
        {
            Start(backlog, NetHelper.CCC_Compress | NetHelper.CCC_Crypto | NetHelper.CCC_Crc);
        }
        public void Start(int backlog, byte cccflag)
        {
            if (Running) return;
            Running = true;
            CCCFlag = cccflag;
            const int SIO_UDP_CONNRESET = -1744830452;
            byte[] inValue = new byte[] { 0 };
            byte[] outValue = new byte[] { 0 };
            Socket.IOControl(SIO_UDP_CONNRESET, inValue, outValue);
            Socket.Bind(EndPoint);
            BeginAccept(this);
            Console.WriteLine(string.Format("NetServer Start {0}:{1},backlog:{2}", EndPoint.Address, EndPoint.Port, backlog));
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
        static void BeginAccept(UDPServer _this)
        {
            var e = _this.m_SessionMgr.PopSocketAsyncEventArgs();
            BeginReceive(e);
        }
        static void BeginReceive(SocketAsyncEventArgs e)
        {
            Console.WriteLine("BeginReceive");
            var _this = e.UserToken as UDPServer;
            if (!_this.Running) return;
            e.RemoteEndPoint = _this.EndPoint;
            if (!(_this.Socket.ReceiveFromAsync(e)))
            {
                ProcessReceive(e);
            }
        }
        static void ProcessReceive(SocketAsyncEventArgs e)
        {
            Console.WriteLine("ProcessReceive");
            var _this = e.UserToken as UDPServer;
            if (!_this.Running) return;
            //new AesKeyIVRsa(client, Handler, RsaKey, CCCFlag);
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                try
                {
                    _this.m_SessionMgr.OnReceive(e);
                }
                catch (Exception) { }
                e = _this.m_SessionMgr.PopSocketAsyncEventArgs();
                BeginReceive(e);
            }
            else
            {
                _this.Restart();
            }
        }
        static void ProcessSend(SocketAsyncEventArgs e)
        {
            Console.WriteLine("ProcessSend");
            if (e.SocketError == SocketError.Success) return;
            var _this = e.UserToken as UDPServer;
            if (!_this.Running) return;
            Console.WriteLine(e.SocketError);
            _this.Restart();
        }

        void CloseSocket()
        {
            Console.WriteLine("CloseSocket");
            if (Socket == null) return;
            try
            {
                Socket.Shutdown(SocketShutdown.Both);
            }
            catch { }
            try
            {
                Socket.Close();
            }
            catch { }
        }

        void Restart()
        {
            CloseSocket();

        }

        public void Stop()
        {
            if (!Running) return;
            Running = false;
            Handler?.Close();
            Socket?.Close();
        }
    }
}
