using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Pipes
{
    class ClientInfo
    {
        public string PipeName { get; set; }
        public string ClientName { get; set; }

        
        public ClientInfo()
        { }
        public ClientInfo(string clientName, string pipeName)
        {
            this.ClientName =  clientName;
            this.PipeName = pipeName;
        }
    }
}
