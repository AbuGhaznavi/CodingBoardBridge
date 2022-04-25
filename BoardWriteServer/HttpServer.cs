namespace BoardWriteServer
{
    // Filename:  HttpServer.cs        
    // Author:    Benjamin N. Summerton <define-private-public>        
    // License:   Unlicense (http://unlicense.org/)

    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Diagnostics;
    using System.Threading;
    using System.Web;
    namespace HttpListenerExample
    {



        class HttpServer
        {
            public static HttpListener listener;
            public static CP2112Device SFPWriter;
            public static CP2112Device CP2SearchInstance;
            public static uint selectedInt;
            public static string url = "http://localhost:42069/";
            public static int pageViews = 0;
            public static int requestCount = 0;
            public static string pageData =
                "<!DOCTYPE>" +
                "<html>" +
                "  <head>" +
                "    <title>HttpListener Example</title>" +
                "  </head>" +
                "  <body>" +
                "    <p>Page Views: {0}</p>" +
                "    <form method=\"post\" action=\"shutdown\">" +
                "      <input type=\"submit\" value=\"Shutdown\" {1}>" +
                "    </form>" +
                "  </body>" +
                "</html>";

            public static byte[] StringToByteArray(string hex)
            {
                return Enumerable.Range(0, hex.Length)
                                 .Where(x => x % 2 == 0)
                                 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                                 .ToArray();
            }

            // Check if two byte segments are the same (used for verification)
            public static bool CompareByteArray(byte[] byteArray1, byte[] byteArray2)
            {
                // Check if lengths are the same
                if (byteArray1.Length != byteArray2.Length)
                {
                    return false;
                } else
                {
                    for (int bIdx = 0; bIdx < byteArray1.Length; bIdx++)
                    {
                        if (byteArray1[bIdx] != byteArray2[bIdx])
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            // Parses an address for its 
            public static byte[] parseAddressString(String addrString, char delimiter)
            {
                string[] pieces = addrString.Split(delimiter);
                byte[] addressPieces = new byte[2];

                // Get the first part of the address string
                addressPieces[0] = StringToByteArray(pieces[0])[0];

                if (pieces.Length == 1)
                {
                    addressPieces[1] = 0;
                } else
                {
                    String addrNoT = pieces[1].Replace("T", "");

                    uint tPageValue = UInt16.Parse(addrNoT);

                    addressPieces[1] = (byte)tPageValue;
                }

                return addressPieces;
            }



           /** public static byte[] readWithPageTable(byte address, byte table_page`, CP2112Device deviceReader)
            {
                // Write to the 0x7F for devices that have multiple tables on the same address
                byte[] resultBytes = new byte[256];
                deviceReader.BoardSafeWrite(address, 0x7F, table_page);
                
            }**/

            public static byte[] readAllRequestAddress(CP2112Device deviceReader, ReadRequest rr, byte address)
            {
                uint byteStartIdx = 0;
                uint bytesPerRead = (uint) rr.readBytes;
                byte[] result = new byte[256];

                
                while (byteStartIdx < 256)
                {
                    // Read from the board
                    byte[] temp = deviceReader.BoardSafeRead(address, bytesPerRead, byteStartIdx);
                    Array.ConstrainedCopy(temp, 0, result, (int) byteStartIdx, (int) bytesPerRead);
                    byteStartIdx += bytesPerRead;
                    Thread.Sleep(rr.rwDelay);
                }

                return result;
            }

            // Assumes that 7F is the write index for page selection
            public static byte[] readAllPageSelect(CP2112Device deviceReader, ReadRequest rr, byte address, byte page)
            {
                // Write to the proper index on the board to select the page
                bool status = deviceReader.BoardSafeWrite((uint)address, 0x7F, page) ;

                return readAllRequestAddress(deviceReader, rr, address);

            }


            // Read in all bytes for a given set of pages
            // Null means device failed to read
            public static ReadReport ExecuteReadRequest(ReadRequest rr)
            {
                // Initialize 256 byte arrays for reading from the part
                byte[,] pageResults = new byte[rr.numPages, 256];
                ReadReport readReport = new ReadReport();
                ReadDataPacket[] readDataPackets = new ReadDataPacket[rr.numPages];
                readReport.dataPackets = readDataPackets;
                readReport.success = true;
                readReport.deviceStatus = "Success";
                CP2112Device deviceReader = new CP2112Device();
                deviceReader.device_index = (uint) rr.deviceIndex;
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                for (int page_idx = 0; page_idx < rr.numPages; page_idx++)
                {
                    String pageText = rr.pages[page_idx];
                    readDataPackets[page_idx] = new ReadDataPacket();
                    readDataPackets[page_idx].pageName = rr.pages[page_idx];
                    byte[] addrPieces = parseAddressString(pageText, '_');
                    byte[] curResult;

                    // SFPs do not require page select byte
                    if (rr.partType == "sfp")
                    {
                        curResult = readAllRequestAddress(deviceReader, rr, addrPieces[0]);
                    } else
                    {
                        curResult = readAllPageSelect(deviceReader, rr, addrPieces[0], addrPieces[1]);
                    }
                    
                    readDataPackets[page_idx].pageHex = Convert.ToHexString(curResult);
                    if (deviceReader.DEVICE_STATUS != "Success")
                    {
                        Console.WriteLine("Failed to read");
                        readReport.deviceStatus = deviceReader.DEVICE_STATUS;
                        readReport.success = false;
                    }
                    
                }
                stopwatch.Stop();
                // Attach the elapsed time to the 
                readReport.milliseconds = stopwatch.ElapsedMilliseconds;
                return readReport;

            }


            public static async Task HandleIncomingConnections()
            {
                bool runServer = true;

                uint page = 0xA0;




                // While a user hasn't visited the `shutdown` url, keep on handling requests
                while (runServer)
                {
                    // Will wait here until we hear from a connection
                    HttpListenerContext ctx = await listener.GetContextAsync();
                    
                    // Peel out the requests and response objects
                    HttpListenerRequest req = ctx.Request;
                    HttpListenerResponse resp = ctx.Response;

                    // Print out some info about the request
                    Console.WriteLine("Request #: {0}", ++requestCount);
                    Console.WriteLine(req.Url.ToString());
                    Console.WriteLine("Path:" + req.Url.AbsolutePath);
                    Console.WriteLine(req.HttpMethod);
                    Console.WriteLine(req.UserHostName);
                    Console.WriteLine(req.UserAgent);
                    Console.WriteLine();


                    // Allow OPTIONS


                    // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                    if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/shutdown"))
                    {
                        Console.WriteLine("Shutdown requested");
                        runServer = false;
                    } 


                    // Attempt a new Read methodology
                    if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/Read")
                    {
                        string post_data;
                        using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                        {
                            post_data = reader.ReadToEnd();
                        }

                        ReadRequest request = JsonSerializer.Deserialize<ReadRequest>(post_data);

                        

                        Console.WriteLine("READING");
                        Console.WriteLine(post_data);
                        ReadReport resultReport = ExecuteReadRequest(request);
                        string readJson = JsonSerializer.Serialize<ReadReport>(resultReport);
                        byte[] nd = Encoding.UTF8.GetBytes(readJson);
                        resp.ContentType = "text/html";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = nd.Length;
                        resp.Headers.Add("Access-Control-Allow-Origin: *");

                        await resp.OutputStream.WriteAsync(nd, 0, nd.Length);
                        continue;
                    }

                    // Handle Request for Device Indices (Per CP2112 interface)
                    if (req.HttpMethod == "GET" && (req.Url.AbsolutePath == "/GetDevices")) {
                        // Retrieve the indices
                        SFPWriter = new CP2112Device();
                        uint[] validBoardIndices = SFPWriter.searchDevices();
                        string[] indicesString = new string[validBoardIndices.Length];
                        for (int x = 0; x < validBoardIndices.Length; x++)
                        {
                            indicesString[x] = "\"CP2112 " + validBoardIndices[x].ToString() + "\"";
                        }
                        string joinedIndices = string.Join(",", indicesString);
                        string result = "{\"num_devices\":" + validBoardIndices.Length + ",\"devices\":[" + joinedIndices + "]}";
                        byte[] device_bytes = Encoding.UTF8.GetBytes(result);
                        resp.ContentType = "text/json";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = device_bytes.Length;
                        resp.Headers.Add("Access-Control-Allow-Origin: *");
                        await resp.OutputStream.WriteAsync(device_bytes, 0, device_bytes.Length);
                        continue;
                    }


                    // Handle Post Methods for Write Requests
                    if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/WritePart")
                    {
                        Console.WriteLine("Writing part");

                        string post_data;
                        using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                        {
                            post_data = reader.ReadToEnd();
                        }

                        // Deserialize WriteFields from browser input
                        WriteFields writeFields = JsonSerializer.Deserialize<WriteFields>(post_data);

                        // Get the values from the options
                        uint address =  (uint) int.Parse(writeFields.AddressFields.rwAddress, System.Globalization.NumberStyles.HexNumber);
                        uint segStart = (uint) int.Parse(writeFields.AddressFields.rwStart, System.Globalization.NumberStyles.HexNumber);
                        uint segEnd =   (uint) int.Parse(writeFields.AddressFields.rwEnd, System.Globalization.NumberStyles.HexNumber);
                        byte[] currentByteSelection = StringToByteArray(writeFields.PageInfo.curByteString);

                        

                        // Get relevant slice of input bytes
                        byte[] selectionToWrite = new byte[segEnd + 1 - segStart];

                        Array.Copy(currentByteSelection, segStart, selectionToWrite, 0, (segEnd - segStart) + 1);

                        // Process the current byte string


                        // Using the WriteFields Handle the Write
                        SFPWriter = new CP2112Device();

                        // Set the device from the fields 
                        SFPWriter.device_index = (uint)writeFields.DeviceFields.deviceIndex;
                        Stopwatch stopwatch = new Stopwatch();
                        I2CProgrammer.READ_WRITE_DELAY = writeFields.ReadWriteFields.rwDelay;
                        bool passwordWrite = false;
                        stopwatch.Start();

                       
                        

                        // If a password is provided, write the password to the part
                        if (writeFields.PasswordFields.passEnabled)
                        {
                            // Convert password write fields to necessary types
                            uint wPassAddress = (uint) int.Parse(writeFields.PasswordFields.passAddress, System.Globalization.NumberStyles.HexNumber);
                            uint wPassIndex = (uint) int.Parse(writeFields.PasswordFields.passIndex, System.Globalization.NumberStyles.HexNumber);
                            byte[] passKey = StringToByteArray(writeFields.PasswordFields.passKey);
                            passwordWrite = SFPWriter.BoardSafeWrite(wPassAddress, wPassIndex, passKey);
                        }

                        bool write_success = SFPWriter.BoardSafeWrite(address, segStart, selectionToWrite);
                        bool verification_success = false;


                        byte[] readBack = SFPWriter.BoardSafeRead(address, (segEnd - segStart) + 1, segStart);
                        verification_success = CompareByteArray(readBack, selectionToWrite);
                        stopwatch.Stop();



                        TimeSpan ts = stopwatch.Elapsed;
                        int milliseconds = ts.Milliseconds + (1000 * ts.Seconds);

                        
                        StatusReport report = new StatusReport();
                        report.success = write_success;
                        report.message = write_success ? "Success" : "Invalid Device";
                        report.extraData = "";
                        report.milliseconds = milliseconds;

                        // If verification fails send a message
                        if (!verification_success)
                        {
                            report.success = false;
                            report.message = "Verification failed on " + writeFields.AddressFields.rwAddress;
                        }

                        string reportJSON = JsonSerializer.Serialize<StatusReport>(report);

                        byte[] nd = Encoding.UTF8.GetBytes(reportJSON);
                        resp.ContentType = "text/html";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = nd.Length;
                        resp.Headers.Add("Access-Control-Allow-Origin: *");

                        // Write out to the response stream (asynchronously), then close it
                        await resp.OutputStream.WriteAsync(nd, 0, nd.Length);
                        continue;
                    }

                    // Make sure we don't increment the page views counter if `favicon.ico` is requested
                    if (req.Url.AbsolutePath != "/favicon.ico")
                        pageViews += 1;

                    // Get selected index of device if possible
                    NameValueCollection queryParams = req.QueryString;
                    if (queryParams["devIndex"] != null)
                    {
                        selectedInt = uint.Parse(queryParams["devIndex"]);
                    }


                    // Handle Parsing of URL Path
                    string mainPart = req.Url.AbsolutePath.Split("/")[1];
                    string[] locationPieces = mainPart.Split("_");

                    // Convert the first part of the of the location to address
                    byte locAddress = Convert.FromHexString(locationPieces[0])[0];


                    // Do the actual reading
                    SFPWriter = new CP2112Device();
                    SFPWriter.device_index = selectedInt;
                    byte[] read = new byte[256];

                    byte[] intermRead = SFPWriter.BoardSafeRead(locAddress, 128, 0);
                    Array.ConstrainedCopy(intermRead, 0, read, 0, 128);

                    intermRead = SFPWriter.BoardSafeRead(locAddress, 128, 128);
                    Array.ConstrainedCopy(intermRead, 0, read, 128, 128);

                    ReadReport readResult = new ReadReport();
                    readResult.deviceStatus = SFPWriter.DEVICE_STATUS;
                    if (readResult.deviceStatus == "Success")
                    {
                        readResult.success = true;
                    }

                    readResult.dataPackets = new ReadDataPacket[1];
                    readResult.dataPackets[0] = new ReadDataPacket();
                    readResult.dataPackets[0].pageName = "A0_T0";
                    readResult.dataPackets[0].pageHex = Convert.ToHexString(read);

                    String jsonReadReport = JsonSerializer.Serialize<ReadReport>(readResult);


                    Console.WriteLine(SFPWriter.DEVICE_STATUS);
                    Console.WriteLine(jsonReadReport);
                    
                    // Do Proper conversions
                    string hexString = Convert.ToHexString(read);
                    byte[] send = Encoding.UTF8.GetBytes(hexString);
                    

                    // Write the response info
                    string disableSubmit = !runServer ? "disabled" : "";
                    byte[] data = Encoding.UTF8.GetBytes(String.Format(pageData, pageViews, disableSubmit));
                    resp.ContentType = "text/html";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = send.Length;
                    resp.Headers.Add("Access-Control-Allow-Origin: *");

                    // Write out to the response stream (asynchronously), then close it
                    await resp.OutputStream.WriteAsync(send, 0, send.Length);


                    resp.Close();
                }
            }


            public static void Main(string[] args)
            {
                // Create a Http server and start listening for incoming connections
                listener = new HttpListener();
                listener.Prefixes.Add(url);
                listener.Start();
                Console.WriteLine("Listening for connections on {0}", url);

                // Handle requests
                Task listenTask = HandleIncomingConnections();
                listenTask.GetAwaiter().GetResult();
                

                // Close the listener
                listener.Close();
            }
        }
    }
}
