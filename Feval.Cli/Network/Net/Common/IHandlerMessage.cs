using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Common
{
    public interface IHandlerMessage
    {
        void HandleInitialize(IConnection connection);
        void HandleConnected(bool v);
        /// <summary>
        /// 消息数据处理接口。
        /// 注：为了极致优化本内存拷贝，故由网络层来申请PooledMemoryStream,由使用者来视情况回收，暂时违背谁污染谁治理的原则
        /// </summary>
        /// <param name="stream"></param>
        void Handle(PooledMemoryStream stream);
        void HandleDisconnected();
        void HandleClose();
    }
}
