using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Net.Common
{
    public interface IServer
    {
        Socket Socket { get; }
        IHandlerServer Handler { get; }
        bool Running { get; }
        IPEndPoint EndPoint { get; }
        IPack Pack { get; }
        void Start();
        void Stop();
    }
}
