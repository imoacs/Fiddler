namespace Fiddler
{
    using System;

    public class SessionTimers
    {
        public DateTime ClientBeginRequest;
        public DateTime ClientBeginResponse;
        public DateTime ClientConnected;
        public DateTime ClientDoneRequest;
        public DateTime ClientDoneResponse;
        public int DNSTime;
        public DateTime FiddlerBeginRequest;
        public int GatewayDeterminationTime;
        public int HTTPSHandshakeTime;
        public DateTime ServerBeginResponse;
        public DateTime ServerConnected;
        public DateTime ServerDoneResponse;
        public DateTime ServerGotRequest;
        public int TCPConnectTime;

        public override string ToString()
        {
            return this.ToString(false);
        }

        public string ToString(bool bMultiLine)
        {
            if (bMultiLine)
            {
                return string.Format("ClientConnected:\t{0:HH:mm:ss.fff}\r\nClientBeginRequest:\t{1:HH:mm:ss.fff}\r\nClientDoneRequest:\t{2:HH:mm:ss.fff}\r\nGateway Determination:\t{3,0}ms\r\nDNS Lookup: \t\t{4,0}ms\r\nTCP/IP Connect:\t\t{5,0}ms\r\nHTTPS Handshake:\t{6,0}ms\r\nServerConnected:\t{7:HH:mm:ss.fff}\r\nFiddlerBeginRequest:\t{8:HH:mm:ss.fff}\r\nServerGotRequest:\t{9:HH:mm:ss.fff}\r\nServerBeginResponse:\t{10:HH:mm:ss.fff}\r\nServerDoneResponse:\t{11:HH:mm:ss.fff}\r\nClientBeginResponse:\t{12:HH:mm:ss.fff}\r\nClientDoneResponse:\t{13:HH:mm:ss.fff}\r\n\r\n{14}", new object[] { this.ClientConnected, this.ClientBeginRequest, this.ClientDoneRequest, this.GatewayDeterminationTime, this.DNSTime, this.TCPConnectTime, this.HTTPSHandshakeTime, this.ServerConnected, this.FiddlerBeginRequest, this.ServerGotRequest, this.ServerBeginResponse, this.ServerDoneResponse, this.ClientBeginResponse, this.ClientDoneResponse, (TimeSpan.Zero < (this.ClientDoneResponse - this.ClientBeginRequest)) ? string.Format("\tOverall Elapsed:\t{0:h\\:mm\\:ss\\.fff}\r\n", (TimeSpan) (this.ClientDoneResponse - this.ClientBeginRequest)) : string.Empty });
            }
            return string.Format("ClientConnected: {0:HH:mm:ss.fff}, ClientBeginRequest: {1:HH:mm:ss.fff}, ClientDoneRequest: {2:HH:mm:ss.fff}, Gateway Determination: {3,0}ms, DNS Lookup: {4,0}ms, TCP/IP Connect: {5,0}ms, HTTPS Handshake: {6,0}ms, ServerConnected: {7:HH:mm:ss.fff},FiddlerBeginRequest: {8:HH:mm:ss.fff}, ServerGotRequest: {9:HH:mm:ss.fff}, ServerBeginResponse: {10:HH:mm:ss.fff},ServerDoneResponse: {11:HH:mm:ss.fff}, ClientBeginResponse: {12:HH:mm:ss.fff}, ClientDoneResponse: {13:HH:mm:ss.fff}{14}", new object[] { this.ClientConnected, this.ClientBeginRequest, this.ClientDoneRequest, this.GatewayDeterminationTime, this.DNSTime, this.TCPConnectTime, this.HTTPSHandshakeTime, this.ServerConnected, this.FiddlerBeginRequest, this.ServerGotRequest, this.ServerBeginResponse, this.ServerDoneResponse, this.ClientBeginResponse, this.ClientDoneResponse, (TimeSpan.Zero < (this.ClientDoneResponse - this.ClientBeginRequest)) ? string.Format(@", Overall Elapsed: {0:h\:mm\:ss\.fff}", (TimeSpan) (this.ClientDoneResponse - this.ClientBeginRequest)) : string.Empty });
        }
    }
}

