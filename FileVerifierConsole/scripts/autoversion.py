import re, sys, argparse

def readlines(file):
    with open(file, 'r') as f:
        return f.read().splitlines()

def parse_lines(lines):
    AssemblyVersion = None
    AssemblyFileVersion = None
    for i, line in enumerate(lines):
        if line.startswith("[assembly: AssemblyVersion("):
            if AssemblyVersion is not None: raise ValueError("Multiple AssemblyVersion attributes")
            AssemblyVersion = i
        elif line.startswith("[assembly: AssemblyFileVersion(")    :
            if AssemblyFileVersion is not None: raise ValueError("Multiple AssemblyFileVersion attributes")
            AssemblyFileVersion = i
    if AssemblyVersion is None or AssemblyFileVersion is None:
        raise ValueError("Failed to find both AssemblyVersion and AssemblyFileVersion")
    return AssemblyVersion, AssemblyFileVersion

def parse_version(line):
    # Not super robust if the input can be anything other than
    # the expected attributes. 
    version = re.search("(\\d+)\\.(\\d+)\\.(\\d+)\\.(\\d+)", line).groups()
    return list(map(int,version))  # str->int, also so it is mutable

def incr(av,afv, idx):
    av[idx] += 1
    afv[idx] += 1

def make_line(typ, version):
    vs = "%d.%d.%d.%d"%tuple(version)
    return '[assembly: %s("%s")]'%(typ,vs)

def writelines(file, lines):
    with open(file, 'w') as f:
        f.write("\n".join(lines))

def main(args):
    lines = readlines(args.filename)
    av_idx, afv_idx = parse_lines(lines)

    av = parse_version(lines[av_idx])
    afv = parse_version(lines[afv_idx])

    if args.major:
        incr(av,afv,0)
    if args.minor:
        incr(av,afv,1)
    if args.patch:
        incr(av,afv,2)
    incr(av, afv, 3)  # build, always

    lines[av_idx] = make_line("AssemblyVersion", av)
    lines[afv_idx] = make_line("AssemblyFileVersion", afv)
    writelines(args.filename, lines)


def parse_args(argv):
    parser = argparse.ArgumentParser()
    parser.add_argument("filename", help="filename containing AssemblyVersion and AssemblyFileVersion attributes")
    parser.add_argument("--major", help="increment major version number", action="store_true")
    parser.add_argument("--minor", help="increment minor version number", action="store_true")
    parser.add_argument("--patch", help="increment patch number", action="store_true")
    args = parser.parse_args(argv)
    return args


if __name__ == "__main__":
    parsed_args = parse_args(sys.argv[1:])
    main(parsed_args)