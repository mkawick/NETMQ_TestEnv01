using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NetMQ;
using NetMQ.Sockets;

namespace GenericTesting
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            NetMQTests nmq = new NetMQTests();

            nmq.Test02();
            Console.ReadKey();
        }

    }

    public class NetMQTests
    {
        public void Test03()
        {
            using (var server = new ResponseSocket("@tcp://127.0.0.1:5556"))
            using (var client = new RequestSocket(">tcp://127.0.0.1:5556"))
            {
                client.SendFrame("Hello");
                string fromClientMessage = server.ReceiveFrameString();
                Console.WriteLine("From Client: {0}", fromClientMessage);
                server.SendFrame("Hi Back");
                string fromServerMessage = client.ReceiveFrameString();
                Console.WriteLine("From Server: {0}", fromServerMessage);
                Console.ReadLine();
            }
        }
        public void Test01()
        {
            using (var end1 = new PairSocket())
            using (var end2 = new PairSocket())
            {
                end1.Bind("inproc://inproc-demo");
                end2.Connect("inproc://inproc-demo");
                var end1Task = Task.Run(() =>
                {
                    Console.WriteLine("ThreadId = {0}", Thread.CurrentThread.ManagedThreadId);
                    Console.WriteLine("Sending hello down the inproc pipeline");
                    end1.SendFrame("Hello");
                });
                var end2Task = Task.Run(() =>
                {
                    Console.WriteLine("ThreadId = {0}", Thread.CurrentThread.ManagedThreadId);
                    var message = end2.ReceiveFrameString();
                    Console.WriteLine(message);
                });
                Task.WaitAll(new[] { end1Task, end2Task });
            }
        }
        public void Test02()
        {
            using (var server = new ResponseSocket("@tcp://127.0.0.1:5556"))
            using (var client = new RequestSocket(">tcp://127.0.0.1:5556"))
            {
                // client sends message consisting of two frames
                Console.WriteLine("Client sending");
                client.SendMoreFrame("A").SendFrame("Hello");
                // server receives frames
                bool more = true;
                while (more)
                {
                    string frame = server.ReceiveFrameString(out more);
                    Console.WriteLine("Server received frame={0} more={1}",
                        frame, more);
                }
                Console.WriteLine("================================");
                // server sends message, this time using NetMqMessage
                var msg = new NetMQMessage();
                msg.Append("From");
                msg.Append("Server");
                Console.WriteLine("Server sending");
                server.SendMultipartMessage(msg);
                // client receives the message
                msg = client.ReceiveMultipartMessage();
                Console.WriteLine("Client received {0} frames", msg.FrameCount);
                foreach (var frame in msg)
                    Console.WriteLine("Frame={0}", frame.ConvertToString());
                Console.ReadLine();
            }
        }
    }
}
