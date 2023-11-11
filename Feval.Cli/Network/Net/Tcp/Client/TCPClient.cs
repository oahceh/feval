using Crypto;
using Helper;
using Net.Common;
using Net.Tcp.Common;
using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace Net.Tcp.Client
{
    public sealed class TCPClient : IClient
    {
        #region Property

        public class SocketArgs
        {
            public Socket socket;

            public IConnection connection;

            public bool isConnected;

            public Semaphore semaphore;

            public TCPClient tcpClient;

            public void Close()
            {
                isConnected = false;
                semaphore?.Release();
                connection?.Close();
                connection = null;
                semaphore = null;
                IOCPConn.Close(socket);
                socket = null;
            }
        }

        public IHandlerMessage Handler { get; private set; }

        public string RsaPub { get; private set; }

        public byte CCCFlag { get; private set; }

        public bool Running => m_SocketArgs != null;

        public bool Connected => m_SocketArgs != null && m_SocketArgs.isConnected;

        public IPEndPoint RemoteEndPoint { get; private set; }
        public IPack Pack { get; private set; } = new CPack();

        #endregion

        #region Interface

        public static TCPClient Create<T>() where T : class, IHandlerMessage, new()
        {
            var handler = Activator.CreateInstance<T>();
            return new TCPClient(handler);
        }

        public static TCPClient Create(IHandlerMessage handler)
        {
            return new TCPClient(handler);
        }

        public Semaphore Connect(string ip, int port, int timeout = 0)
        {
            return Connect(ip, port, null, timeout);
        }

        public Semaphore Connect(string ip, int port, string pub, int timeout = 0)
        {
            return Connect(ip, port, pub, true, NetHelper.CCC_Compress | NetHelper.CCC_Crypto | NetHelper.CCC_Crc,
                timeout);
        }

        public Semaphore Connect(string ip, int port, string pub, bool async, byte cccFlag, int timeout = 0)
        {
            var ipAddress = NetHelper.ParseIpAddressV6(ip, port);
            if (ipAddress == null)
            {
                throw new Exception($"Unknown address: {ip}");
            }

            Close();
            if (string.IsNullOrEmpty(pub))
            {
                pub = DefaultRsaPub;
            }

            if (!Rsa.CheckIsPub(pub))
            {
                throw new Exception($"RsaPub error {pub}");
            }

            RsaPub = pub;
            CCCFlag = cccFlag;
            RemoteEndPoint = new IPEndPoint(ipAddress, port);
            var semaphore = new Semaphore(0, 1024);
            lock (m_Lock)
            {
                m_SocketArgs = new SocketArgs
                {
                    tcpClient = this,
                    socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                    {
                        NoDelay = true
                    },
                    semaphore = semaphore
                };
            }

            // Console.WriteLine($"TCP client connecting {ipAddress}:{port}");

            if (async)
            {
                var asyncResult = m_SocketArgs.socket.BeginConnect(RemoteEndPoint, ConnectCallback, m_SocketArgs);
                if (timeout > 0)
                {
                    var thread = new Thread(() =>
                    {
                        asyncResult.AsyncWaitHandle.WaitOne(new TimeSpan(0, 0, timeout));
                        var args = (SocketArgs) asyncResult.AsyncState;
                        if (args.socket != null && !args.socket.Connected && !asyncResult.IsCompleted)
                        {
                            Console.WriteLine("BeginConnect TimeOut");
                            ConnectCallback(asyncResult);
                        }
                    });

                    thread.IsBackground = true;
                    thread.Start();
                }
            }
            else
            {
                m_SocketArgs.socket.Connect(RemoteEndPoint);
                Console.WriteLine($"NetClient Connected");
                OnConnected(m_SocketArgs);
            }

            return semaphore;
        }

        public void Close()
        {
            lock (m_Lock)
            {
                if (m_SocketArgs != null)
                {
                    m_SocketArgs.Close();
                    m_SocketArgs = null;
                }
            }
        }

        public void SetPack(IPack pack)
        {
            if (Running)
            {
                Console.WriteLine("Please set before connect.");
                return;
            }

            Pack = pack;
        }

        #endregion

        #region Method

        private TCPClient(IHandlerMessage handler)
        {
            Handler = handler;
        }

        private void OnConnected(SocketArgs args)
        {
            args.semaphore?.Release();
            if (m_SocketArgs != args)
            {
                return;
            }

            // 交换私钥
            var kiv = AesKeyIV.GenKeyIV();
            AesKeyIV.SendAesKeyIVRsa(args.socket, args.tcpClient.RsaPub, kiv);
            new AesKeyIVAes(args, KIVHandleConnected, KIVHandleClose, kiv);
        }

        private void KIVHandleClose(SocketArgs args)
        {
            args.Close();
        }

        private void KIVHandleConnected(SocketArgs args, byte[] kiv)
        {
            if (args != m_SocketArgs)
            {
                return;
            }

            args.isConnected = true;
            args.connection = new TCPConnection(args.socket, Handler, Pack, ConnectionClose, kiv,
                args.tcpClient.CCCFlag);
            args.connection.Initialize();
        }

        private void ConnectionClose(IConnection conn)
        {
            if (conn == m_SocketArgs?.connection)
            {
                Close();
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            var args = ar.AsyncState as SocketArgs;
            if (args != m_SocketArgs)
            {
                try
                {
                    args.Close();
                    Console.WriteLine("Close Last Connect");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ConnectCallback timeout! exception: {e.ToString()}");
                }

                return;
            }

            try
            {
                var s = args.socket;
                if (!s.Connected)
                {
                    throw new SocketException((int) SocketError.TimedOut);
                }

                s.EndConnect(ar);
                OnConnected(args);
            }
            catch (Exception e)
            {
                Console.WriteLine($"ConnectCallback exception:  {e}");
                Close();
                Handler.HandleConnected(false);
                Handler.HandleClose();
            }
        }

        #endregion

        #region Field

        private readonly object m_Lock = new object();

        private SocketArgs m_SocketArgs;

        private const string DefaultRsaPub =
            "<RSAKeyValue><Modulus>3xzU8e+jSKtePBcKoZjqfAlU3OAYmJhaCrm3WRmibuiGXNOIW/QnsFu/2wCSii556fT/kNcvcCKu8TEZ9MbVdOJ0B+4SpLcy1akLvu5qEPtZvOftei1lxiPYbjg0l5Akos7t5gpF6uxflIN18kBcE2QPLZ/o7JuLwYvgH7lHyNE=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        #endregion
    }
}