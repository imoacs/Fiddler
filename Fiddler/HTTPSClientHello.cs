namespace Fiddler
{
    using System;
    using System.IO;
    using System.Text;

    internal class HTTPSClientHello
    {
        private uint[] _CipherSuites;
        private int _HandshakeVersion;
        private int _MajorVersion;
        private int _MessageLen;
        private int _MinorVersion;
        private byte[] _Random;
        private byte[] _SessionID;
        private static readonly string[] SSL3CipherSuites = new string[] { 
            "SSL_NULL_WITH_NULL_NULL", "SSL_RSA_WITH_NULL_MD5", "SSL_RSA_WITH_NULL_SHA", "SSL_RSA_EXPORT_WITH_RC4_40_MD5", "SSL_RSA_WITH_RC4_128_MD5", "SSL_RSA_WITH_RC4_128_SHA", "SSL_RSA_EXPORT_WITH_RC2_40_MD5", "SSL_RSA_WITH_IDEA_SHA", "SSL_RSA_EXPORT_WITH_DES40_SHA", "SSL_RSA_WITH_DES_SHA", "SSL_RSA_WITH_3DES_EDE_SHA", "SSL_DH_DSS_EXPORT_WITH_DES40_SHA", "SSL_DH_DSS_WITH_DES_SHA", "SSL_DH_DSS_WITH_3DES_EDE_SHA", "SSL_DH_RSA_EXPORT_WITH_DES40_SHA", "SSL_DH_RSA_WITH_DES_SHA", 
            "SSL_DH_RSA_WITH_3DES_EDE_SHA", "SSL_DHE_DSS_EXPORT_WITH_DES40_SHA", "SSL_DHE_DSS_WITH_DES_SHA", "SSL_DHE_DSS_WITH_3DES_EDE_SHA", "SSL_DHE_RSA_EXPORT_WITH_DES40_SHA", "SSL_DHE_RSA_WITH_DES_SHA", "SSL_DHE_RSA_WITH_3DES_EDE_SHA", "SSL_DH_anon_EXPORT_WITH_RC4_40_MD5", "SSL_DH_anon_WITH_RC4_128_MD5", "SSL_DH_anon_EXPORT_WITH_DES40_SHA", "SSL_DH_anon_WITH_DES_SHA", "SSL_DH_anon_WITH_3DES_EDE_SHA", "SSL_FORTEZZA_KEA_WITH_NULL_SHA", "SSL_FORTEZZA_KEA_WITH_FORTEZZA_SHA", "SSL_FORTEZZA_KEA_WITH_RC4_128_SHA"
         };

        private static string CipherSuitesToString(uint[] inArr)
        {
            if (inArr == null)
            {
                return "null";
            }
            if (inArr.Length == 0)
            {
                return "empty";
            }
            StringBuilder builder = new StringBuilder(inArr.Length * 20);
            for (int i = 0; i < inArr.Length; i++)
            {
                builder.Append("\t[" + inArr[i].ToString("X4") + "] ");
                if (inArr[i] < SSL3CipherSuites.Length)
                {
                    builder.Append(SSL3CipherSuites[inArr[i]] + "\n");
                    continue;
                }
                switch (inArr[i])
                {
                    case 30:
                    {
                        builder.Append("TLS_KRB5_WITH_DES_SHA\n");
                        continue;
                    }
                    case 0x1f:
                    {
                        builder.Append("TLS_KRB5_WITH_3DES_EDE_SHA\n");
                        continue;
                    }
                    case 0x20:
                    {
                        builder.Append("TLS_KRB5_WITH_RC4_128_SHA\n");
                        continue;
                    }
                    case 0x21:
                    {
                        builder.Append("TLS_KRB5_WITH_IDEA_SHA\n");
                        continue;
                    }
                    case 0x22:
                    {
                        builder.Append("TLS_KRB5_WITH_DES_MD5\n");
                        continue;
                    }
                    case 0x23:
                    {
                        builder.Append("TLS_KRB5_WITH_3DES_EDE_MD5\n");
                        continue;
                    }
                    case 0x24:
                    {
                        builder.Append("TLS_KRB5_WITH_RC4_128_MD5\n");
                        continue;
                    }
                    case 0x25:
                    {
                        builder.Append("TLS_RSA_EXPORT1024_WITH_RC4_56_SHA\n");
                        continue;
                    }
                    case 0x26:
                    {
                        builder.Append("TLS_KRB5_EXPORT_WITH_DES_40_SHA\n");
                        continue;
                    }
                    case 0x27:
                    {
                        builder.Append("TLS_KRB5_EXPORT_WITH_RC2_40_SHA\n");
                        continue;
                    }
                    case 40:
                    {
                        builder.Append("TLS_KRB5_EXPORT_WITH_RC4_40_SHA\n");
                        continue;
                    }
                    case 0x29:
                    {
                        builder.Append("TLS_KRB5_EXPORT_WITH_DES_40_MD5\n");
                        continue;
                    }
                    case 0x2a:
                    {
                        builder.Append("TLS_KRB5_EXPORT_WITH_RC2_40_MD5\n");
                        continue;
                    }
                    case 0x2b:
                    {
                        builder.Append("TLS_KRB5_EXPORT_WITH_RC4_40_MD5\n");
                        continue;
                    }
                    case 0x2f:
                    {
                        builder.Append("TLS_RSA_AES_128_SHA\n");
                        continue;
                    }
                    case 0x30:
                    {
                        builder.Append("TLS_DH_DSS_WITH_AES_128_SHA\n");
                        continue;
                    }
                    case 0x31:
                    {
                        builder.Append("TLS_DH_RSA_WITH_AES_128_SHA\n");
                        continue;
                    }
                    case 50:
                    {
                        builder.Append("TLS_DHE_DSS_WITH_AES_128_SHA\n");
                        continue;
                    }
                    case 0x33:
                    {
                        builder.Append("TLS_DHE_RSA_WITH_AES_128_SHA\n");
                        continue;
                    }
                    case 0x34:
                    {
                        builder.Append("TLS_DH_ANON_WITH_AES_128_SHA\n");
                        continue;
                    }
                    case 0x35:
                    {
                        builder.Append("TLS_RSA_AES_256_SHA\n");
                        continue;
                    }
                    case 0x36:
                    {
                        builder.Append("TLS_DH_DSS_WITH_AES_256_SHA\n");
                        continue;
                    }
                    case 0x37:
                    {
                        builder.Append("TLS_DH_RSA_WITH_AES_256_SHA\n");
                        continue;
                    }
                    case 0x38:
                    {
                        builder.Append("TLS_DHE_DSS_WITH_AES_256_SHA\n");
                        continue;
                    }
                    case 0x39:
                    {
                        builder.Append("TLS_DHE_RSA_WITH_AES_256_SHA\n");
                        continue;
                    }
                    case 0x3a:
                    {
                        builder.Append("TLS_DH_ANON_WITH_AES_256_SHA\n");
                        continue;
                    }
                    case 60:
                    {
                        builder.Append("TLS_RSA_WITH_AES_128_CBC_SHA256\n");
                        continue;
                    }
                    case 0x3d:
                    {
                        builder.Append("TLS_RSA_WITH_AES_256_CBC_SHA256\n");
                        continue;
                    }
                    case 0x40:
                    {
                        builder.Append("TLS_DHE_DSS_WITH_AES_128_CBC_SHA256\n");
                        continue;
                    }
                    case 0x41:
                    {
                        builder.Append("TLS_RSA_WITH_CAMELLIA_128_CBC_SHA\n");
                        continue;
                    }
                    case 0x44:
                    {
                        builder.Append("TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA\n");
                        continue;
                    }
                    case 0x45:
                    {
                        builder.Append("TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA\n");
                        continue;
                    }
                    case 0x47:
                    {
                        builder.Append("TLS_ECDH_ECDSA_WITH_NULL_SHA\n");
                        continue;
                    }
                    case 0x48:
                    {
                        builder.Append("TLS_ECDH_ECDSA_WITH_RC4_128_SHA\n");
                        continue;
                    }
                    case 0x49:
                    {
                        builder.Append("TLS_ECDH_ECDSA_WITH_DES_SHA\n");
                        continue;
                    }
                    case 0x4a:
                    {
                        builder.Append("TLS_ECDH_ECDSA_WITH_3DES_EDE_SHA\n");
                        continue;
                    }
                    case 0x4b:
                    {
                        builder.Append("TLS_ECDH_ECDSA_WITH_AES_128_SHA\n");
                        continue;
                    }
                    case 0x4c:
                    {
                        builder.Append("TLS_ECDH_ECDSA_WITH_AES_256_SHA\n");
                        continue;
                    }
                    case 0x4d:
                    {
                        builder.Append("TLS_ECDH_RSA_WITH_NULL_SHA\n");
                        continue;
                    }
                    case 0x4e:
                    {
                        builder.Append("TLS_ECDH_RSA_WITH_RC4_128_SHA\n");
                        continue;
                    }
                    case 0x4f:
                    {
                        builder.Append("TLS_ECDH_RSA_WITH_DES_SHA\n");
                        continue;
                    }
                    case 80:
                    {
                        builder.Append("TLS_ECDH_RSA_WITH_3DES_EDE_SHA\n");
                        continue;
                    }
                    case 0x51:
                    {
                        builder.Append("TLS_ECDH_RSA_WITH_AES_128_SHA\n");
                        continue;
                    }
                    case 0x52:
                    {
                        builder.Append("TLS_ECDH_RSA_WITH_AES_256_SHA\n");
                        continue;
                    }
                    case 0x62:
                    {
                        builder.Append("TLS_RSA_EXPORT1024_WITH_DES_SHA\n");
                        continue;
                    }
                    case 0x63:
                    {
                        builder.Append("TLS_DHE_DSS_EXPORT1024_WITH_DES_SHA\n");
                        continue;
                    }
                    case 100:
                    {
                        builder.Append("TLS_RSA_EXPORT1024_WITH_RC4_56_SHA\n");
                        continue;
                    }
                    case 0x65:
                    {
                        builder.Append("TLS_DHE_DSS_EXPORT1024_WITH_RC4_56_SHA\n");
                        continue;
                    }
                    case 0x66:
                    {
                        builder.Append("TLS_DHE_DSS_WITH_RC4_128_SHA\n");
                        continue;
                    }
                    case 0x6a:
                    {
                        builder.Append("TLS_DHE_DSS_WITH_AES_256_CBC_SHA256\n");
                        continue;
                    }
                    case 0x77:
                    {
                        builder.Append("TLS_ECDHE_ECDSA_WITH_AES_128_SHA\n");
                        continue;
                    }
                    case 120:
                    {
                        builder.Append("TLS_ECDHE_RSA_WITH_AES_128_SHA\n");
                        continue;
                    }
                    case 0x84:
                    {
                        builder.Append("TLS_RSA_WITH_CAMELLIA_256_CBC_SHA\n");
                        continue;
                    }
                    case 0x87:
                    {
                        builder.Append("TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA\n");
                        continue;
                    }
                    case 0x88:
                    {
                        builder.Append("TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA\n");
                        continue;
                    }
                    case 150:
                    {
                        builder.Append("TLS_RSA_WITH_SEED_CBC_SHA\n");
                        continue;
                    }
                    case 0xff:
                    {
                        builder.AppendLine("TLS_EMPTY_RENEGOTIATION_INFO_SCSV");
                        continue;
                    }
                    case 0xc002:
                    {
                        builder.Append("TLS_ECDH_ECDSA_WITH_RC4_128_SHA\n");
                        continue;
                    }
                    case 0xc003:
                    {
                        builder.Append("TLS_ECDH_ECDSA_WITH_3DES_EDE_CBC_SHA\n");
                        continue;
                    }
                    case 0xc004:
                    {
                        builder.Append("TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA\n");
                        continue;
                    }
                    case 0xc005:
                    {
                        builder.Append("TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA\n");
                        continue;
                    }
                    case 0xc007:
                    {
                        builder.Append("TLS_ECDHE_ECDSA_WITH_RC4_128_SHA\n");
                        continue;
                    }
                    case 0xc008:
                    {
                        builder.Append("TLS_ECDHE_ECDSA_WITH_3DES_EDE_CBC_SHA\n");
                        continue;
                    }
                    case 0xc009:
                    {
                        builder.Append("TLS1_CK_ECDHE_ECDSA_WITH_AES_128_CBC_SHA\n");
                        continue;
                    }
                    case 0xc00a:
                    {
                        builder.Append("TLS1_CK_ECDHE_ECDSA_WITH_AES_256_CBC_SHA\n");
                        continue;
                    }
                    case 0xc00c:
                    {
                        builder.Append("TLS_ECDH_RSA_WITH_RC4_128_SHA\n");
                        continue;
                    }
                    case 0xc00d:
                    {
                        builder.Append("TLS_ECDH_RSA_WITH_3DES_EDE_CBC_SHA\n");
                        continue;
                    }
                    case 0xc00e:
                    {
                        builder.Append("TLS_ECDH_RSA_WITH_AES_128_CBC_SHA\n");
                        continue;
                    }
                    case 0xc00f:
                    {
                        builder.Append("TLS_ECDH_RSA_WITH_AES_256_CBC_SHA\n");
                        continue;
                    }
                    case 0xc011:
                    {
                        builder.Append("TLS_ECDHE_RSA_WITH_RC4_128_SHA\n");
                        continue;
                    }
                    case 0xc012:
                    {
                        builder.Append("TLS_ECDHE_RSA_WITH_3DES_EDE_CBC_SHA\n");
                        continue;
                    }
                    case 0xc013:
                    {
                        builder.Append("TLS1_CK_ECDHE_RSA_WITH_AES_128_CBC_SHA\n");
                        continue;
                    }
                    case 0xc014:
                    {
                        builder.Append("TLS1_CK_ECDHE_RSA_WITH_AES_256_CBC_SHA\n");
                        continue;
                    }
                    case 0xc023:
                    {
                        builder.Append("TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256\n");
                        continue;
                    }
                    case 0xc024:
                    {
                        builder.Append("TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384\n");
                        continue;
                    }
                    case 0xc027:
                    {
                        builder.Append("TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256\n");
                        continue;
                    }
                    case 0xc02b:
                    {
                        builder.Append("TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256\n");
                        continue;
                    }
                    case 0xc02c:
                    {
                        builder.Append("TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384\n");
                        continue;
                    }
                    case 0xfefe:
                    {
                        builder.Append("SSL_RSA_FIPS_WITH_DES_SHA\n");
                        continue;
                    }
                    case 0xfeff:
                    {
                        builder.Append("SSL_RSA_FIPS_WITH_3DES_EDE_SHA\n");
                        continue;
                    }
                    case 0xff01:
                    {
                        builder.Append("SSL_EN_RC4_128_WITH_MD5\n");
                        continue;
                    }
                    case 0xff02:
                    {
                        builder.Append("SSL_EN_RC4_128_EXPORT40_WITH_MD5\n");
                        continue;
                    }
                    case 0xff03:
                    {
                        builder.Append("SSL_EN_RC2_128_WITH_MD5\n");
                        continue;
                    }
                    case 0xff04:
                    {
                        builder.Append("SSL_EN_RC2_128_EXPORT40_WITH_MD5\n");
                        continue;
                    }
                    case 0xff05:
                    {
                        builder.Append("SSL_EN_IDEA_128_WITH_MD5\n");
                        continue;
                    }
                    case 0xff06:
                    {
                        builder.Append("SSL_EN_DES_64_WITH_MD5\n");
                        continue;
                    }
                    case 0xff07:
                    {
                        builder.Append("SSL_EN_DES_192_EDE3_WITH_MD5\n");
                        continue;
                    }
                    case 0x20080:
                    {
                        builder.Append("SSL2_RC4_128_EXPORT40_WITH_MD5\n");
                        continue;
                    }
                    case 0x30080:
                    {
                        builder.Append("SSL2_RC2_128_WITH_MD5\n");
                        continue;
                    }
                    case 0xffe0:
                    {
                        builder.Append("SSL_RSA_NSFIPS_WITH_3DES_EDE_SHA\n");
                        continue;
                    }
                    case 0xffe1:
                    {
                        builder.Append("SSL_RSA_NSFIPS_WITH_DES_SHA\n");
                        continue;
                    }
                    case 0x10080:
                    {
                        builder.Append("SSL2_RC4_128_WITH_MD5\n");
                        continue;
                    }
                    case 0x40080:
                    {
                        builder.Append("SSL2_RC2_128_EXPORT40_WITH_MD5\n");
                        continue;
                    }
                    case 0x50080:
                    {
                        builder.Append("SSL2_IDEA_128_WITH_MD5\n");
                        continue;
                    }
                    case 0x60040:
                    {
                        builder.Append("SSL2_DES_64_WITH_MD5\n");
                        continue;
                    }
                    case 0x700c0:
                    {
                        builder.Append("SSL2_DES_192_EDE3_WITH_MD5\n");
                        continue;
                    }
                }
                builder.Append("Unrecognized cipher - See http://www.iana.org/assignments/tls-parameters/\n");
            }
            return builder.ToString();
        }

        internal bool LoadFromStream(Stream oNS)
        {
            switch (oNS.ReadByte())
            {
                case 0x80:
                {
                    this._HandshakeVersion = 2;
                    oNS.ReadByte();
                    oNS.ReadByte();
                    this._MajorVersion = oNS.ReadByte();
                    this._MinorVersion = oNS.ReadByte();
                    if ((this._MajorVersion == 0) && (this._MinorVersion == 2))
                    {
                        this._MajorVersion = 2;
                        this._MinorVersion = 0;
                    }
                    int num2 = oNS.ReadByte() << 8;
                    num2 += oNS.ReadByte();
                    int num3 = oNS.ReadByte() << 8;
                    num3 += oNS.ReadByte();
                    int num4 = oNS.ReadByte() << 8;
                    num4 += oNS.ReadByte();
                    this._CipherSuites = new uint[num2 / 3];
                    for (int i = 0; i < this._CipherSuites.Length; i++)
                    {
                        this._CipherSuites[i] = (uint) (((oNS.ReadByte() << 0x10) + (oNS.ReadByte() << 8)) + oNS.ReadByte());
                    }
                    this._SessionID = new byte[num3];
                    oNS.Read(this._SessionID, 0, this._SessionID.Length);
                    this._Random = new byte[num4];
                    oNS.Read(this._Random, 0, this._Random.Length);
                    break;
                }
                case 0x16:
                {
                    this._HandshakeVersion = 3;
                    this._MajorVersion = oNS.ReadByte();
                    this._MinorVersion = oNS.ReadByte();
                    int num6 = oNS.ReadByte() << 8;
                    num6 += oNS.ReadByte();
                    oNS.ReadByte();
                    byte[] buffer = new byte[3];
                    oNS.Read(buffer, 0, buffer.Length);
                    this._MessageLen = ((buffer[0] << 0x10) + (buffer[1] << 8)) + buffer[2];
                    this._MajorVersion = oNS.ReadByte();
                    this._MinorVersion = oNS.ReadByte();
                    this._Random = new byte[0x20];
                    oNS.Read(this._Random, 0, 0x20);
                    int num7 = oNS.ReadByte();
                    this._SessionID = new byte[num7];
                    oNS.Read(this._SessionID, 0, this._SessionID.Length);
                    buffer = new byte[2];
                    oNS.Read(buffer, 0, buffer.Length);
                    int num8 = (buffer[0] << 8) + buffer[1];
                    this._CipherSuites = new uint[num8 / 2];
                    buffer = new byte[num8];
                    oNS.Read(buffer, 0, buffer.Length);
                    for (int j = 0; j < this._CipherSuites.Length; j++)
                    {
                        this._CipherSuites[j] = (uint) ((buffer[2 * j] << 8) + buffer[(2 * j) + 1]);
                    }
                    break;
                }
            }
            return true;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(0x200);
            if (this._HandshakeVersion == 2)
            {
                builder.Append("The data sent represents an SSLv2-compatible ClientHello handshake. For your convenience, the data is extracted below.\n\n");
            }
            else
            {
                builder.Append("The data sent represents an SSLv3-compatible ClientHello handshake. For your convenience, the data is extracted below.\n\n");
            }
            builder.Append(string.Format("Major Version: {0}\n", this._MajorVersion));
            builder.Append(string.Format("Minor Version: {0}\n", this._MinorVersion));
            builder.Append(string.Format("Random: {0}\n", Utilities.ByteArrayToString(this._Random)));
            builder.Append(string.Format("SessionID: {0}\n", Utilities.ByteArrayToString(this._SessionID)));
            builder.Append(string.Format("Ciphers: \n{0}\n", CipherSuitesToString(this._CipherSuites)));
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

