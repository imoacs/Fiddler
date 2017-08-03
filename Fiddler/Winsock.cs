namespace Fiddler
{
    using System;
    using System.Runtime.InteropServices;

    internal class Winsock
    {
        private const int AF_INET = 2;
        private const int AF_INET6 = 0x17;
        private const int ERROR_INSUFFICIENT_BUFFER = 0x7a;
        private const int NO_ERROR = 0;

        private static int FindPIDForConnection(int iTargetPort, uint iAddressType)
        {
            IntPtr zero = IntPtr.Zero;
            uint dwTcpTableLength = 0;
            int num2 = 12;
            int ofs = 12;
            int num4 = 0x18;
            if (iAddressType == 0x17)
            {
                num2 = 0x18;
                ofs = 0x20;
                num4 = 0x38;
            }
            if (0x7a == GetExtendedTcpTable(zero, ref dwTcpTableLength, false, iAddressType, TcpTableType.OwnerPidConnections, 0))
            {
                try
                {
                    zero = Marshal.AllocHGlobal((int) dwTcpTableLength);
                    if (GetExtendedTcpTable(zero, ref dwTcpTableLength, false, iAddressType, TcpTableType.OwnerPidConnections, 0) == 0)
                    {
                        int num5 = ((iTargetPort & 0xff) << 8) + ((iTargetPort & 0xff00) >> 8);
                        int num6 = Marshal.ReadInt32(zero);
                        if (num6 == 0)
                        {
                            return 0;
                        }
                        IntPtr ptr = (IntPtr) (((long) zero) + num2);
                        for (int i = 0; i < num6; i++)
                        {
                            if (num5 == Marshal.ReadInt32(ptr))
                            {
                                return Marshal.ReadInt32(ptr, ofs);
                            }
                            ptr = (IntPtr) (((long) ptr) + num4);
                        }
                        goto Label_0125;
                    }
                    FiddlerApplication.Log.LogFormat("GetExtendedTcpTable() returned error #{0}", new object[] { Marshal.GetLastWin32Error().ToString() });
                    return 0;
                }
                finally
                {
                    Marshal.FreeHGlobal(zero);
                }
            }
            FiddlerApplication.Log.LogFormat("Initial call to GetExtendedTcpTable() returned error #{0}", new object[] { Marshal.GetLastWin32Error().ToString() });
        Label_0125:
            return 0;
        }

        private static int FindPIDForPort(int iTargetPort)
        {
            int num = 0;
            try
            {
                num = FindPIDForConnection(iTargetPort, 2);
                if ((num > 0) || !CONFIG.bEnableIPv6)
                {
                    return num;
                }
                return FindPIDForConnection(iTargetPort, 0x17);
            }
            catch (Exception exception)
            {
                FiddlerApplication.Log.LogFormat("Fiddler.Network.TCPTable> Unable to call IPHelperAPI function: {0}", new object[] { exception.Message });
            }
            return 0;
        }

        [DllImport("iphlpapi.dll", SetLastError=true, ExactSpelling=true)]
        private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref uint dwTcpTableLength, [MarshalAs(UnmanagedType.Bool)] bool sort, uint ipVersion, TcpTableType tcpTableType, uint reserved);
        internal static int MapLocalPortToProcessId(int iPort)
        {
            return FindPIDForPort(iPort);
        }

        private enum TcpTableType
        {
            BasicListener,
            BasicConnections,
            BasicAll,
            OwnerPidListener,
            OwnerPidConnections,
            OwnerPidAll,
            OwnerModuleListener,
            OwnerModuleConnections,
            OwnerModuleAll
        }
    }
}

