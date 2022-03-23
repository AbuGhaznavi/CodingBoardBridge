using System;
using System.Collections.Generic;
using System.Linq;

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

    // A class to get converted to JSON after
    public class StatusReport
    {
        public String message { get; set; }
        public bool success { get; set; }
        public String extraData { get; set; } 
        public int milliseconds { get; set; }
    }

    // Create a class to contain the post data from the WriteRequest
    public class AddressFields
    {
        public string rwAddress { get; set; }
        public string rwStart { get; set; }
        public string rwEnd { get; set; }
    }

    public class PasswordFields
    {

        public bool passEnabled { get; set; }
        public string passAddress { get; set; }
        public string passIndex { get; set; }
        public string passKey { get; set; }
        public bool executeEnabled { get; set; }
        public string executeMethod { get; set; }
        public bool keygenEnabled { get; set; }
        public string keygenMethod { get; set; }

    }

    public class ReadWriteFields
    {
        public int dataRate { get; set; }
        public int readBytes { get; set; }
        public int writeBytes { get; set; }
        public bool overwriteSerial { get; set; }
        public bool overwriteDate { get; set; }
        public int rwDelay { get; set; }
    }

    public class PageInfo
    {
        public string selectedType { get; set; }
        public string page { get; set; }
        public bool allPages { get; set; }
        public string curByteString { get; set; }
        public List<string> byteStrings { get; set; }
    }

    public class DeviceFields
    {
        public string deviceName { get; set; }
        public int deviceIndex { get; set; }
    }

    public class WriteFields
    {
        public PageInfo PageInfo { get; set; }
        public ReadWriteFields ReadWriteFields { get; set; }
        public PasswordFields PasswordFields { get; set; }
        public AddressFields AddressFields { get; set; }
        public DeviceFields DeviceFields { get; set; }
    }
}
