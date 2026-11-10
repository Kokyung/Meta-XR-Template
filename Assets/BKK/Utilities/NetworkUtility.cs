using System.Net;
using System.Net.Sockets;

public static class NetworkUtility
{
    public static string GetLocalIp()
    {
        // UDP Connect는 패킷을 보내지 않고 라우팅만 결정하므로,
        // 기본 경로의 어댑터 IP를 얻어 Hyper-V/WSL 같은 가상 어댑터를 피할 수 있다.
        try
        {
            using (var socket =
                new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect("8.8.8.8", 65530);

                return ((IPEndPoint)socket.LocalEndPoint).Address.ToString();
            }
        }
        catch (SocketException)
        {
            // 기본 경로가 없는 네트워크(인터넷 없는 로컬 공유기 등)에서는 첫 번째 IPv4 주소를 사용한다.
        }

        var host = Dns.GetHostEntry(Dns.GetHostName());

        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new System.Exception("IPv4 주소를 가진 네트워크 어댑터가 없습니다.");
    }

    public static bool UdpPortAlreadyUse(int port)
    {
        // GetIPGlobalProperties는 Android(Quest)에서 /proc/net 접근 제한으로 동작하지 않을 수 있으므로
        // 직접 바인딩을 시도해 사용 여부를 판단한다.
        try
        {
            using (new UdpClient(port))
            {
                return false;
            }
        }
        catch (SocketException)
        {
            return true;
        }
    }
}
