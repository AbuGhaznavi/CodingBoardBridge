namespace BoardWriteServer
{
    // Filename:  HttpServer.cs        
    // Author:    Benjamin N. Summerton <define-private-public>        
    // License:   Unlicense (http://unlicense.org/)

    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using System.Collections.Specialized;

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
                    } else if (req.HttpMethod == "GET")
                    {

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
                        byte[] nd = Encoding.UTF8.GetBytes("Test response");
                        resp.ContentType = "text/html";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = nd.Length;
                        resp.Headers.Add("Access-Control-Allow-Origin: *");

                        string post_data;
                        using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                        {
                            post_data = reader.ReadToEnd();
                        }
                        Console.WriteLine("POSTED DATA:");
                        Console.WriteLine(post_data);

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
                    SFPWriter.searchDevices();
                    SFPWriter.device_index = selectedInt;
                    byte[] read = new byte[256];

                    byte[] intermRead = SFPWriter.BoardSafeRead(locAddress, 128, 0);
                    Array.ConstrainedCopy(intermRead, 0, read, 0, 128);

                    intermRead = SFPWriter.BoardSafeRead(locAddress, 128, 128);
                    Array.ConstrainedCopy(intermRead, 0, read, 128, 128);


                    
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
