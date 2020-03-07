from pyftpdlib.authorizers import DummyAuthorizer
from pyftpdlib.handlers import FTPHandler
from pyftpdlib.servers import FTPServer

import threading
class ServerThread(threading.Thread):
    
    @classmethod
    def RunSingleTonOrWHatever(cls):
        authorizer = DummyAuthorizer()
        authorizer.add_user("fvconsole_user", "uRttUR3MRsF4z6UUngZ3", ".")

        handler = FTPHandler
        handler.authorizer = authorizer
        if hasattr(cls, "server"):
            cls.server.close_all()
        cls.server = FTPServer(("127.0.0.1", 21), handler)
        cls.server.serve_forever()
    def run(self):
        self.RunSingleTonOrWHatever()

    def stop(self):
        self.__class__.server.close_all()
        del self.__class__.server
        self.join()
    def __del__(self, *args):
        try:
            self.stop()
        except:
            pass

if __name__ == "__main__":
    server = ServerThread()
    server.start()
    while True:
        try:
            server.join(0.5)
        except BaseException:
            break
    server.stop()