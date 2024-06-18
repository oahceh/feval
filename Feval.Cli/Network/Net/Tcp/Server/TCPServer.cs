using Crypto;
using Helper;
using Net.Common;
using Net.Tcp.Common;
using System;
using System.Net;
using System.Net.Sockets;


namespace Net.Tcp.Server
{
    public sealed class TCPServer : IServer
    {
        #region Property

        public Socket Socket { get; private set; }

        public IHandlerServer Handler { get; private set; }

        public byte CCCFlag { get; private set; }

        public bool Running { get; private set; }

        public IPEndPoint EndPoint { get; private set; }

        public IPack Pack { get; private set; } = new CPack();

        public string RsaKey { get; private set; }

        #endregion

        #region Interface

        public static TCPServer Create<T>(int listenPort, string rsaKey = null) where T : class, IHandlerMessage, new()
        {
            var handler = new TCPServerHandler().H<T>();
            return new TCPServer(handler, listenPort, rsaKey);
        }

        public static TCPServer Create<T>(IPAddress localIPAddress, int listenPort, string rsaKey = null)
            where T : class, IHandlerMessage, new()
        {
            var handler = new TCPServerHandler().H<T>();
            return new TCPServer(handler, localIPAddress, listenPort, rsaKey);
        }

        public static TCPServer Create(IHandlerServer handler, int listenPort, string rsaKey = null)
        {
            return new TCPServer(handler, listenPort, rsaKey);
        }

        public static TCPServer Create(IHandlerServer handler, IPAddress localIPAddress, int listenPort,
            string rsaKey = null)
        {
            return new TCPServer(handler, localIPAddress, listenPort, rsaKey);
        }

        public void SetPack(IPack pack)
        {
            if (Running)
            {
                Console.WriteLine("Please set before start.");
                return;
            }

            Pack = pack;
        }

        public void Start()
        {
            Start(1024);
        }

        public void Start(int backlog)
        {
            Start(1024, NetHelper.CCC_Compress | NetHelper.CCC_Crypto | NetHelper.CCC_Crc);
        }

        public void Start(int backlog, byte cccFlag)
        {
            if (Running)
            {
                return;
            }

            Running = true;
            CCCFlag = cccFlag;
            Socket.Bind(EndPoint);
            Socket.Listen(backlog);
            var e = new SocketAsyncEventArgs();
            e.Completed += OnAcceptCompleted;
            BeginAccept(e);
            Console.WriteLine($"TCP server start {EndPoint.Address}:{EndPoint.Port}, backlog: {backlog}");
        }

        public void Stop()
        {
            if (!Running) return;
            Running = false;
            Handler?.Close();
            Socket?.Close();
        }

        #endregion

        #region Method

        private TCPServer(IHandlerServer handler, int listenPort, string rsaKey = null)
            : this(handler, IPAddress.Any, listenPort, rsaKey)
        {
        }

        private TCPServer(IHandlerServer handler, IPEndPoint localEP, string rsaKey = null)
            : this(handler, localEP.Address, localEP.Port, rsaKey)
        {
        }

        private TCPServer(IHandlerServer handler, IPAddress localIPAddress, int listenPort, string rsaKey = null)
        {
            rsaKey = string.IsNullOrEmpty(rsaKey) ? DefaultRsaKey : rsaKey;
            Handler = handler;
            EndPoint = new IPEndPoint(localIPAddress, listenPort);
            RsaKey = rsaKey;
            CCCFlag = NetHelper.CCC_Compress | NetHelper.CCC_Crypto | NetHelper.CCC_Crc;
            if (!Rsa.CheckIsKey(rsaKey))
            {
                throw new Exception($"RasKey error {rsaKey}");
            }

            Socket = new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void BeginAccept(SocketAsyncEventArgs e)
        {
            e.AcceptSocket = null;
            //AcceptAsync=>true如果I/O挂起等待异步则触发AcceptAsyn_Asyn_Completed事件
            //AcceptAsync=>false此时I/O操作同步完成，不会触发Asyn_Completed事件，直接调用ProcessAccept()方法
            if (!Socket.AcceptAsync(e))
            {
                ProcessAccept(e);
            }
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (!Running) return;
            var client = e.AcceptSocket;
            Console.WriteLine(
                $"NetServer ProcessAccept {(client.RemoteEndPoint as IPEndPoint).Address}:{(client.RemoteEndPoint as IPEndPoint).Port}");
            new AesKeyIVRsa(client, Handler, this, RsaKey, CCCFlag);
            BeginAccept(e);
        }

        #endregion

        #region Field

        private const string DefaultRsaKey =
            "<RSAKeyValue><Modulus>3xzU8e+jSKtePBcKoZjqfAlU3OAYmJhaCrm3WRmibuiGXNOIW/QnsFu/2wCSii556fT/kNcvcCKu8TEZ9MbVdOJ0B+4SpLcy1akLvu5qEPtZvOftei1lxiPYbjg0l5Akos7t5gpF6uxflIN18kBcE2QPLZ/o7JuLwYvgH7lHyNE=</Modulus><Exponent>AQAB</Exponent><P>/nGIgM2rMV5RBCGSiQLndFRImAHlqLlcg1LCtc96X02flUwMo8DPHLLsTyc5Bl4iYl9nbliei+k06esPA1LkXw==</P><Q>4Ho7wzOrPLbcPZzQgoEzemtXGuWs70ye1M5Ef0C2WmyhvtAkDo3HVifP2FKjzu+sO8msfueGBqwZWNi/hVLgzw==</Q><DP>DVAZaUvZijK6IHI1PY/2VkLWrYVj48kXCxP4dhTN/VCNaf1Zp/O9om3GKXoO5MNmHymIuuBOI1nnV9nhpjXfFw==</DP><DQ>cabRbTJSx0mZ1oP3uatqgdeo4VBZr0quu/W3DmqYKM4JUk+VgdzciM1dWRv2HcaRADBKanIUFHq71pTe2sSsVw==</DQ><InverseQ>V7fK2dLpnBlpKADfpwO/wYSpG9eMgS27ExkoAg0JZa1gBa+bCJifk3t6XpAkv8B/CK2gHJcz1fk3qKNwGr4MoQ==</InverseQ><D>zKvOTQLgb1GFaOpaPlPhB1goGVcaOSHJt/0WTQ5PDB8S4yTJ+lDH9+iy31xvEYQBIrY1m9FLGzs18NxySzH7rT6YSjpBFefM5Seet6Q2ALkex5xUhlVpQsgOvtkLE6uuey4IIqtHoJ65VyFK5vsGY+CHbRuP6bEPJEL1TyWl+mE=</D></RSAKeyValue>";

        #endregion
    }
}