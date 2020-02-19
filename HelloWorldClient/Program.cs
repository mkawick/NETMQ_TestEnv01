using System;
using System.Collections.Generic;
using System.Linq;
//using NetworkCommsDotNet;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NetMQ;
using NetMQ.Sockets;

namespace HelloWorldClient
{
    class Program
    {
        
        
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            NetMQTests nmq = new NetMQTests();

            //nmq.MultiBroadcastTest();
            //nmq.MultipartMessageTest();
            nmq.Test02();
            nmq.HugeMessageTest();
            Console.ReadKey();
        }
        
    }
   
}
