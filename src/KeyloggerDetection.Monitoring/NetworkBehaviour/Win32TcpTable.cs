using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace KeyloggerDetection.Monitoring.NetworkBehaviour;

/// <summary>
/// Managed wrapper for Win32 GetExtendedTcpTable providing full TCP 
/// telemetry attributable directly to PIDs. 
/// Extremely lightweight native call bypassing heavy WMI or netstat scraping.
/// </summary>
public static class Win32TcpTable
{
    private const int AF_INET = 2; // IPv4
    private const int AF_INET6 = 23; // IPv6

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref int dwOutBufLen,
        bool sort,
        int ipVersion,
        TcpTableClass tblClass,
        uint reserved = 0);

    private enum TcpTableClass
    {
        TCP_TABLE_OWNER_PID_ALL = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_TCPROW_OWNER_PID
    {
        public uint state;
        public uint localAddr;
        public byte localPort1;
        public byte localPort2;
        public byte localPort3;
        public byte localPort4;
        public uint remoteAddr;
        public byte remotePort1;
        public byte remotePort2;
        public byte remotePort3;
        public byte remotePort4;
        public int owningPid;

        public ushort LocalPort => BitConverter.ToUInt16(new[] { localPort2, localPort1 }, 0);
        public ushort RemotePort => BitConverter.ToUInt16(new[] { remotePort2, remotePort1 }, 0);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_TCPTABLE_OWNER_PID
    {
        public uint dwNumEntries;
        // Followed by MIB_TCPROW_OWNER_PID entries
    }

    public record TcpConnectionRecord(
        int Pid, 
        IPAddress LocalAddress, 
        ushort LocalPort, 
        IPAddress RemoteAddress, 
        ushort RemotePort, 
        TcpState State);

    public static List<TcpConnectionRecord> GetAllTcpConnections()
    {
        var connections = new List<TcpConnectionRecord>();
        int bufferSize = 0;
        
        // Query size
        uint ret = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, AF_INET, TcpTableClass.TCP_TABLE_OWNER_PID_ALL);
        if (ret != 0 && ret != 122) // 122 = ERROR_INSUFFICIENT_BUFFER
            return connections;

        IntPtr tcpTablePtr = Marshal.AllocHGlobal(bufferSize);
        try
        {
            ret = GetExtendedTcpTable(tcpTablePtr, ref bufferSize, true, AF_INET, TcpTableClass.TCP_TABLE_OWNER_PID_ALL);
            if (ret != 0) return connections;

            var table = Marshal.PtrToStructure<MIB_TCPTABLE_OWNER_PID>(tcpTablePtr);
            
            // Pointer arithmetic to read all rows dynamically
            IntPtr rowPtr = tcpTablePtr + Marshal.SizeOf(table.dwNumEntries);
            for (int i = 0; i < table.dwNumEntries; i++)
            {
                var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPtr);
                
                connections.Add(new TcpConnectionRecord(
                    row.owningPid,
                    new IPAddress(row.localAddr),
                     BitConverter.ToUInt16(new[] { row.localPort2, row.localPort1 }, 0),
                    new IPAddress(row.remoteAddr),
                     BitConverter.ToUInt16(new[] { row.remotePort2, row.remotePort1 }, 0),
                    (TcpState)row.state
                ));

                rowPtr += Marshal.SizeOf(typeof(MIB_TCPROW_OWNER_PID));
            }
        }
        finally
        {
            Marshal.FreeHGlobal(tcpTablePtr);
        }

        return connections;
    }
}
