using Net.Common;
using System;
using System.Threading;
using System.Net.Sockets;


namespace Net.Tcp.Common
{
    internal class IOCPConn
    {
        #region Interface

        public static void Start(IIOCP iocp)
        {
            var e = SocketAsyncEventArgsPool.Get();
            e.UserToken = iocp;
            iocp.IOCPInitialize(e);
            var thread = new Thread(() => ReceiveThread(iocp, e))
            {
                IsBackground = true
            };
            thread.Start();
        }

        public static void Close(SocketAsyncEventArgs e)
        {
            var token = e.UserToken as IIOCP;
            SocketAsyncEventArgsPool.Release(e);
            Close(token);
        }

        public static void Close(IIOCP token)
        {
            try
            {
                token.IOCPClose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void Close(Socket sock)
        {
            if (sock == null)
            {
                return;
            }

            try
            {
                sock.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
                // ignored
            }

            try
            {
                sock.Close();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        #endregion

        #region Method

        private static void ReceiveThread(IIOCP iocp, SocketAsyncEventArgs e)
        {
            const int pollTimeout = 50 * 1000;
            try
            {
                iocp.Socket.Blocking = false;
                while (iocp.Socket != null && e.SocketError == SocketError.Success)
                {
                    iocp.Socket.Poll(pollTimeout, SelectMode.SelectRead);
                    var totalRead = 0;
                    while (true)
                    {
                        var len = iocp.Socket.Receive(e.Buffer, e.Offset + totalRead,
                            e.Count - totalRead, SocketFlags.None, out var socketError);
                        e.SocketError = socketError;

                        if (e.SocketError != SocketError.Success || len == 0)
                        {
                            break;
                        }

                        totalRead += len;
                    }

                    // If the remote host shuts down the Socket connection with the Shutdown method,
                    // and all available data has been received, the Receive method will complete
                    // immediately and return zero bytes.
                    if (totalRead == 0 && e.SocketError == SocketError.Success)
                    {
                        Console.WriteLine($"IOCP: remote host shut down connection");
                        Close(e);
                        break;
                    }

                    if (e.SocketError == SocketError.WouldBlock)
                    {
                        e.SocketError = SocketError.Success;
                    }

                    if (totalRead > 0 && e.SocketError == SocketError.Success)
                    {
                        if (!iocp.IOCPReceived(totalRead, e))
                        {
                            SocketAsyncEventArgsPool.Release(e);
                            break;
                        }
                    }
                    else if (e.SocketError != SocketError.Success)
                    {
                        Console.WriteLine($"IOCP receive error: {e.SocketError}");
                        Close(e);
                        break;
                    }

                    // sleep for moment，or it will be fire...
                    Thread.Sleep(1);
                }
            }
            catch (NullReferenceException)
            {
            }
            catch (SocketException)
            {
                Close(e);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        #endregion
    }
}