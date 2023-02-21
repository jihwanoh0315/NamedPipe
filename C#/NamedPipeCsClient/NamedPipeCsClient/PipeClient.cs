using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading;

public class PipeClient
{
    const int BUFFER_SIZE = 512;
    private static int numClients = 1;

    public static void Main(string[] args)
    {
        Console.WriteLine("-------------This is C# Client----------------------------");
        if (args.Length > 0)
        {
            if (args[0] == "spawnclient")
            {
                var pipeClient =
                    new NamedPipeClientStream(".", "mynamedpipe",
                        PipeDirection.InOut, PipeOptions.None,
                        TokenImpersonationLevel.Impersonation);

                pipeClient.Connect();
                Console.WriteLine("Connected to pipe.");
                Console.WriteLine("There are currently {0} pipe server instances open.",
               pipeClient.NumberOfServerInstances);

                var ss = new StreamString(pipeClient);

                ss.WriteString("Good to see you!");
                // Validate the server's signature string.

                while (true)
                {
                    string rdString = ss.ReadString();
                    Console.WriteLine("Read : " + rdString);
                    if (rdString == "I am the one true server!")
                    {
                        // The client security token is sent with the first write.
                        // Send the name of the file whose contents are returned
                        // by the server.
                        ss.WriteString("c:\\textfile.txt");

                        // Print the file to the screen.
                        Console.Write(ss.ReadString());
                    }
                    else
                    {
                        Console.WriteLine("Server could not be verified.");
                    }
                }
                pipeClient.Close();
                // Give the client process some time to display results before exiting.
            }
        }
        else
        {
            Console.WriteLine("\n*** Named pipe client stream with impersonation example ***\n");
            StartClients();
        }
    }

    // Helper function to create pipe client processes
    private static void StartClients()
    {
        string currentProcessName = Environment.CommandLine;

        // Remove extra characters when launched from Visual Studio
        currentProcessName = currentProcessName.Trim('"', ' ');

        currentProcessName = Path.ChangeExtension(currentProcessName, ".exe");
        Process[] plist = new Process[numClients];

        Console.WriteLine("Spawning client processes...\n");

        if (currentProcessName.Contains(Environment.CurrentDirectory))
        {
            currentProcessName = currentProcessName.Replace(Environment.CurrentDirectory, String.Empty);
        }

        // Remove extra characters when launched from Visual Studio
        currentProcessName = currentProcessName.Replace("\\", String.Empty);
        currentProcessName = currentProcessName.Replace("\"", String.Empty);

        int i;
        for (i = 0; i < numClients; i++)
        {
            // Start 'this' program but spawn a named pipe client.
            plist[i] = Process.Start(currentProcessName, "spawnclient");
        }
        while (i > 0)
        {
            for (int j = 0; j < numClients; j++)
            {
                if (plist[j] != null)
                {
                    if (plist[j].HasExited)
                    {
                        Console.WriteLine($"Client process[{plist[j].Id}] has exited.");
                        plist[j] = null;
                        i--;    // decrement the process watch count
                    }
                    else
                    {
                        Thread.Sleep(250);
                    }
                }
            }
        }
        Console.WriteLine("\nClient processes finished, exiting.");
    }
}

// Defines the data protocol for reading and writing strings on our stream.
public class StreamString
{
    private Stream ioStream;
    private UnicodeEncoding streamEncoding;

    public StreamString(Stream ioStream)
    {
        this.ioStream = ioStream;
        streamEncoding = new UnicodeEncoding();
    }

    public string ReadString()
    {
        int len = 512;
        //len = ioStream.ReadByte() * 256;
        //len += ioStream.ReadByte();
        var inBuffer = new byte[len];
        ioStream.Read(inBuffer, 0, len);
        Console.WriteLine("len : " + len);
        return streamEncoding.GetString(inBuffer);
    }

    public int WriteString(string outString)
    {
        Console.WriteLine("Write : "+ outString);
        byte[] outBuffer = streamEncoding.GetBytes(outString);
        int len = outBuffer.Length;
        if (len > UInt16.MaxValue)
        {
            len = (int)UInt16.MaxValue;
        }
        //ioStream.WriteByte((byte)(len / 256));
        //ioStream.WriteByte((byte)(len & 255));
        ioStream.Write(outBuffer, 0, len);
        ioStream.Flush();

        return outBuffer.Length + 2;
    }
}