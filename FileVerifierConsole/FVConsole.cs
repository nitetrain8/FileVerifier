using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CommandLine;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Net;

[assembly: InternalsVisibleTo("FVUnitTest")]
namespace FileVerifierConsole
{
    /// <summary>
    /// Command line options for the Calc function.
    /// </summary>
    [Verb("calc", HelpText = "Calculate checksum and print the value to StdOut")]
    internal class CalcOptions
    {
        /// <summary>
        /// File to calculate checksum for. 
        /// </summary>
        [Value(0, Required=true, HelpText = "File to calculate checksum for.")]
        public string File { get; set; }

        /// <summary>
        /// Enable verbose messages. 
        /// </summary>
        [Option('v', "verbose", Required = false, Default = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }
    }

    [Verb("create", HelpText = "Create spec file from a JSON source.")]
    internal class CreateOptions
    {
        [Option('v', "verbose", Required = false, Default = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('i', "input_file", Required = true, HelpText = "In Single File Mode: Use input_file as the single file source to calculate checksum for. Requires -t to specify target. " + 
            "In Batch Mode: JSON source file that specifies source and target filenames. " +
            "File must be an array of {'source':<source>, 'target':<target>} objects.\n"
            )]
        public string InputFile { get; set; }

        [Option('t', "target_file", Required = false, HelpText = "In Single File Mode, specifies the target filename for the provided source file. Ignored in Batch Mode.")]
        public string TargetFile { get; set; }

        [Option('b', "batch_mode", Required = false, Default = false, HelpText = "Run in Batch Mode.")]
        public bool BatchMode { get; set; }

        [Option('o', "output_file", Required = true, HelpText = "Filename to save created specification.")]
        public string OutputFile { get; set; }

        [Option('a', "Append", Required = false, Default = false, HelpText = "Append to output_file instead of creating a new file.")]
        public bool Append { get; set; }
    }

    [Verb("verify", HelpText = "Verify file checksum(s) based on the provided options.")]
    internal class VerifyOptions
    {
        [Value(0, MetaName="input_file", HelpText = "JSON spec file to run, or single file to verify if -c is provided")]
        public string FileIn { get; set; }

        [Option('c', "checksum", Required = false, HelpText = "Checksum value of for single file. Run in single-file mode and compare checksum of input_file against reference.")]
        public string Checksum { get; set; }

        [Option('v', "verbose", Required = false, Default = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }
    }

    /// <summary>
    /// All error codes that can be returned by the command line
    /// program. Note that error codes are shared by all program 
    /// modes. 
    /// </summary>
    internal enum ProgramErrors : int
    {
        Success = 0,    // operation successful / files verified
        Fail = 1,       // checksum verification failed
        SourceNotFound,
        NoFileProvided, // unsure if this is even possible
        BadFileSpec,    // Error loading file spec
        ChecksumError,  // Error generating checksum
        FileNotFound,
        NoData,
        FileSaveError,
    }

    internal abstract class BaseProgram
    {
        /// <summary>
        /// Base class for program modes. Contains common functions,
        /// including message helpers and convenience functions for 
        /// triggering errors and returning the appropriate error code.
        /// </summary>
        abstract internal bool Verbose { get; }

        internal void MsgLine(string msg)
        {
            if (Verbose)
                Console.WriteLine(msg);
        }
        internal void ErrorMsg(string msg)
        {
            if (Verbose)
                Console.Error.WriteLine(msg);
        }
        internal void Msg(string msg)
        {
            if (Verbose)
                Console.Write(msg);
        }
        internal int SourceNotFound(string fn)
        {
            ErrorMsg($"Source file not found: {fn}");
            return (int)ProgramErrors.SourceNotFound;
        }
        internal int NoFileProvided(string operation)
        {
            ErrorMsg($"No file provided for {operation}");
            return (int)ProgramErrors.NoFileProvided;
        }
        internal int Success()
        {
            return (int)ProgramErrors.Success;
        }
        internal int Fail()
        {
            return (int)ProgramErrors.Fail;
        }
        internal int BadSpec(string filename, Exception e)
        {
            ErrorMsg($"Failed to load spec file: '{filename}'");
            ErrorMsg(e.ToString());
            return (int)ProgramErrors.BadFileSpec;
        }
        internal int ChecksumError(Exception e)
        {
            ErrorMsg("Error");
            ErrorMsg(e.ToString());
            return (int)ProgramErrors.ChecksumError;
        }
        internal int NoData(string operation)
        {
            ErrorMsg($"No data found while {operation}");
            return (int)ProgramErrors.NoData;
        }
        internal int FileSaveError(string filename, Exception e)
        {
            ErrorMsg($"Failed to save spec file: '{filename}'");
            ErrorMsg(e.ToString());
            return (int)ProgramErrors.FileSaveError;
        }
    }

    internal class FVVerifyProgram : BaseProgram
    {
        VerifyOptions opts;
        override internal bool Verbose { get { return opts.Verbose; } }
        FileVerifier.FVEngine engine;

        public FVVerifyProgram(VerifyOptions opts)
        {
            this.opts = opts;
            this.engine = new FileVerifier.FVEngine();
        }

        public int Run()
        {
            if (String.IsNullOrWhiteSpace(opts.FileIn))
            {
                return NoFileProvided("verification");
            }
            if (!FileVerifier.FVFileUtil.DoesFileExist(opts.FileIn))
            {
                return SourceNotFound(opts.FileIn);
            }

            if (opts.Checksum != null)
            {
                MsgLine("Running single file mode ...");
                return RunSingleFile(opts.FileIn, opts.Checksum);
            }

            return RunFileSpec();
        }

        private int RunFileSpec()
        {
            try
            {
                engine.Load(opts.FileIn);
            }
            catch (Exception e)
            {
                return BadSpec(opts.FileIn, e);
            }

            var items = engine.GetData();
            int result = 0;
            foreach (var item in items)
            {
                result += RunSingleFile(item.Key, item.Value);  // 0 on success, nonzero on fail or error
            }
            return result == 0 ? (int)ProgramErrors.Success : (int)ProgramErrors.Fail;
        }

        private int RunSingleFile(string file, string exp_checksum)
        {
            Msg($"'{file}' ... ");
            string rec_checksum;
            try
            {
                rec_checksum = engine.GetChecksum(file);
            }
            catch (Exception e)
            {
                return ChecksumError(e);
            }
            var match = rec_checksum == exp_checksum;

            if (match)
            {
                MsgLine($"Verified.");
                return Success();
            }
            else
            {
                MsgLine("Failed");
                MsgLine($"    Expected {exp_checksum}, Got: {rec_checksum}");
                return Fail();
            }
        }
    }

    internal class FVCreateSpec
    {
        public string source { get; set; }
        public string target { get; set; }
    }

    internal class FVCreateProgram : BaseProgram
    {
        CreateOptions opts;
        override internal bool Verbose { get { return opts.Verbose; } }
        private FileVerifier.FVEngine engine;

        public FVCreateProgram(CreateOptions opts)
        {
            this.opts = opts;
            this.engine = new FileVerifier.FVEngine();
        }

        internal List<FVCreateSpec> LoadInputSpec(string filename)
        {
            string contents;
            using (var stream = FileVerifier.FVFileUtil.GetContentStream(filename))
            using (var reader = new StreamReader(stream))
            {
                contents = reader.ReadToEnd();
            }
            return JsonConvert.DeserializeObject<List<FVCreateSpec>>(contents);
        }

        public int Run()
        {
            if (opts.Append)
            {
                if (!FileVerifier.FVFileUtil.DoesFileExist(opts.OutputFile))
                {
                    MsgLine("Warning: output file not found while running in append mode. New file will be created.");
                }
                else
                {
                    engine.Load(opts.OutputFile);
                }
            }

            if (opts.BatchMode)
            {
                return RunCreateBatchMode();
            }

            // single file mode
            if (opts.TargetFile == null)
            {
                return NoFileProvided("single file mode target");
            }

            int result = AddSingleFile(opts.InputFile, opts.TargetFile);
            if (result != (int)ProgramErrors.Success)
            {
                return result;
            }
            return Save(opts.OutputFile);
        }

        internal int AddSingleFile(string src, string target)
        {
            try
            {
                var cs = engine.AddFileAuto(src, target);
                MsgLine($"Adding spec for \"{src}\": \"{target}\"=\"{cs}\"");
            }
            catch (Exception e)
            {
                return ChecksumError(e);
            }
            return (int)ProgramErrors.Success;
        }

        private int RunCreateBatchMode()
        {
            List<FVCreateSpec> input_data;
            try
            {
                input_data = LoadInputSpec(opts.InputFile);
            }
            catch (Exception e)
            {
                return BadSpec(opts.InputFile, e);
            }
            foreach (var data in input_data)
            {
                int result = AddSingleFile(data.source, data.target);
                if (result != (int)ProgramErrors.Success)
                {
                    return result;
                }
            }
            return Save(opts.OutputFile);
        }
        internal int Save(string fn)
        {
            try
            {
                engine.Save(fn);
            }
            catch (Exception e)
            {
                return FileSaveError(fn, e);
            }
            return Success();
        }
    }

    class FVConsole
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<CreateOptions, VerifyOptions, CalcOptions>(args)
                .MapResult(
                    (CreateOptions opts) => RunCreateMode(opts),
                    (VerifyOptions opts) => RunVerifyMode(opts),
                    (CalcOptions opts) => RunCalcMode(opts),
                    errs => 1
                );
        }

        static int RunCreateMode(CreateOptions opts)
        {
            var program = new FVCreateProgram(opts);
            return program.Run();
        }

        static int RunVerifyMode(VerifyOptions opts)
        {
            var program = new FVVerifyProgram(opts);
            return program.Run();
        }

        static int RunCalcMode(CalcOptions opts)
        {
            var program = new FVCalcProgram(opts);
            return program.Run();
        }
    }
    
    internal class FVCalcProgram : BaseProgram
    {
        private CalcOptions opts;
        internal override bool Verbose
        {
            get
            {
                return opts.Verbose;
            }
        }

        public FVCalcProgram(CalcOptions opts)
        {
            this.opts = opts;
        }

        public int Run()
        {
            var engine = new FileVerifier.FVEngine();
            string checksum;
            try
            {
                checksum = engine.GetChecksum(opts.File);
            }
            catch (FileNotFoundException)
            {
                return SourceNotFound(opts.File);
            }
            catch (WebException)
            {
                return SourceNotFound(opts.File);
            }
            catch (Exception e)
            {
                MsgLine("Failed to calculate checksum");
                return ChecksumError(e);
            }
            Console.WriteLine(checksum);
            return Success();
        }
    }
}
