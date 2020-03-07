using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Net.Sockets;
using System.Text;
using System.Globalization;
using FileVerifierConsole;
using CsvHelper;
using FVUnitTest.lib;

namespace FVUnitTest
{
    public class GlobalData
    {
        public static readonly string DataDirRelative = ".\\..\\..\\data\\";
        public static readonly string DataDirFull = Path.GetFullPath(DataDirRelative);
        public static readonly string DefaultDir = Directory.GetCurrentDirectory();
        public static readonly string EXE = Path.Combine(DefaultDir, "FileVerifierConsole.exe");
        public static readonly bool CaptureDebug = true;
    }

    [TestClass]
    public class FVTester
    {
        private TestContext _ctx;
        public TestContext TestContext { get { return _ctx; } set { _ctx = value; } }

        [ClassInitialize]
        public static void ClassInitialize(TestContext ctx)
        {
            FVTestBootstrap.Initialize(ctx);
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            Debug.WriteLine("Cleaning up! :)");
            FVTestUtil.ShutdownConsoleHook();
        }

        internal string Column(int i)
        {
            var obj = TestContext.DataRow[i];
            if (obj is System.DBNull)
                return String.Empty;
            return (string)obj;
        }

        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "DDT-calc.csv", "DDT-calc#csv", DataAccessMethod.Sequential)]
        [TestMethod]
        public void TestCalc()
        {
            //using (var calc = MyCSVWriter._initCSV("DDT-calc.csv", "name", "verb", "input_file", "checksum", "verbose"))

            var folder = Column(0);
            var verb = Column(1); // unused, "calc"
            var input = Column(2);
            var checksum = Column(3);
            var ftp = Column(4);
            var verbose = Boolean.Parse(Column(5));

            Process ftp_proc = null;
            if (!String.IsNullOrWhiteSpace(ftp))
            {
                ftp_proc = FVTestUtil.launchFTP(folder);
                input = $"{ftp}{input}";
            }

            string cmdline = $"calc \"{input}\"";

            if (verbose)
                cmdline += " --verbose";

            var proc = FVTestUtil.RunProcess(cmdline, folder);
            if (ftp_proc != null)
            {
                ftp_proc.Kill();
                ftp_proc.WaitForExit();
            }
                
            Assert.AreEqual(0, proc.ExitCode, folder);
            string line = proc.StandardOutput.ReadLine();
            Assert.AreEqual(checksum, line, folder);
        }
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "DDT-create.csv", "DDT-create#csv", DataAccessMethod.Sequential)]
        [TestMethod]
        public void TestCreate()
        {
            //using (var create = MyCSVWriter._initCSV("DDT-create.csv", "name", "verb", "batchmode", "input_file", "target_file", "expected", "append", "verbose"))
            var folder = Column(0);
            var verb = Column(1); // "create", unused
            var isbatch = Boolean.Parse(Column(2));
            var input = Column(3);
            var target = Column(4); // unused
            var expected = Column(5);
            var output = Column(6);
            var append = Boolean.Parse(Column(7));
            var verbose = Boolean.Parse(Column(8));

            using (var cleaner = FileCleaner.New())
            {
                // this is the sloppiest code ever 
                if (append)
                {
                    // copy to out-append.json for the test to avoid overwriting generated artifacts
                    var out_append = "out-append.json";
                    var full_out_append = Path.Combine(folder, out_append);
                    File.Copy(Path.Combine(folder, output), full_out_append, true);
                    output = out_append;
                    cleaner.Add(full_out_append);
                }
                else
                {
                    cleaner.Add(Path.Combine(folder, output));
                }
                if (!String.IsNullOrWhiteSpace(target))
                    cleaner.Add(target);

                string cmdline;
                if (isbatch)
                {
                    cmdline = $"create -b -i \"{input}\" -o \"{output}\"";
                }
                else
                {
                    cmdline = $"create -i \"{input}\" -t \"target.txt\" -o \"{output}\"";
                }
                if (append)
                    cmdline += " -a";

                var proc = FVTestUtil.RunProcess(cmdline, folder);
                Assert.AreEqual(0, proc.ExitCode);
                var exp_text = LoadOutput(Path.Combine(folder, expected));
                var got_text = LoadOutput(Path.Combine(folder, output));
                DictCompare(exp_text, got_text);
            }
        }

        internal void DictCompare(Dictionary<string,string> expected, Dictionary<string,string> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count, $"Lengths not equal.");
            foreach (var key in expected.Keys)
            {
                Assert.IsTrue(actual.ContainsKey(key), $"Key not found in actual: {key}");
                var evalue = expected[key];
                var avalue = actual[key];
                Assert.AreEqual(evalue, avalue, $"Value Mismatch: '{evalue}' != '{avalue}'");
            }
        }

        internal Dictionary<string, string> LoadOutput(string filename)
        {
            var contents = File.ReadAllText(filename);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(contents);
        }

        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "DDT-verify.csv", "DDT-verify#csv", DataAccessMethod.Sequential)]
        [TestMethod]
        public void TestVerify()
        {
            //using (var verify = MyCSVWriter._initCSV("DDT-verify.csv", "name", "verb", "input_file", "checksum", "fail", "verbose"))
            var folder = Column(0);
            var verb = Column(1);
            var input_file = Column(2);
            var checksum = Column(3);
            var fail = Boolean.Parse(Column(4));
            var verbose = Boolean.Parse(Column(5));
            bool have_checksum = !String.IsNullOrWhiteSpace(checksum);

            var cmdline = $"verify {input_file}";
            if (have_checksum)
                cmdline += $" -c {checksum}";
            if (verbose)
                cmdline += $" -v";

            var proc = FVTestUtil.RunProcess(cmdline, folder);

            // Check against explicit numbers because it is important to ensure that 
            // these are correct when batch scripts use the exe in production. 
            if (fail)
                Assert.AreEqual(1, proc.ExitCode, folder);
            else
                Assert.AreEqual(0, proc.ExitCode, folder);
        }
    }

    /// <summary>
    /// Handles bootstrapping the DDT input files needed for FVconsole unittests.
    /// Contains no test cases: the TestClass attribute is required for the framework
    /// to detect and call the AssemblyInitialize method properly. 
    /// </summary>
    public class FVTestBootstrap
    {
        public static void Initialize(TestContext ctx)
        {
            Directory.SetCurrentDirectory(GlobalData.DataDirFull);
            FVTestUtil.InitConsoleHookDefault();

            var create_cases = Directory.GetDirectories(@".\case\create", @"test_*");
            var verify_cases = Directory.GetDirectories(@".\case\verify", @"test_*");
            var calc_cases = Directory.GetDirectories(@".\case\calc", @"test_*");

            _loadCreateCases(create_cases);
            _loadVerifyCases(verify_cases);
            _loadCalcCases(calc_cases);
        }

        private static void _loadCalcCases(string[] calc_cases)
        {
            using (var calc = MyCSVWriter._initCSV("DDT-calc.csv", "name", "verb", "input_file", "checksum", "ftp", "verbose"))
                foreach (var test_case in calc_cases)
                    _buildCalcTest(calc, test_case, Path.GetFileName(test_case));
        }

        private static void _loadCreateCases(string[] create_cases)
        {
            using (var create = MyCSVWriter._initCSV("DDT-create.csv", "name", "verb", "batchmode", "input_file", "target_file", "expected", "append", "verbose"))
                foreach (var test_case in create_cases)
                    _buildCreateTest(create, test_case, Path.GetFileName(test_case));
        }

        private static void _loadVerifyCases(string[] verify_cases)
        {
            using (var verify = MyCSVWriter._initCSV("DDT-verify.csv", "name", "verb", "input_file", "checksum", "fail", "verbose"))
                foreach (var test_case in verify_cases)
                    _buildVerifyTest(verify, test_case, Path.GetFileName(test_case));
        }

        private static void _TestAdded(string verb, string name)
        {
            Console.WriteLine($"Added test case: {verb}.{name}");
        }

        private static void _buildVerifyTest(MyCSVWriter csv, string folder, string name)
        {
            var inf = "in.json";
            var csf = "checksum.txt";
            var ff = "fail.txt";
            if (!_Require(folder, inf))
                return;

            string checksum ="";
            if (File.Exists(Path.Combine(folder, csf)))
            {
                checksum = _ReadChecksum(folder,csf);
                if (checksum == null) return;  // handled, msg printed
            }
            var fail = File.Exists(Path.Combine(folder, ff)) ? Boolean.TrueString : Boolean.FalseString;

            // one each for verbose true/false
            // strings: name, verb, in, checksum, fail? verbose?
            csv.AddRow($"{folder}", "verify", inf, checksum, fail, Boolean.FalseString);
            csv.AddRow($"{folder}", "verify", inf, checksum, fail, Boolean.TrueString);
        }

        private static void _buildCreateTest(MyCSVWriter csv, string folder, string name)
        {
            if (name.StartsWith("test_batch"))
            {
                _buildCreateBatchTest(csv, folder, name);
            }
            else if (name.StartsWith("test_single"))
            {
                _buildCreateSingleTest(csv, folder, name);
            }
            else
            {
                Console.WriteLine("Warning: Create test must be prefixed with \"test_batch\" or \"test_single\"");
            }
        }

        private static void _buildCreateSingleTest(MyCSVWriter csv, string folder, string name)
        {
            var inf  = "in.json";
            var expf = "expect.json";
            var outf = "out.json";
            //var tgtf = "target.txt";
            if (!_Require(folder, inf, expf))
                return;

            var append = File.Exists(Path.Combine(folder, outf)) ? Boolean.TrueString : Boolean.FalseString;

            // name, verb, batchmode?, in, target, out, expected, append, verbose
            csv.AddRow($"{folder}", "create", "false", inf, "", expf, outf, append, Boolean.FalseString);
            csv.AddRow($"{folder}", "create", "false", inf, "", expf, outf, append, Boolean.TrueString);
        }

        private static void _buildCreateBatchTest(MyCSVWriter csv, string folder, string name)
        {
            var inf  = "in.json";
            var expf = "expect.json";
            var outf = "out.json";
            if (!_Require(folder, inf, expf))
                return;

            var append = File.Exists(Path.Combine(folder, outf)) ? Boolean.TrueString : Boolean.FalseString;

            // name, verb, batchmode?, in, target, out, expected, append, verbose
            csv.AddRow($"{folder}", "create", "true", inf, "", expf, outf, append, Boolean.FalseString);
            csv.AddRow($"{folder}", "create", "true", inf, "", expf, outf, append, Boolean.TrueString);
        }

        private static void _buildCalcTest(MyCSVWriter csv, string folder, string name)
        {
            var inf = "in.txt";
            var expf = "expect.txt";
            if (!_Require(folder, inf, expf))
                return;

            var checksum = _ReadChecksum(folder, expf);
            if (checksum == null) return;

            string ftp = "";
            var ftpf = Path.Combine(folder, "ftp.txt");
            if (File.Exists(ftpf))
                ftp = _ReadOneLine(ftpf);

            // one each for verbose true/false
            // strings: name, verb, in, expected, ftp?, verbose?
            csv.AddRow($"{folder}", "calc", inf, checksum, ftp, Boolean.FalseString);
            csv.AddRow($"{folder}", "calc", inf, checksum, ftp, Boolean.TrueString);
        }

        private static string _ReadChecksum(string folder, string filename)
        {
            try
            {
                return _ReadOneLine(Path.Combine(folder, filename));
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine($"Failed to extract checksum for calc test: {Path.GetDirectoryName(filename)}");
            }
            return null;
        }

        private static string _ReadOneLine(string file)
        {
            return File.ReadAllLines(file)[0].Trim();
        }

        private static bool _Require(string folder, params string[] files)
        {
            foreach (var f in files)
            {
                if (!File.Exists(Path.Combine(folder,f)))
                {
                    Console.WriteLine($"Warning: Unable to extract file from test case: '{folder}\\{f}'");
                    return false;
                }
            }
            return true;
        }



    }
}
