using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDSClient.ConnectionManager.ConnException
{
    class DBException : Exception
    {
        public DBException() : base("Error with the connection to the database. Please check if the database is online.") { }
        public DBException(String msg) : base(msg) { }
    }
}
