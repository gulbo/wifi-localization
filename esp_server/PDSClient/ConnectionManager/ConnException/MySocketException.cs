using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDSClient.ConnectionManager.ConnException
{
    class MySocketException : Exception
    {
        public int Status { get; private set; }

        public const int SOCKET_ERR = 0;
        public const int INVALID_SSID_LEN = 1;

        public MySocketException() : base()
        {
            Status = SOCKET_ERR;
        }
        public MySocketException(String msg) : base(msg)
        {
            Status = SOCKET_ERR;
        }
        public MySocketException(int status) : base()
        {
            Status = status;
        }
    }
}
