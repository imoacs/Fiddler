namespace Fiddler
{
    using System;
    using System.IO;
    using System.Text;

    internal class HTTPSServerHello
    {
        private int _HandshakeVersion;
        private int _iCipherSuite;
        private int _iCompression;
        private int _MajorVersion;
        private int _MessageLen;
        private int _MinorVersion;
        private byte[] _Random;
        private byte[] _SessionID;

        internal bool LoadFromStream(Stream oNS)
        {
            int num = oNS.ReadByte();
            switch (num)
            {
                case 0x16:
                {
                    this._HandshakeVersion = 3;
                    this._MajorVersion = oNS.ReadByte();
                    this._MinorVersion = oNS.ReadByte();
                    int num2 = oNS.ReadByte() << 8;
                    num2 += oNS.ReadByte();
                    oNS.ReadByte();
                    byte[] buffer = new byte[3];
                    oNS.Read(buffer, 0, buffer.Length);
                    this._MessageLen = ((buffer[0] << 0x10) + (buffer[1] << 8)) + buffer[2];
                    this._MajorVersion = oNS.ReadByte();
                    this._MinorVersion = oNS.ReadByte();
                    this._Random = new byte[0x20];
                    oNS.Read(this._Random, 0, 0x20);
                    int num3 = oNS.ReadByte();
                    this._SessionID = new byte[num3];
                    oNS.Read(this._SessionID, 0, this._SessionID.Length);
                    this._iCipherSuite = oNS.ReadByte() << 8;
                    this._iCipherSuite += oNS.ReadByte();
                    this._iCompression = oNS.ReadByte();
                    break;
                }
                case 0x15:
                {
                    byte[] buffer2 = new byte[7];
                    oNS.Read(buffer2, 0, 7);
                    FiddlerApplication.Log.LogFormat("Got an alert from the server!\n{0}", new object[] { Utilities.ByteArrayToHexView(buffer2, 8) });
                    return false;
                }
                default:
                    this._HandshakeVersion = 2;
                    oNS.ReadByte();
                    if (0x80 != (num & 0x80))
                    {
                        oNS.ReadByte();
                    }
                    if (oNS.ReadByte() != 4)
                    {
                        return false;
                    }
                    this._SessionID = new byte[1];
                    oNS.Read(this._SessionID, 0, 1);
                    oNS.ReadByte();
                    this._MinorVersion = oNS.ReadByte();
                    this._MajorVersion = oNS.ReadByte();
                    break;
            }
            return true;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(0x200);
            if (this._HandshakeVersion == 2)
            {
                builder.Append("The data sent represents an SSLv2-compatible ServerHello handshake. In v2, the ~client~ selects the active cipher after the ServerHello, when sending the Client-Master-Key message. Fiddler only parses the handshake.\n\n");
            }
            else
            {
                builder.Append("The data sent represents an SSLv3-compatible ServerHello handshake. For your convenience, the data is extracted below.\n\n");
            }
            builder.Append(string.Format("Major Version: {0}\n", this._MajorVersion));
            builder.Append(string.Format("Minor Version: {0}\n", this._MinorVersion));
            builder.Append(string.Format("SessionID: {0}\n", Utilities.ByteArrayToString(this._SessionID)));
            if (this._HandshakeVersion == 3)
            {
                builder.Append(string.Format("Random: {0}\n", Utilities.ByteArrayToString(this._Random)));
                builder.Append(string.Format("Cipher: 0x{0}\n", this._iCipherSuite.ToString("X2")));
            }
            return builder.ToString();
        }

        public string SessionID
        {
            get
            {
                return Utilities.ByteArrayToString(this._SessionID);
            }
        }
    }
}

