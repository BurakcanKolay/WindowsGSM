﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WindowsGSM.GameServer.Query
{
    class UT3
    {
        private static readonly byte[] UT3_MAGIC = { 0xFE, 0xFD };
        private static readonly byte[] UT3_HANDSHAKE = { 0x09 };
        private static readonly byte[] UT3_INFO = { 0x00 };
        private static readonly byte[] UT3_SESSIONID = { 0x10, 0x20, 0x30, 0x40 };

        private UdpClient _udpClient;
        private IPEndPoint _IPEndPoint;
        private int _timeout;

        public UT3() { }

        public UT3(string address, int port, int timeout = 5)
        {
            SetAddressPort(address, port, timeout);
        }

        public void SetAddressPort(string address, int port, int timeout = 5)
        {
            _IPEndPoint = new IPEndPoint(IPAddress.Parse(address), port);
            _timeout = timeout;
        }

        public async Task<Dictionary<string, string>> GetInfo()
        {
            return await Task.Run(() =>
            {
                try
                {
                    _udpClient = new UdpClient();
                    _udpClient.Client.SendTimeout = _udpClient.Client.ReceiveTimeout = _timeout * 1000;
                    _udpClient.Connect(_IPEndPoint);

                    // Send UT3_HANDSHAKE request
                    byte[] request = new byte[0].Concat(UT3_MAGIC).Concat(UT3_HANDSHAKE).Concat(UT3_SESSIONID).ToArray();
                    _udpClient.Send(request, request.Length);

                    // Receive response
                    byte[] token = GetToken(_udpClient.Receive(ref _IPEndPoint).ToArray());

                    // Send UT3_INFO request
                    request = new byte[0].Concat(UT3_MAGIC).Concat(UT3_INFO).Concat(UT3_SESSIONID).Concat(token).ToArray();
                    _udpClient.Send(request, request.Length);

                    // Receive response
                    byte[] response = _udpClient.Receive(ref _IPEndPoint).Skip(5).ToArray();

                    var keys = new Dictionary<string, string>();
                    using (var br = new BinaryReader(new MemoryStream(response), Encoding.UTF8))
                    {
                        keys["MOTD"] = ReadString(br);
                        keys["GameType"] = ReadString(br);
                        keys["Map"] = ReadString(br);
                        keys["Players"] = ReadString(br);
                        keys["MaxPlayers"] = ReadString(br);
                        keys["Port"] = br.ReadInt16().ToString();
                        keys["IP"] = ReadString(br);
                    }

                    return keys.Count <= 0 ? null : keys;
                }
                catch
                {
                    return null;
                }
            });
        }

        private byte[] GetToken(byte[] response)
        {
            Int32 challenge = Int32.Parse(Encoding.ASCII.GetString(response.Skip(5).ToArray()));
            return new byte[] { (byte)(challenge >> 24 & 0xFF), (byte)(challenge >> 16 & 0xFF), (byte)(challenge >> 8 & 0xFF), (byte)(challenge >> 0 & 0xFF) };
        }

        private string ReadString(BinaryReader br)
        {
            byte[] bytes = new byte[0];

            // Get all bytes until 0x00
            do
            {
                bytes = bytes.Concat(new byte[] { br.ReadByte() }).ToArray();
            }
            while (bytes[bytes.Length - 1] != 0x00);

            // Return bytes in UTF8 except the last byte because it is 0x00
            return Encoding.UTF8.GetString(bytes.Take(bytes.Length - 1).ToArray());
        }

        public async Task<string> GetPlayersAndMaxPlayers()
        {
            try
            {
                Dictionary<string, string> kv = await GetInfo();
                return kv["Players"] + '/' + kv["MaxPlayers"];
            }
            catch
            {
                return null;
            }
        }
    }
}
