namespace Fiddler
{
    using System;
    using System.Net;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Threading;

    public abstract class BasePipe
    {
        protected Socket _baseSocket;
        protected SslStream _httpsStream;
        private int _iTransmitDelayMS;
        protected internal string _sHackSessionList;
        protected internal string _sPipeName;
        protected internal uint iUseCount;

        public BasePipe(Socket oSocket, string sName)
        {
            this._sPipeName = sName;
            this._baseSocket = oSocket;
        }

        public void End()
        {
            try
            {
                if (this._httpsStream != null)
                {
                    this._httpsStream.Close();
                }
                if (this._baseSocket != null)
                {
                    this._baseSocket.Shutdown(SocketShutdown.Both);
                    this._baseSocket.Close();
                }
            }
            catch (Exception)
            {
            }
            this._baseSocket = null;
            this._httpsStream = null;
        }

        public Socket GetRawSocket()
        {
            return this._baseSocket;
        }

        internal void IncrementUse(int iSession)
        {
            this._iTransmitDelayMS = 0;
            this.iUseCount++;
            this._sHackSessionList = this._sHackSessionList + iSession.ToString() + ",";
        }

        internal int Receive(byte[] arrBuffer)
        {
            if (this.bIsSecured)
            {
                return this._httpsStream.Read(arrBuffer, 0, arrBuffer.Length);
            }
            return this._baseSocket.Receive(arrBuffer);
        }

        public void Send(byte[] oBytes)
        {
            this.Send(oBytes, 0, oBytes.Length);
        }

        internal void Send(byte[] oBytes, int iOffset, int iCount)
        {
            if (oBytes != null)
            {
                if ((iOffset + iCount) > oBytes.LongLength)
                {
                    iCount = oBytes.Length - iOffset;
                }
                if (iCount >= 1)
                {
                    if (this._iTransmitDelayMS < 1)
                    {
                        if (this.bIsSecured)
                        {
                            this._httpsStream.Write(oBytes, iOffset, iCount);
                        }
                        else
                        {
                            this._baseSocket.Send(oBytes, iOffset, iCount, SocketFlags.None);
                        }
                    }
                    else
                    {
                        int count = 0x400;
                        for (int i = iOffset; i < (iOffset + iCount); i += count)
                        {
                            if ((i + count) > (iOffset + iCount))
                            {
                                count = (iOffset + iCount) - i;
                            }
                            Thread.Sleep((int) (this._iTransmitDelayMS / 2));
                            if (this.bIsSecured)
                            {
                                this._httpsStream.Write(oBytes, i, count);
                            }
                            else
                            {
                                this._baseSocket.Send(oBytes, i, count, SocketFlags.None);
                            }
                            Thread.Sleep((int) (this._iTransmitDelayMS / 2));
                        }
                    }
                }
            }
        }

        public IPAddress Address
        {
            get
            {
                if ((this._baseSocket != null) && (this._baseSocket.RemoteEndPoint != null))
                {
                    return (this._baseSocket.RemoteEndPoint as IPEndPoint).Address;
                }
                return new IPAddress(0L);
            }
        }

        public bool bIsSecured
        {
            get
            {
                return (null != this._httpsStream);
            }
        }

        public bool Connected
        {
            get
            {
                if (this._baseSocket == null)
                {
                    return false;
                }
                return this._baseSocket.Connected;
            }
        }

        public int LocalPort
        {
            get
            {
                if ((this._baseSocket != null) && (this._baseSocket.LocalEndPoint != null))
                {
                    return (this._baseSocket.LocalEndPoint as IPEndPoint).Port;
                }
                return 0;
            }
        }

        public int Port
        {
            get
            {
                if ((this._baseSocket != null) && (this._baseSocket.RemoteEndPoint != null))
                {
                    return (this._baseSocket.RemoteEndPoint as IPEndPoint).Port;
                }
                return 0;
            }
        }

        public int TransmitDelay
        {
            get
            {
                return this._iTransmitDelayMS;
            }
            set
            {
                this._iTransmitDelayMS = value;
            }
        }
    }
}

