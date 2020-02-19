using NetMQ.Sockets;
using NetMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace HelloWorldClient
{
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
        public void MultipartMessageTest()
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
        public void Send(IOutgoingSocket socket, byte[] message, int numBytes, bool doesBlockAllOtherTraffic)
        {
            var msg = new Msg();
            msg.InitPool(numBytes);
            message.CopyTo(msg.Data, 0);
            socket.Send(ref msg, doesBlockAllOtherTraffic);
            msg.Close();
        }
        public void HugeMessageTest()
        {
            using (var server = new ResponseSocket("@tcp://127.0.0.1:5556"))
            using (var client = new RequestSocket(">tcp://127.0.0.1:5556"))
            {
                const int bufferSize = 1024 * 1024;
                Console.WriteLine("Client sending " + bufferSize + " bytes");
                Encoding encoding = Encoding.Default;


                byte[] buffer = new byte[bufferSize];
                Send(client, buffer, bufferSize, false);

                //client.Send(buffer, false);
                //client.SendMoreFrame("A").SendFrame("Hello");
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

        SubscriberSocket prepSubscriber(string ip, ushort port, int bufferSize)
        {
            var sub = new SubscriberSocket();
            sub.Options.ReceiveBuffer = bufferSize;
            string pgm = "pgm://" + ip + ":" + port.ToString(); // "pgm://224.0.0.1:5555"
            sub.Bind(pgm);
            sub.Subscribe("");

            return sub;
        }
        public void MultiBroadcastTest()
        {
            Stopwatch sw = new Stopwatch();
            const int MegaBit = 1024;
            const int MegaByte = 1024 * MegaBit;
            using (var pub = new PublisherSocket())
            //using (var sub1 = new SubscriberSocket())
            //using (var sub2 = new SubscriberSocket())
            {
                pub.Options.MulticastHops = 2;
                pub.Options.MulticastRate = 140 * MegaBit; // 40 megabit
                pub.Options.MulticastRecoveryInterval = TimeSpan.FromMinutes(10);
                pub.Options.SendBuffer = MegaByte * 10; // 10 megabyte
                pub.Connect("pgm://224.0.0.1:5555");

                const int numConnections = 10;
                SubscriberSocket[] subs = new SubscriberSocket[numConnections];
                for (int i=0; i< numConnections; i++)
                {
                    subs[i] = prepSubscriber("224.0.0.1", 5555, MegaByte * 10);
                }
                Console.WriteLine("Server sending 'Hi'");

                /* string sentString = "hi";
                 Send(pub, Encoding.ASCII.GetBytes(sentString), 2, false);*/
                int bufferSize =  MegaByte * 3;
                byte[] buffer = new byte[bufferSize];
                sw.Start();
                Send(pub, buffer, bufferSize, false);
                Console.WriteLine("Send Elapsed={0}", sw.Elapsed);
                bool more;

                for (int i = 0; i < numConnections; i++)
                {
                    string result = subs[i].ReceiveFrameString(out more);
                    int size = result.Length;
                    Console.WriteLine("sub {1} received = '{0}'", size, i) ;
                }
                sw.Stop();

                Console.WriteLine("Receive Elapsed={0}", sw.Elapsed);
            }
        }
    }
}
