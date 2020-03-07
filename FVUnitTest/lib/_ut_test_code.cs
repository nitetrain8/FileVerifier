using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FVUnitTest
{
    class _ut_test_code
    {
        //[TestClass]
        //public class FVUnitTest
        //{

        //    string TestDir;
        //    public FVUnitTest()
        //    {
        //        TestDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\tests\\files"));
        //    }

        //    private string GetFilePath(string name)
        //    {
        //        return Path.Combine(TestDir, name);
        //    }

        //    [TestMethod]
        //    public void TestFVCompare1()
        //    {
        //        var f1 = GetFilePath("SV_Air_IM226J.sys");
        //        var f2 = GetFilePath("SV_Mag_IM226J.sys");

        //        var checker = new FileVerifier.FileChecksumGetterSHA256();
        //        var checker2 = new FileVerifier.FileChecksumGetterSHA256();

        //        using (var source = new CancellationTokenSource())
        //        {
        //            var token = source.Token;
        //            string cs1, cs2, cs3;

        //            using (Stream s1 = File.OpenRead(f1),
        //                    s2 = File.OpenRead(f2))
        //            {
        //                cs1 = checker.Calculate(s1, token);
        //                cs2 = checker.Calculate(s2, token);

        //                s2.Seek(0, SeekOrigin.Begin);
        //                cs3 = checker2.Calculate(s2, token);
        //            }
        //            Assert.AreEqual(cs2, cs3);
        //        }
        //    }

        //    private FileVerifierConsole.CreateOptions _CreateCreateOptionsBatch(string fpin, string fpout)
        //    {
        //        var options = new FileVerifierConsole.CreateOptions
        //        {
        //            Append = false,
        //            BatchMode = true,
        //            InputFile = fpin,
        //            TargetFile = null,
        //            OutputFile = fpout
        //        };
        //        return options;
        //    }

        //    private void _PrepBatchInFilePathSpec(string raw, string prepared)
        //    {
        //        // allow spec file to be written with local filesnames for ease of testing.
        //        // This routine loads and converts them to the relative paths expecte by this program. 
        //        var json = File.ReadAllText(raw);
        //        var spec = JsonConvert.DeserializeObject<List<FileVerifierConsole.FVCreateSpec>>(json);
        //        var dir = Path.GetDirectoryName(raw);

        //        foreach (var item in spec)
        //        {
        //            item.source = Path.Combine(dir, item.source);
        //            item.target = Path.Combine(dir, item.target);
        //        }
        //        var contents = JsonConvert.SerializeObject(spec, Formatting.Indented);
        //        File.WriteAllText(prepared, contents);
        //    }

        //    private void _PrepBatchOutFilePathSpec(string raw, string prepared)
        //    {
        //        var json = File.ReadAllText(raw);
        //        var spec = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        //        var dir = Path.GetDirectoryName(raw);

        //        var outspec = new Dictionary<string, string>();
        //        foreach (var item in spec)
        //        {
        //            var key = Path.Combine(dir, item.Key);
        //            outspec[key] = item.Value;
        //        }
        //        var contents = JsonConvert.SerializeObject(outspec, Formatting.Indented);
        //        File.WriteAllText(prepared, contents);
        //    }

        //    private class CleanupFileHandler : IDisposable
        //    {
        //        string[] files;
        //        public CleanupFileHandler(params string[] args)
        //        {
        //            files = args;
        //        }

        //        public void Dispose()
        //        {
        //            //Cleanup();
        //        }

        //        public void Cleanup()
        //        {
        //            if (files == null)
        //                return;
        //            foreach (var file in files)
        //            {
        //                try
        //                {
        //                    File.Delete(file);
        //                } catch(FileNotFoundException)
        //                {
        //                    // pass
        //                }
        //            }
        //            files = null;
        //        }

        //    }

        //    [TestMethod]
        //    public void TestCreateBatch()
        //    {
        //        var raw_file = GetFilePath("Create\\Batch\\in1.json");
        //        var out_file = GetFilePath("Create\\Batch\\out1.json");
        //        var exp_file = GetFilePath("Create\\Batch\\exp1.json");
        //        DoTestBatchMode(raw_file, out_file, exp_file);
        //    }

        //    private void DoTestBatchMode(string raw_in_file, string out_file, string raw_exp_file)
        //    {
        //        var in_file = _GetPrepFileName(raw_in_file);
        //        var exp_file = _GetPrepFileName(raw_exp_file);
        //        _PrepBatchInFilePathSpec(raw_in_file, in_file);
        //        _PrepBatchOutFilePathSpec(raw_exp_file, exp_file);

        //        var cleaner = new CleanupFileHandler(out_file, in_file);

        //        var options = _CreateCreateOptionsBatch(in_file, out_file);
        //        var program = new FileVerifierConsole.FVCreateProgram(options);

        //        int result = program.Run();
        //        Assert.AreEqual(0, result);

        //        var exp = File.ReadAllText(exp_file);
        //        var res = File.ReadAllText(out_file);
        //        Assert.AreEqual(exp, res);

        //        cleaner.Cleanup(); // deliberately skip on Assert failure
        //    }

        //    private static string _GetPrepFileName(string raw_file)
        //    {
        //        var fn = Path.GetFileNameWithoutExtension(raw_file);
        //        var ext = Path.GetExtension(raw_file);
        //        var ind = Path.GetDirectoryName(raw_file);
        //        var file = $"{ind}\\{fn}_prep{ext}";
        //        return file;
        //    }
        //}

        //[TestClass]
        //public class TestTesting
        //{

        //    private TestContext _tc;
        //    public TestContext TestContext { get { return _tc; } set { _tc = value; } }

        //    //[AssemblyInitialize]
        //    //public static void AssemblyInitialize(TestContext testContext)
        //    //{
        //    //    var ddt = Path.GetFullPath(".\\..\\..\\data\\ddt.csv");
        //    //    Debug.WriteLine($"Making DDT file at '{ddt}'");
        //    //    void echo(StreamWriter w, string msg)
        //    //    {
        //    //        Debug.WriteLine(msg);
        //    //        w.WriteLine(msg);
        //    //    }

        //    //    string[] days =
        //    //    {
        //    //        "Monday", "Mon",
        //    //        "Tuesday", "Tues",
        //    //        "Wednesday", "Wed",
        //    //        "Thursday", "Thurs",
        //    //        "Friday", "Fri",
        //    //        "Saturday", "Sat",
        //    //        "Sunday", "Sun"
        //    //    };

        //    //    var rand = new Random();
        //    //    string rday() => days[rand.Next(0, days.Length)];

        //    //    using (var writer = new StreamWriter(ddt))
        //    //    {
        //    //        echo(writer, "var1,var2,var3");
        //    //        echo(writer, $"{rday()},{rday()},{rday()}");
        //    //        echo(writer, $"{rday()},{rday()},{rday()}");
        //    //        echo(writer, $"{rday()},{rday()},{rday()}");
        //    //        echo(writer, $"{rday()},{rday()},{rday()}");
        //    //    }
        //    //}

        //    [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", ".\\..\\..\\data\\DDT.csv", "DDT#csv", DataAccessMethod.Sequential)]
        //    [TestMethod]
        //    public void Test()
        //    {
        //        Console.WriteLine($"{_tc.DataRow[0]}, {_tc.DataRow[1]}, {_tc.DataRow[2]}");
        //        Assert.IsTrue(true);
        //    }

        //    new public static Type GetType()
        //    {
        //        return typeof(TestTesting);
        //    }

        //    [ClassInitialize]
        //    public static void ClassInitialize(TestContext testContext)
        //    {
        //        var ddt = Path.GetFullPath(".\\..\\..\\data\\ddt.csv");
        //        Debug.WriteLine($"Making DDT file at '{ddt}'");

        //        string[] days =
        //        {
        //                "Mon,day", "Mon",
        //                "Tues,day", "Tues",
        //                "Wed\"ne\"sday", "Wed",
        //                "Thu,rsd,ay", "Thurs",
        //                "Fri\"da\"y", "Fri",
        //                "Sa,tur,day", "Sat",
        //                "Su,nd,ay", "Sun"
        //            };

        //        var rand = new Random();
        //        string rday() => days[rand.Next(0, days.Length)];

        //        void echo2(CsvHelper.CsvWriter w, string a, string b, string c)
        //        {
        //            Debug.WriteLine($"{a},{b},{c}");
        //            w.WriteField(a);
        //            w.WriteField(b);
        //            w.WriteField(c);
        //        }

        //        var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.CurrentCulture);
        //        using (var writer = new StreamWriter(ddt))
        //        using (var csv = new CsvHelper.CsvWriter(writer, config))
        //        {
        //            // header
        //            echo2(csv, "var1", "var2", "var3");
        //            csv.NextRecord();

        //            int rows = 3;
        //            for (int i = 0; i < rows; ++i)
        //            {
        //                echo2(csv, rday(), rday(), rday());
        //                csv.NextRecord();
        //            }
        //        }
        //    }
        //}
    }
}
