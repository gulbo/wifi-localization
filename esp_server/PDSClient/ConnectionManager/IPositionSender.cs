using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDSClient.ConnectionManager
{
    public interface IPositionSender
    {
        void WaitAll();
        void WaitAny();
    }
}
