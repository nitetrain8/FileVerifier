# File Verifier (fvconsole.exe) 

This solution contains a universal file checksum utility useful for verifying the integrity and contents of files. It includes the ability to verify and calculate checksum for any file accessible via an ordinary filepath or via FTP, and includes the ability to prepare a pre-configured "specification" file for ease in preparing for use. 

## Usage


#### TL;DR:
	Arguments: {required}, [optional] 

	fvconsole verify {input}
	fvconsole verify {input} -c {checksum}
	fvconsole create -i {input} -o {output} [-a]
	fvconsole create -i {input} -t {target} -o {output} [-a]
	fvconsole calc {input}


File Verifier is a command-line utility designed to be used either by hand or incorporated in an automated processing pipeline. There are three main functions (also known as "verbs") supported: `calc`, `create`, and `verify`. Usage help for each function can be can be obtained by:

	fvconsole help {verb}

The core functionality for all three features works by calculating a hex digest of an SHA256 checksum of the raw file contents.

Note that in all modes, `-v` or `--verbose` can be specified for additional output, which may be useful for debugging. 

All filepaths used by File Verifier, whether on the command line or specified via JSON input files, can be relative, absolute, or an FTP url. Note that for FTP, authentication credentials must be included in the url if the file is not accessible via anonymous login:

	ftp://user:password@hostname/path/to/file

Uploading output to FTP hasn't been tested, but will probably work.

**Note:** In documentation for the three functions below, example checksums are abbreviated for brevity. In the real utility, the entire checksum is used.      

### Verify

Verify is the most important function (the whole point of the utility!). It is used to determine whether a file or set of files matches an expected checksum.

Exit Codes:

	0   => Success
	1   => Fail (checksum mismatch)
	>=2 => Error (file not found, etc)

Verify can run in one of two modes: 

##### Batch Mode:

	fvconsole verify {input}

Uses a JSON input file to specify the filename and expected checksum for each file. For example:

	{
		"my_file.txt": "861ba[...]fa4f6",
		"some_other_file.txt": "a1f00[...]d682e"
		...
	}

For each key-value pair specified by the input file, the file (key) is hashed and the checksum is compared against the expected checksum (value). 

##### Single-File Mode 

	fvconsole verify {input} -c {checksum}

In single-file mode, the checksum of the input file is compared against the expected checksum value. 


### Create

Create allows a user to easily create the JSON input file expected by Verify's batch mode. Like Verify, it can run in batch or single mode, and can append to an existing JSON file across multiple runs. 

##### Batch Mode
	
	fvconsole create -i {input} -o {output}
	
The input file must be a JSON file specifying a list of source and target files:

	[
		{
			"source": "my_file.txt",
			"target": "./some/other/folder/other_file.txt"
		},
		{
			...
		}
		...
	]

Each file specified by `source` is opened, the checksum is calculated, and then a key-value pair is added to the output file with the file specified by target and the calculated checksum. The output is a JSON file identical in structure to what is expected by the Verify function. For example, the above might produce the following: 

	{
		"./some/other/folder/other_file.txt": "a1f00[...]d682e",
		...
	}

The difference in file structures serves two purposes:

* Ensures that an error is generated if the wrong file is used by mistake
* Allows the same local file to be checked against multiple files later. 
 
##### Single File Mode

In single file mode, the source and target files are specified directly on the command line:

	fvconsole create -i {source} -t {target} -o {output}

This functions identically to running in batch mode with the following JSON input file:

	[	
		{ "source": <source>, "target": <target> }
	]  

Single file mode by itself is not very useful, because it just creates a new output file each time. Unless, of course, you run in Append mode...

##### Append

This is not a distinct mode, but rather specifies that instead of creating or overwriting the specified output file, append to the file instead. Existing contents will be preserved unless they are silently overwritten in the case of a conflicting key. 

For example running the following in sequence (assuming out.json does not already exist):

	fvconsole create -i in1.txt -t blue.txt -o out.json -a
	fvconsole create -i in1.txt -t red.txt -o out.json -a
	fvconsole create -i in1.txt -t green.txt -o out.json -a

will first create out.json with blue.txt, then add red.txt, and finally green.txt, resulting in the following: 

	{
		"blue.txt": "a1f00[...]d682e",
		"red.txt": "a1f00[...]d682e",
		"green.txt": "a1f00[...]d682e"
	}

### Calc

Calc is very simple. Input a file, write the checksum to console (stdout). Note that this output includes a newline character (uses `Console.WriteLine(...)`)

	fvconsole calc {input} 

## Developing

This project was developed and compiled using VS2017 Community Edition for .NET framework v4.5. Note the choice of .NET version was deliberate: this is approximately the latest version compatible with the PBS Bioreactors as of March 2020. This allows the utility to run directly on a bioreactor, which is very convenient for testing. 

The full solution should work as-is after downloading from version control and installing the required packages. 

The project structure was modeled after [a Microsoft ASP.NET architect's recommended structure](https://gist.github.com/davidfowl/ed7564297c61fe9ab814). Note that I screwed it up, so it doesn't actually match. Bummer. 

### Prerequisites


NuGet Packages (note: NuGet should automatically install these for you): 

* CSVHelper
* CommandLineParser
* ILMerge (used to merge assemblies into fvconsole.exe)
* Netwonsoft.Json

Other Microsoft-provided packages may be required, but should be included with the standard VS2017 installation. 

Python v3 or higher is required to run the autoversion.py pre-build step. This script automatically increments the build version whenever the executable is rebuilt, whether in release or debug mode. The build should be successful anyway, but the version won't be updated.

This solution is intended to work on Windows only.  

### Installing

Download the respository from version control and open in Visual Studio. If you can build the FileVeriferConsole project - you're good to go. If errors occur, check the following:

* All NuGet packages installed
* Python v3 or later installed
* Running on Windows
* Check the filepaths located in the `FileVerifierConsole/scripts/ilmerge.bat` file. If these don't match your filesystem, the ILMerge step won't be able to run. 

## Documentation

File Verifier has a very simple structure:

* FVEngine: The machinery responsible for reading files and calculating checksums, as well as loading and saving the JSON files used for batch Verify.
* FVConsole: Command line utility responsible for parsing arguments and executing functions. 
* FVTest: misc snippets and other user test code.
* FVUnitTest: unit tests. 

Documentation for code is done via in-code [XML Comments](https://en.wikipedia.org/wiki/C_Sharp_(programming_language)#XML_documentation_system). 

## Running the tests

See the (very long) readme in the FVUnitTest directory. 

## Deployment

The build script uses ILMerge.exe to merge all dependencies into a single executable `fvconsole.exe`. This will be located in the `./FileVerifierConsole/bin/Release/` folder.  

Other files in the folder are build artifacts and can be ignored. 

## Versioning

This utility uses [SemVer](http://semver.org/) for versioning, because this is a version number system that is useful. 

The version number format is Major.Minor.Patch.Build. Major, minor, and patch are identical to SemVer. Build is the build number, and is updated automatically as a pre-build step whenever a build is run, even if no changes have been made. 

## Authors

* **Nathan Starkweather**
