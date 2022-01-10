using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace BoardWriteServer
{
    class BoardHost
    {

        public BoardHost()
        {
            Int32 port = 1337;
            IPAddress localAddr = IPAddress.Parse("192.168.13.128");
            TcpListener server = new TcpListener(localAddr, port);

            server.Start();

            Byte[] bytes = new Byte[128];
            String data = null;

            while (true)
            {
                Console.WriteLine("Waiting for connection...");
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Connected!");
                data = null;

                NetworkStream stream = client.GetStream();

                CP2112Device SFPWriter = new CP2112Device();
                Console.WriteLine(SFPWriter.device_id);
                SFPWriter.device_index = Program.SFPDeduceDeviceIndex();
                byte[] read = SFPWriter.BoardSafeRead(0xA0, 128, 0);

                // Bytes

                /**
                Console.WriteLine("============== BYTES ============");
                for (int x = 0; x < read.Length; x++)
                {
                    Console.WriteLine(x.ToString() + "\t\t" + read[x]);
                } **/

                stream.Write(read, 0, 128);
            }

        }
    }
}
