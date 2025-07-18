﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticationServer.Network
{
    public class Database
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private static Database? _instance;
        private static object _lock = new object();

        private Database() { }

        public static Database Instance { 
            get 
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new Database();
                        }
                    }
                }
                return _instance;
            } 
        }

        public bool IsConnected => _client?.Connected ?? false;

        public async Task ConnectAsync(string host, int port)
        {
            Console.WriteLine("접속시도");
            _client = new TcpClient();
            try
            {
                await _client.ConnectAsync(host, port);
                _stream = _client.GetStream();
                Console.WriteLine("Connected to DB server.");
            }
            catch (Exception ex) 
            { Console.WriteLine(ex.Message); }
        }

        public async Task SendAsync(string json)
        {
            if (_stream == null) 
                throw new InvalidOperationException("Not connected to server.");

            byte[] data = Encoding.UTF8.GetBytes(json);
            await _stream.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();
        }
        
        public async Task<byte[]> ReceiveAsync(int bufferSize = 1024)
        {
            if (_stream == null) throw new InvalidOperationException("Not connected to server.");

            var buffer = new byte[bufferSize];
            int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);

            if (bytesRead == 0)
                throw new Exception("Disconnected from server.");

            byte[] result = new byte[bytesRead];
            Array.Copy(buffer, result, bytesRead);
            return result;
        }
    }
}
