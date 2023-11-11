using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Net.Common
{
    public delegate IConnection NewConnFunc(IHandlerMessage handler, Action<IConnection> close);
    public interface IHandlerServer
    {
        void Close();
        void Add(IConnection conn);
        void Remove(IConnection conn);
        IHandlerMessage HandleAcceptConnected(NewConnFunc newConnFunc);
    }
}
