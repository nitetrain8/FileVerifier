import socket, select, time

class Server():
    def __init__(self, host, port):
        self.host = host
        self.port = port
        self.socket = None

    def onServerBind(*a,**kw):
        pass # hook

    def onPeerClose(self, peersock):
        pass # hook
        
    def run(self):
        with socket.socket() as server_socket:
            server_socket.settimeout(2)
            server_socket.bind((self.host, self.port))
            server_socket.listen(1)
            self.onServerBind(server_socket.getsockname())
            print("Listening on: %s:%d"%server_socket.getsockname(), flush=True)
            try:
                self._run(server_socket)
            except KeyboardInterrupt:
                pass
        
    def _run(self, server_socket):
        while True:
            r,w,x = select.select([server_socket], [], [], 0.2)
            if r:
                c,a = server_socket.accept()
                self._echo(c)
                
    def _echo(self, peersock):
        print("\n-------- Connection: %s:%d --------"%peersock.getpeername())
        nl = True
        run = True
        while run:
            msg = b""
            try:
                r,w,x = select.select([peersock], [], [], 1)
                if r:
                    msg = peersock.recv(4096)
                    if not msg:
                        run = False
            except (ConnectionResetError, ConnectionAbortedError):
                # peer closed
                if not nl:
                    print()
                run = False
            else:
                try:
                    string = msg.decode('utf-8')
                except UnicodeDecodeError:
                    print("Error decoding string: %r"%string,end="")
                else:
                    print(string,end="")
                    nl = string.endswith("\n");
        print("-------- Connection: Closed ------------------\n")

if __name__ == "__main__":
    import sys, pickle
    if sys.argv[1] == "--use":
        host, port = sys.argv[2].split(":")
        server = Server(host, int(port))
        server.run()

        # while msg not in "qQ":
        #     input("Process complete: enter 'q' or 'Q' to exit.")
    else:
        _lhost, _lport = sys.argv[1:]
        server = Server("localhost", 0)
        
        def onBind(addr):
            # any errors here will crash the script, on purpose. 
            with socket.socket() as s:
                s.settimeout(3)
                s.connect((_lhost, int(_lport)))
                with s.makefile("wb", buffering=False) as w:
                    pickle.dump(addr, w)
        server.onServerBind = onBind

        class KillServer(Exception): pass
        def peerClose(peer):
            raise KillServer()
        server.onPeerClose = peerClose

        try:
            server.run()
        except KillServer:
            pass
        