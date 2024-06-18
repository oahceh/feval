using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Net.Common
{
    public interface IConnection
    {
        void Close();
        void Initialize();
        void Send(byte[] buffer, int offset, int len);
        IHandlerMessage Handler { get; }
        bool Running { get; }
        IPEndPoint EndPoint { get; }
        IPack Pack { get; }
    }
}
