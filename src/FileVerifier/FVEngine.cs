using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Threading;
using System.Net; 


namespace FileVerifier
{
    using DictItem = KeyValuePair<string, string>;

    public class VerifyResult
    {

        public string FilePath { get; private set; }
        public bool Verified { get; private set; }

        public VerifyResult(string path, bool verified)
        {
            FilePath = path;
            Verified = verified;
        }
    }

    public class FileChecksumGetter<T> : IDisposable where T : HashAlgorithm, new()
    {
        private T algorithm;

        // For reading status e.g. as part of worker thread
        public string CurrentFile { get; private set; }
        public long BytesTotal { get; private set; }
        public long BytesRead { get; private set; }
        public bool Running { get; private set; }
        public long PercentProgress
        {
            get
            {
                if (BytesTotal == 0)
                {
                    return 0;
                }
                return Math.Max(100, BytesRead / BytesTotal * 100);
            }
        }

        public FileChecksumGetter()
        {
            algorithm = new T();
            Reset();
        }

        public void Reset()
        {
            algorithm.Initialize();
            CurrentFile = "";
            BytesTotal = 0;
            BytesRead = 0;
            Running = false;
        }

        public string Calculate(Stream file, CancellationToken token)
        {
            // Main Entry point for calculating checksum

            Reset();
            Running = true;
            byte[] hash = _CalcChecksum(file, token);
            Running = false;

            // if canceled, hash might be gibberish, so return empty string
            if (token.IsCancellationRequested)
                return "";

            return String.Join("", hash.Select(b => b.ToString("x2")));
        }

        private byte[] _CalcChecksum(Stream stream, CancellationToken token)
        {
            int csz = 4096*4*4; // roughly 64kb
            var chunk = new byte[csz];

            // supposedly, this is the fastest method
            // of finding the current file size (at least in C++)
            if (stream.CanSeek)
            {
                long pos = stream.Position;
                stream.Seek(0, SeekOrigin.End);
                BytesTotal = stream.Position;
                stream.Position = pos;  // equivalent to file.Seek(pos, SeekOrigin.Begin)
            }

            while (!token.IsCancellationRequested)
            {
                int n = stream.Read(chunk, 0, csz);
                BytesRead += n;
                if (n == 0)
                    break;
                algorithm.TransformBlock(chunk, 0, n, chunk, 0);
                if (n < csz)
                    break;
            }
            
            // no issues if loop was cancelled
            algorithm.TransformFinalBlock(chunk, 0, 0);
            return algorithm.Hash;
        }

        public void Dispose()
        {
            algorithm.Dispose();
            algorithm = null;
        }
    }

    public class FileChecksumGetterSHA256 : FileChecksumGetter<SHA256CryptoServiceProvider> {}

    public class FVFileUtil
    {
        /// <summary>
        /// Returns the URI scheme for the provided uri. The URI is parsed as an 
        /// absolute URI. If parsing fails, the uri is assumed to be a relative filepath.
        /// </summary>
        /// <param name="rawuri">Raw URI or filename.</param>
        /// <returns>The identified URI scheme.</returns>
        internal static Uri _GetURI(string rawuri)
        {
            Uri uri;
            try
            {
                uri = new Uri(rawuri, UriKind.Absolute);
            }
            catch (UriFormatException)
            {
                return null;  // relative file path
            }
            return uri;
        }

        /// <summary>
        /// Download the file specified by `url`. 
        /// </summary>
        /// <param name="url">FTP url to download. Must include the file to download.</param>
        /// <param name="token">Cancellation token. Use CancellationToken.None if not using tokens.</param>
        /// <param name="credential">Credentials to use for login.</param>
        /// <returns></returns>
        internal static Stream _DownloadFtp(string url, Uri uri)
        {

            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = true;
            // magic here
            request.Method = WebRequestMethods.Ftp.DownloadFile; // "RETR"
            return request.GetResponse().GetResponseStream();
        }

        internal static bool _FileExistsFTP(Uri uri)
        {
            // ref: https://stackoverflow.com/questions/347897/how-to-check-if-file-exists-on-ftp-before-ftpwebrequest
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = true;
            request.Method = WebRequestMethods.Ftp.GetDateTimestamp;
            try
            {
                var response = request.GetResponse();
                return true;
            }
            catch (WebException ex)
            {
                var response = (FtpWebResponse)ex.Response;
                if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    return false;
                }
                throw;
            }
        }

        public static bool DoesFileExist(string file)
        {
            Uri uri = _GetURI(file);
            if (uri == null || uri.Scheme == "file")
                return File.Exists(file);
            else if (uri.Scheme == "ftp")
            {
                return _FileExistsFTP(uri);    
            } 
            else
            {
                return false; // fuck off
            }
        }

        public static void WriteData(string filename, string contents)
        {
            var uri = _GetURI(filename);
            if (uri == null || uri.Scheme == "file")
            {
                File.WriteAllText(filename, contents);
            }
            else
            {
                _UploadFTP(uri, contents);
            }
        }

        private static void _UploadFTP(Uri uri, string contents)
        {
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = true;
            using (var stream = request.GetRequestStream())
            {
                var buffer = Encoding.UTF8.GetBytes(contents);
                stream.Write(buffer, 0, buffer.Length);
                stream.Flush();
            }
        }

        public static Stream GetContentStream(string filename)
        {
            Stream stream;
            var uri = _GetURI(filename);
            if (uri == null)
            {
                stream = File.OpenRead(filename); // relative filepath
            }
            else
            {
                switch (uri.Scheme)
                {
                    case "file":
                        stream = File.OpenRead(filename);
                        break;
                    case "ftp":
                        stream = _DownloadFtp(filename, uri);
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported scheme: \"{uri.Scheme}\".");
                }
            }

            return stream;
        }
    }

    public class FVEngine : IDisposable
    {
        // event-based verification alert (useful for background threading)
        public delegate void ResultHandler(VerifyResult result);
        public event ResultHandler OnFileTested;

        private Dictionary<string, string> Specs;
        private FileChecksumGetterSHA256 verifier;
        private CancellationToken defaultToken;

        public FVEngine()
        {
            verifier = new FileChecksumGetterSHA256();
            defaultToken = CancellationToken.None;
            Specs = new Dictionary<string, string>();
        }

        public void Load(string filepath)
        {
            string contents;
            using (var stream = FVFileUtil.GetContentStream(filepath))
            using (var reader = new StreamReader(stream))
            {
                contents = reader.ReadToEnd();
            }
                
            Specs = JsonConvert.DeserializeObject<Dictionary<string, string>>(contents);
        }

        public void Save(string filepath)
        {
            var contents = JsonConvert.SerializeObject(Specs, Formatting.Indented);
            FVFileUtil.WriteData(filepath, contents);
        }

        /// <summary>
        /// Calculate checksum for a file. The file may be a local file, or a file accessible via FTP. 
        /// This function should be the primary entrypoint to calculating checksum based on a filepath or
        /// URL. 
        /// </summary>
        /// <param name="filename">Full filepath or absolute FTP url</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>Checksum for the requested file.</returns>
        public string GetChecksum(string filename, CancellationToken? token=null)
        {
            Stream stream = FVFileUtil.GetContentStream(filename);
            using (stream)
            {
                return verifier.Calculate(stream, token ?? defaultToken);
            }
        }

        /// <summary>
        /// Get checksum of the provided text string. The string is converted to 
        /// a byte array using Encoding.UTF8.GetBytes(). 
        /// </summary>
        /// <param name="text">Text string to calculate checksum for.</param>
        /// <param name="token">Optional cancellation token</param>
        /// <returns>Resulting checksum.</returns>
        public string GetChecksumRaw(string text, CancellationToken? token = null)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            {
                return verifier.Calculate(stream, token ?? defaultToken);
            }
        }

        public string GetChecksum(string filename)
        {
            return GetChecksum(filename, defaultToken);
        }

        public void VerifyOne(string filename, string checksum)
        {
            VerifyOne(filename, checksum, defaultToken);
        }

        private void SendVerifyResult(string path, bool match)
        {
            var result = new VerifyResult(path, match);
            OnFileTested?.Invoke(result);
        }

        public bool VerifyOne(string filename, string checksum, CancellationToken token)
        {
            string found_hash = GetChecksum(filename, token);
            var match = found_hash == checksum;
            SendVerifyResult(filename, match);
            return match;
        }

        public bool VerifyAll()
        {
            return VerifyAll(defaultToken);
        }

        public bool VerifyAll(CancellationToken? token)
        {
            var result = true;
            var localtoken = token ?? defaultToken;
            foreach (var item in Specs)
            {
                localtoken.ThrowIfCancellationRequested();
                result = result && VerifyOne(item.Key, item.Value, localtoken);
            }
            return result;
        }
        
        /// <summary>
        /// Add a file to the engine store by specifying a uri of a file on 
        /// a filesystem or via FTP. Returns the calculated checksum.
        /// </summary>
        /// <param name="file_uri">Source file to obtain for checksum.</param>
        /// <param name="target_path">Target filepath.</param>
        /// <param name="token">Optional cancellation token.</param>
        /// <returns>Checksum. </returns>
        public string AddFileAuto(string file_uri, string target_path, CancellationToken? token=null)
        {
            
            string checksum = GetChecksum(file_uri, token ?? defaultToken);
            AddFile(target_path, checksum);
            return checksum;  // for convenience / chaining
        }

        public void AddFile(string file, string checksum)
        {
            Specs[file] = checksum;
        }

        public void AddNewFile(string path, string checksum)
        {
            Specs.Add(path, checksum);
        }

        public string AddNewFileAuto(string local_path, string target_path, CancellationToken? token=null)
        {
            string checksum = GetChecksum(local_path, token ?? defaultToken);
            AddNewFile(target_path, checksum);
            return checksum;
        }

        public void RemoveFile(string file)
        {
            Specs.Remove(file);
        }

        public List<DictItem> GetData()
        {
            return GetData<List<DictItem>>();
        }

        // Generic allows to e.g. pass in ObservableCollection
        public T GetData<T>() where T : IList<DictItem>, new()
        {
            var ret = new T();
            foreach (var item in Specs)
            {
                ret.Add(item);
            }
            return ret;
        }

        public void Dispose()
        {
            verifier.Dispose();

            verifier = null;
            Specs = null;
        }
    }
}
