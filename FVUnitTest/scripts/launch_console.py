import subprocess, socket, pickle, os

def _cmd2(c, shell=False, **kw):
    rv = subprocess.Popen(c, shell=shell, **kw)
    return rv

def _cmd3(c, cwd, **kw):
    """ starts an independent process """
    return _cmd2(c, shell=False, stdin=None, cwd=cwd, stderr=None, stdout=None, creationflags=subprocess.CREATE_NEW_PROCESS_GROUP|subprocess.CREATE_NEW_CONSOLE, **kw)

if __name__ == "__main__":
    import sys
    here = os.path.dirname(__file__) or "."
    if len(sys.argv) > 1 and sys.argv[1] == "--use":
            cmd = "python remote_console.py --use %s"%sys.argv[2]
            _cmd3(cmd, here)
            print(sys.argv[2])
    else:
        with socket.socket() as sock:
            sock.bind(("localhost",0))
            cmd = "python remote_console.py %s %d"%sock.getsockname()
            proc = _cmd3(cmd, here)
            # print("Launcher: %s %d"%sock.getsockname())
            sock.listen(1)
            sock.settimeout(3)
            try:
                c,a=sock.accept()
            except:
                msg = proc.stderr.read()
                here = os.path.dirname(__file__)
                errfile = os.path.join(here, "error.log")
                with open(errfile, 'w') as f:
                    print("Internal error launching remote console", file=f)
                    print(msg.decode(), file=f)
                    print("<error>:<error>")
            else:
                with c.makefile("rb", buffering=False) as r:
                    host, port = pickle.load(r)
                print("%s:%d"%(host, port), flush=True)
