using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Net.Common
{
    public interface IClient
    {
        IHandlerMessage Handler { get; }
        bool Running { get; }
        IPEndPoint RemoteEndPoint { get; }
        IPack Pack { get; }

        bool Connected { get; }

        Semaphore Connect(string addr, int port, int timeout = 0);
        void Close();
    }
}
