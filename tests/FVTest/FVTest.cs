using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Net;
using System.Diagnostics;

namespace FVTest
{
    class FVTest
    {
        static void print<T>(T ob)
        {
            Console.WriteLine(ob);
        }
        static void Main(string[] args)
        {
            string ddir = @"C:\Users\Nathan\Source\Repos\FileVerifier\FVUnitTest\data";
            Directory.SetCurrentDirectory(ddir);
            var create = Directory.GetDirectories(@".\case\create", @"test_*");
            var verify = Directory.GetDirectories(@".\case\verify", @"test_*");
            var calc = Directory.GetDirectories(@".\case\calc", @"test_*");

            print("Create");
            show_found(create);
            print("Verify");
            show_found(verify);
            print("Calc");
            show_found(calc);

            //show_found(found);
        }

        private static void show_found(string[] found)
        {
            foreach (var f in found)
                Console.WriteLine($"Found: {f}");
        }

        static void Main4(string[] args)
        {
            //var dir = "C:\\users\\nathan\\downloads\\foo.txt";
            //Console.WriteLine(Path.GetDirectoryName(dir));
            //Console.WriteLine(Path.GetExtension(dir));
            Directory.SetCurrentDirectory("..\\..\\..\\..\\FVUnitTest\\data");
            Console.WriteLine(Path.GetFullPath("."));
            //var proc = launchFTP(".");
            //var url = "ftp://fvconsole_user:uRttUR3MRsF4z6UUngZ3@127.0.0.1/in.txt";
            var url = "ftp://100.100.100.145/config/System Variables.sys";
            Uri uri = new Uri(url);
            foreach (var p in uri.GetType().GetProperties())
            {
                Console.WriteLine($"{p.Name}: {p.GetValue(uri)}");
            }
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = true;

            Console.WriteLine("");
            var stream = request.GetResponse().GetResponseStream();
            using (var reader = new StreamReader(stream))
                Console.WriteLine(reader.ReadToEnd());
        }

        static void Main3(string[] args)
        {
            var buf = new byte[0];
            var sha256 = new SHA256CryptoServiceProvider();
            sha256.TransformFinalBlock(buf, 0, 0);
            var tester = new AutoTest();
            tester.Test();

            const string ftp_uri = "ftp://100.100.100.145/config/System Variables.sys";
            var request = (FtpWebRequest)WebRequest.Create(ftp_uri);
            request.Credentials = new NetworkCredential("anon", "12345");
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;

            request.Method = WebRequestMethods.Ftp.DownloadFile;

            var rsp = request.GetResponse();

            Console.WriteLine("Got Response:");
            using (var reader = new StreamReader(rsp.GetResponseStream()))
            {
                Console.WriteLine(reader.ReadToEnd());
            }

            Console.WriteLine("\n\n\n\n\n");

            Console.WriteLine("Absolute File URI");
            var uri = new Uri("C:\\foo\\bar.txt");
            ShowProps(uri);

            //Console.WriteLine("Relative File URI");
            //var uri3 = new Uri("\\foo\\..\\..\\bar.txt", UriKind.Absolute);
            //ShowProps(uri3);

            Console.WriteLine("\nFTP URI");
            var uri2 = new Uri(ftp_uri.ToUpper());
            ShowProps(uri2);

            Console.WriteLine("\n\n\n\n\n");
            var eng = new FileVerifier.FVEngine();
            var cs = eng.AddFileAuto(ftp_uri, "C:\\ftp\\out.txt");
            Console.WriteLine(cs);
            foreach (var item in eng.GetData())
            {
                Console.WriteLine($"{item.Key}: {item.Value}");
            }
        }

        static void ShowProps<T>(T obj)
        {
            foreach (var prop in obj.GetType().GetProperties())
            {
                Console.WriteLine($"{prop.Name} = {prop.GetValue(obj)}");
            }
        }
        
        static void BoolTest ()
        {
            bool result = true;

            result = comb(result, true);
            result = comb(result, true);
            result = comb(result, true);

            Console.WriteLine($"Result: {result}");

            result = comb(result, true);
            result = comb(result, false);
            result = comb(result, true);

            Console.WriteLine($"Result: {result}");

            result = comb(result, true);
            result = comb(result, false);
            result = comb(result, false);

            Console.WriteLine($"Result: {result}");
        }
        static bool comb(bool a, bool b)
        {
            return a && b;
        }

    }

    [AttributeUsage(AttributeTargets.Method)]
    class TestMethodAttribute : Attribute
    {
        public TestMethodAttribute() { }
    }

    class AutoTest
    {
        /// <summary>
        /// Simple, poorly designed automatic test class to run through
        /// hardcoded tests. 
        /// </summary>
        string TestDir;
        public AutoTest(string test_dir=null)
        {
            TestDir = test_dir ?? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\files"));
        }

        private string GetFilePath(string filename)
        {
            return Path.Combine(TestDir, filename);
        }

        private List<MethodInfo> GetTestMethods()
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
            return this.GetType().GetMethods(flags).Where((methodinfo) =>
            {
                return methodinfo.GetCustomAttributes(typeof(TestMethodAttribute), false).Any();
            }).ToList();
        }

        public void Test()
        {
            var methods = GetTestMethods();

            int passed = 0;
            int total = methods.Count;
            if (total == 0)
            {
                Console.WriteLine("No tests found.");
                return;
            }
            Console.WriteLine($"Running {total} tests.");

            foreach (var m in methods)
            {
                Console.Write($"Running '{m.Name}' ... ");
                bool result;
                try
                {
                    result = (bool)m.Invoke(this, null);
                }
                catch (TargetInvocationException e)
                {
                    Console.WriteLine($"error ({e.InnerException.ToString()})");
                    continue;
                }

                string msg;
                if (result)
                {
                    msg = "Pass";
                    passed += 1;
                } 
                else
                {
                    msg = "Fail";
                }
                Console.WriteLine(msg);
            }

            Console.WriteLine($"Testing complete: {passed}/{total} passing.");
        }

        [TestMethodAttribute]
        private bool TestFileChecksumReuse()
        {
            var f1 = GetFilePath("SV_Air_IM226J.sys");
            var f2 = GetFilePath("SV_Mag_IM226J.sys");
            return _DoFileChecksumReuse(f1, f2);

        }

        private bool _DoFileChecksumReuse(string fn1, string fn2)
        {

            var f1 = GetFilePath("SV_Air_IM226J.sys");
            var f2 = GetFilePath("SV_Mag_IM226J.sys");

            var checker = new FileVerifier.FileChecksumGetterSHA256();
            var checker2 = new FileVerifier.FileChecksumGetterSHA256();

            using (var source = new CancellationTokenSource())
            {
                var token = source.Token;
                string cs1, cs2, cs3;

                using (Stream s1 = File.OpenRead(f1),
                        s2 = File.OpenRead(f2))
                {
                    cs1 = checker.Calculate(s1, token);
                    cs2 = checker.Calculate(s2, token);

                    s2.Seek(0, SeekOrigin.Begin);
                    cs3 = checker2.Calculate(s2, token);
                }

                return cs2 == cs3;
            }

            
        }
    }

}
