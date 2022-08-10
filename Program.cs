using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Ping_V2
{
    public class Program
    {
        static void Main(string[] args)
        {
            string remoteHost = "discovery.ru";
            Socket host = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            IPHostEntry ip = Dns.Resolve(remoteHost);
            IPEndPoint ipEndPoint = new IPEndPoint(ip.AddressList[0], 0);
            EndPoint ep = (EndPoint)ipEndPoint;

            host.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 3000);

            int count = 4;
            int complete = 0;
            Console.WriteLine($"Обмен пакетами с {remoteHost} [{ip.AddressList[0]}]");
            for (byte i = 0; i < count; i++)
            {
                ICMP packet = CreateIcmpPacket(i);

                DateTime timeStart = DateTime.Now;
                host.SendTo(packet.GetBytes(), packet.messageSize + 4, SocketFlags.None, ipEndPoint);
                try
                {
                    byte[] data = new byte[1024];
                    int recv = host.ReceiveFrom(data, ref ep);
                    TimeSpan timeStop = DateTime.Now - timeStart;
                    ICMP response = new ICMP(data, recv);
                    complete++;
                    if (Char.GetNumericValue((char)(response.id)) == i)
                        Console.WriteLine($"Ответ от {remoteHost} число байт {recv} время {timeStop.Milliseconds}мс");
                    else
                        Console.WriteLine($"Получен чужой пакет.");
                }
                catch (SocketException se)
                {
                    Console.WriteLine("Превышен интервал ожидания для запроса");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message}");
                }
            }
            host.Close();
            Console.WriteLine($"\nСтатистика Ping для {ip.AddressList[0]}:\n\tПакетов: отправлено: {count}, получено = {complete}, потеряно = {count - complete}\n" +
                $"({(count - complete) * 100 / count}% потерь)");
            Console.ReadLine();
        }

        private static ICMP CreateIcmpPacket(byte id)
        {
            byte[] data = new byte[1024];
            ICMP packet = new ICMP();

            packet.id = id;
            packet.type = 0x08;
            packet.code = 0x00;
            packet.checkSum = 0;
            Buffer.BlockCopy(BitConverter.GetBytes(1), 0, packet.message, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(1), 0, packet.message, 2, 2);
            data = Encoding.ASCII.GetBytes($"test packet with id = {id}");
            Buffer.BlockCopy(data, 0, packet.message, 4, data.Length);

            packet.messageSize = data.Length + 4;
            int packetsize = packet.messageSize + 4;

            UInt16 checksum = packet.GetCheckSum();
            packet.checkSum = checksum;
            return packet;
        }
    }
}
