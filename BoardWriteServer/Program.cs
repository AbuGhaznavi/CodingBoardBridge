using System;

namespace BoardWriteServer
{
    class Program
    {

        public static UInt32 SFPDeduceDeviceIndex()
        {
            CP2112Device searchWriter = new CP2112Device();
            UInt32[] searchArray = searchWriter.searchDevices();
            
            // Deduce right index for SFP
            // May end up making deduction its own class
            for (int devIdx = 0; devIdx < searchArray.Length; devIdx++)
            {
                searchWriter.device_index = searchArray[devIdx];
                byte[] searchRead = searchWriter.BoardSafeRead(0xA0, 1, 0);

                // Return the first available device that indicates a valid read (part identified)
                if (searchRead[0] != 0)
                {
                    return searchWriter.device_index;
                }
            }
            // Return 666 on failure to find device
            return 666;
        }

        static void Test()
        {
            Console.WriteLine("Hello World!");
            CP2112Device SFPWriter = new CP2112Device();
            Console.WriteLine(SFPWriter.device_id);
            SFPWriter.device_index = SFPDeduceDeviceIndex();
            byte[] read = SFPWriter.BoardSafeRead(0xA0, 128, 0);

            // Bytes
            Console.WriteLine("============== BYTES ============");
            for (int x = 0; x < read.Length; x++)
            {
                Console.WriteLine(x.ToString() + "\t\t" + read[x]);
            }
            
        }

       /** static void Main(string[] args)
        {
            BoardHost bh = new BoardHost();


        }
       **/
    }
}
