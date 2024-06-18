using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Net.Common
{
    class BufferManager
    {
        static Stack<byte[]> s_Buffers = new Stack<byte[]>();
        static object s_BufferLock = new object();

        static byte[] Pop()
        {
            byte[] buffer;
            lock (s_BufferLock) {
                if(s_Buffers.Count == 0) {
                    buffer = new byte[1024 * 4];
                }
                else {
                    buffer = s_Buffers.Pop();
                }
            }
            return buffer;
        }
        static void Push(byte[] buffer)
        {
            if (buffer == null || buffer.Length != 1024 * 4) return;

            lock (s_BufferLock) {
                s_Buffers.Push(buffer);
            }
        }
        public static void Alloc(SocketAsyncEventArgs e)
        {
            var buffer = Pop();
            e.SetBuffer(buffer, 0, buffer.Length);
        }
        public static void Free(SocketAsyncEventArgs e)
        {
            var buffer = e.Buffer;
            Push(buffer);
            e.SetBuffer(null, 0, 0);
        }
    }
}
