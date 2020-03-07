using CsvHelper;
using FileVerifierConsole;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FVUnitTest.lib
{

    internal class FileCleaner : IDisposable
    {
        List<string> Files;
        internal FileCleaner()
        {
            Files = new List<string>();
        }
        internal void Add(string filename)
        {
            Files.Add(filename);
        }
        internal void JustKidding(string filename)
        {
            Files.Remove(filename);
        }
        internal static FileCleaner New()
        {
            return new FileCleaner();
        }
        public void Dispose()
        {
            if (Files == null)
                return;
            foreach(var f in Files)
            {
                try
                {
                    File.Delete(f);
                }
                catch (Exception)  // global
                {
                    Console.WriteLine($"Warning: Unable to delete '{f}'");
                }
            }
            Files = null;
        }
        ~FileCleaner()
        {
            Dispose();
        }
    }

    /// <summary>
    /// Redirects console and debug output to a socket connected at the provided
    /// hostname and port. 
    /// </summary>
    class SocketConsoleSender : IDisposable
    {
        Socket socket;
        NetworkStream stream;
        StreamWriter writer;
        TextWriter _old_stdout;
        TextWriterTraceListener _debug_listener;
        SocketConsoleSender(string ip, int port)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            var result = socket.BeginConnect(ip, port, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(3000, true);
            if (socket.Connected)
            {
                socket.EndConnect(result);
                stream = new NetworkStream(socket);
                writer = new StreamWriter(stream)
                {
                    AutoFlush = true
                };
                _old_stdout = Console.Out;
                
                Console.SetOut(writer);
                if (GlobalData.CaptureDebug)
                {
                    _debug_listener = new TextWriterTraceListener(writer);
                    Debug.Listeners.Add(_debug_listener);
                }
                Console.WriteLine($"SocketConsoleSender: Remote console connected at {ip}:{port}");
            }
            else
            {
                socket.Close();
                stream = null;
                writer = null;
                throw new IOException($"Failed to connect to remote console: '{ip}:{port}'");
            }
        }

        static SocketConsoleSender _Instance = null;
        public static SocketConsoleSender Instance { get { return _Instance; } }

        public static void Init(string ip, int port)
        {
            if (_Instance != null)
            {
                Debug.WriteLine("New Instance");
                _Instance.Dispose();
            }
            _Instance = new SocketConsoleSender(ip, port);
        }

        bool disposed = false;
        public void Dispose()
        {
            if (!disposed)
            {
                // close & shutdown, ignore errors
                try { socket.Shutdown(SocketShutdown.Both); } catch { }
                try { socket.Disconnect(false); } catch { }
                try { socket.Close(); socket.Dispose(); } catch { }
                try { writer.Close(); writer.Dispose(); } catch { }
                try { stream.Close(); stream.Dispose(); } catch { }
                

                if (_debug_listener != null) { try { Trace.Listeners.Remove(_debug_listener); } catch { } }
                try { Console.SetOut(_old_stdout); } catch { }

                writer = null;
                stream = null;
                socket = null;
                _debug_listener = null;
                _old_stdout = null;
                disposed = true;
            }
        }
    }

    internal class MyCSVWriter : IDisposable
    {
        StreamWriter _w;
        CsvWriter _csv;
        public MyCSVWriter(string filename)
        {
            _w = new StreamWriter(filename);
            _csv = new CsvWriter(_w, CultureInfo.CurrentCulture);
        }

        internal static MyCSVWriter _initCSV(string filename, params string[] header)
        {
            var csv = new MyCSVWriter(filename);
            csv.AddRow(header);
            return csv;
        }

        public void AddRow<T>(T data) where T: IEnumerable<string>
        {
            foreach (var d in data)
                _csv.WriteField(d);
            _csv.NextRecord();
        }

        public void AddRow(params string[] data)
        {
            foreach (var d in data)
                _csv.WriteField(d);
            _csv.NextRecord();
        }

        bool _disposed = false;
        public void Dispose()
        {
            if (!_disposed)
            {
                try { _csv.Dispose(); _csv = null; } catch { }
                try { _w.Dispose(); _w = null; } catch { }
                _disposed = true;
            }
        }
    }

    class FVTestUtil
    {

        internal static void ShutdownConsoleHook()
        {
            SocketConsoleSender.Instance.Dispose();
        }

        internal static void InitConsoleHookDefault()
        {
            try
            {
                SocketConsoleSender.Init("127.0.0.1", 56521);
            }
            catch (IOException)
            {
                InitConsoleHook(56521);
            }
        }

        private static void InitConsoleHook(int port = -1)
        {
            string host;
            (host, port) = LaunchRemoteConsoleViaSubProc(port);
            SocketConsoleSender.Init(host, port);
        }

        private static (string, int) LaunchRemoteConsoleViaSubProc(int port)
        {
            string host;
            var remote_console_info = new ProcessStartInfo()
            {
                FileName = "python",
                Arguments = $"..\\scripts\\launch_console.py --use localhost:{port}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Normal
            };
            using (var proc = Process.Start(remote_console_info))
            {
                //proc.WaitForExit(3000);
                var msg = proc.StandardOutput.ReadLine();
                Debug.WriteLine(msg);
                var parts = msg.Split(':');
                host = parts[0];

                if (host == "<error>")
                    throw new Exception("Failed to connect to remote console");
                port = Int32.Parse(parts[1]);
            }

            return (host, port);
        }

        static internal Process launchFTP(string dir)
        {
            var arg = Path.GetFullPath($"..\\scripts\\ftp_host.py");  // necessary since changing Working Directory
            var info = new ProcessStartInfo()
            {
                FileName = "python",
                Arguments = arg,
                UseShellExecute = false,
                CreateNoWindow = false,
                WorkingDirectory = dir,
                RedirectStandardOutput = false,
            };
            return Process.Start(info);
        }

        static internal Process RunProcess(string cmdline, string cwd=null)
        {
            Debug.WriteLine($"Attempting to start EXE with cmd line: {cmdline}");
            if (cwd != null)
                Debug.WriteLine($"Starting with CWD={cwd}");
            var info = new ProcessStartInfo()
            {
                FileName = GlobalData.EXE,
                Arguments = cmdline,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            if (cwd != null)
                info.WorkingDirectory = cwd;
            var process = Process.Start(info);
            process.WaitForExit();
            return process;
        }
    }

}
