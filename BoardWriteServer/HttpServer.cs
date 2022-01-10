using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoardWriteServer
{
    // Filename:  HttpServer.cs        
    // Author:    Benjamin N. Summerton <define-private-public>        
    // License:   Unlicense (http://unlicense.org/)

    using System;
    using System.IO;
    using System.Text;
    using System.Net;
    using System.Threading.Tasks;

    namespace HttpListenerExample
    {
        class HttpServer
        {
            public static HttpListener listener;
            public static CP2112Device SFPWriter;
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


                    // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                    if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/shutdown"))
                    {
                        Console.WriteLine("Shutdown requested");
                        runServer = false;
                    } else if (req.HttpMethod == "GET")
                    {

                    }

                    // Make sure we don't increment the page views counter if `favicon.ico` is requested
                    if (req.Url.AbsolutePath != "/favicon.ico")
                        pageViews += 1;


                    // Handle Parsing of URL Path
                    string mainPart = req.Url.AbsolutePath.Split("/")[1];
                    string[] locationPieces = mainPart.Split("_");

                    // Convert the first part of the of the location to address
                    byte locAddress = Convert.FromHexString(locationPieces[0])[0];


                    // Do the actual reading
                    SFPWriter = new CP2112Device();
                    SFPWriter.device_index = Program.SFPDeduceDeviceIndex();
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
