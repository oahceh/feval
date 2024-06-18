using System.Net.Sockets;

namespace Net.Tcp.Common
{
    internal interface IIOCP
    {
        Socket Socket { get; }

        void IOCPInitialize(SocketAsyncEventArgs e);

        bool IOCPReceived(int len, SocketAsyncEventArgs e);

        void IOCPClose();
    }
}