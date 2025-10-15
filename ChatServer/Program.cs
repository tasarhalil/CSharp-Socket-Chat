using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChatServer
{
    class Server
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<TcpClient, string> clients = new Dictionary<TcpClient, string>();

        static void Main(string[] args)
        {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            int port = 5000;
            TcpListener serverListener = new TcpListener(ipAddress, port);
            serverListener.Start();

            Console.WriteLine($"Sunucu başlatıldı. Port: {port}");
            Console.WriteLine("Komutlar: kick [kullanıcı], broadcast [mesaj]");
            LogMessage("[SİSTEM] Sunucu başlatıldı.");

            // Sunucu komutlarını dinlemek için yeni bir thread başlat
            Thread consoleThread = new Thread(ListenForServerCommands);
            consoleThread.IsBackground = true;
            consoleThread.Start();

            while (true)
            {
                TcpClient client = serverListener.AcceptTcpClient();
                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
        }

        private static void ListenForServerCommands()
        {
            while (true)
            {
                string command = Console.ReadLine();
                if (string.IsNullOrEmpty(command)) continue;

                string[] parts = command.Split(new[] { ' ' }, 2);
                string action = parts[0].ToLower();

                if (action == "kick" && parts.Length > 1)
                {
                    KickUser(parts[1]);
                }
                else if (action == "broadcast" && parts.Length > 1)
                {
                    string message = $"CHAT:[{DateTime.Now:HH:mm:ss}] [YÖNETİCİ]: {parts[1]}";
                    Console.WriteLine($"Yayın yapılıyor: {message}");
                    LogMessage($"[YAYIN] {parts[1]}");
                    Broadcast(message, null);
                }
                else
                {
                    Console.WriteLine("Geçersiz komut. Kullanım: kick [kullanıcı] veya broadcast [mesaj]");
                }
            }
        }

        private static void KickUser(string username)
        {
            lock (_lock)
            {
                TcpClient clientToKick = clients.FirstOrDefault(c => c.Value.Equals(username, StringComparison.OrdinalIgnoreCase)).Key;
                if (clientToKick != null)
                {
                    try
                    {
                        byte[] kickMessage = Encoding.UTF8.GetBytes("KICK:Sunucu yöneticisi tarafından atıldınız.");
                        clientToKick.GetStream().Write(kickMessage, 0, kickMessage.Length);
                        clientToKick.Close(); // Bu işlem HandleClient'taki finally bloğunu tetikleyecektir.
                        Console.WriteLine($"{username} sunucudan atıldı.");
                        LogMessage($"[SİSTEM] {username} yöneticisi tarafından atıldı.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{username} atılırken hata: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Kullanıcı bulunamadı: {username}");
                }
            }
        }

        private static void HandleClient(TcpClient client)
        {
            string username = "";
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int byteCount = stream.Read(buffer, 0, buffer.Length);
                username = Encoding.UTF8.GetString(buffer, 0, byteCount).Trim();

                if (clients.ContainsValue(username))
                {
                    Console.WriteLine($"{username} zaten mevcut. Bağlantı reddedildi.");
                    byte[] errorMsg = Encoding.UTF8.GetBytes("ERROR:Bu kullanıcı adı zaten alınmış.");
                    stream.Write(errorMsg, 0, errorMsg.Length);
                    LogMessage($"[HATA] {username} kullanıcı adı zaten mevcut olduğu için bağlantı reddedildi.");
                    client.Close();
                    return;
                }

                lock (_lock) { clients.Add(client, username); }

                Console.WriteLine($"{username} sohbete katıldı.");
                LogMessage($"[BAĞLANTI] {username} sohbete katıldı.");
                string time = DateTime.Now.ToString("HH:mm:ss");

                string userListMessage;
                lock (_lock) { userListMessage = "USERLIST:" + string.Join(",", clients.Values); }
                byte[] userListBytes = Encoding.UTF8.GetBytes(userListMessage);
                stream.Write(userListBytes, 0, userListBytes.Length);

                Broadcast($"JOIN:{username}", client);
                Broadcast($"CHAT:[{time}] [Sunucu]: {username} sohbete katıldı.", client);

                while ((byteCount = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, byteCount);
                    time = DateTime.Now.ToString("HH:mm:ss");

                    if (message.StartsWith("PM:"))
                    {
                        string[] parts = message.Split(new char[] { ':' }, 3);
                        if (parts.Length == 3) SendPrivateMessage(username, parts[1], parts[2], time);
                    }
                    else if (message.StartsWith("TYPING:"))
                    {
                        Broadcast($"TYPING:{username}", client);
                    }
                    else if (message.StartsWith("STOPTYPING:"))
                    {
                        Broadcast($"STOPTYPING:{username}", client);
                    }
                    else
                    {
                        Console.WriteLine($"Gelen mesaj: {username}: {message}");
                        LogMessage($"[GENEL] {username}: {message}");
                        Broadcast($"CHAT:[{time}] {username}: {message}", null);
                    }
                }
            }
            catch (Exception) { }
            finally
            {
                if (!string.IsNullOrEmpty(username))
                {
                    lock (_lock) { clients.Remove(client); }
                    client.Close();
                    Console.WriteLine($"{username} sohbetten ayrıldı.");
                    LogMessage($"[BAĞLANTI] {username} sohbetten ayrıldı.");
                    string time = DateTime.Now.ToString("HH:mm:ss");
                    Broadcast($"LEAVE:{username}", null);
                    Broadcast($"CHAT:[{time}] [Sunucu]: {username} sohbetten ayrıldı.", null);
                }
            }
        }

        private static void Broadcast(string message, TcpClient sender)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            lock (_lock)
            {
                foreach (var c in clients.Keys)
                {
                    if (c != sender)
                    {
                        try
                        {
                            NetworkStream stream = c.GetStream();
                            stream.Write(buffer, 0, buffer.Length);
                        }
                        catch { }
                    }
                }
            }
        }

        private static void SendPrivateMessage(string fromUser, string toUser, string message, string time)
        {
            LogMessage($"[ÖZEL] {fromUser} -> {toUser}: {message}");
            TcpClient receiverClient = null;
            TcpClient senderClient = null;

            lock (_lock)
            {
                receiverClient = clients.FirstOrDefault(c => c.Value.Equals(toUser, StringComparison.OrdinalIgnoreCase)).Key;
                senderClient = clients.FirstOrDefault(c => c.Value.Equals(fromUser, StringComparison.OrdinalIgnoreCase)).Key;
            }

            if (receiverClient != null)
            {
                string receiverMessage = $"PM_IN:[{time}] {fromUser} (Özel): {message}";
                byte[] receiverBuffer = Encoding.UTF8.GetBytes(receiverMessage);
                receiverClient.GetStream().Write(receiverBuffer, 0, receiverBuffer.Length);
            }

            if (senderClient != null)
            {
                string senderMessage = $"PM_OUT:[{time}] -> {toUser} (Özel): {message}";
                byte[] senderBuffer = Encoding.UTF8.GetBytes(senderMessage);
                senderClient.GetStream().Write(senderBuffer, 0, senderBuffer.Length);
            }
        }

        private static void LogMessage(string message)
        {
            try
            {
                string logFileName = $"chatlog_{DateTime.Now:yyyy-MM-dd}.txt";
                using (StreamWriter writer = new StreamWriter(logFileName, true))
                {
                    writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
                }
            }
            catch (Exception ex) { Console.WriteLine($"[HATA] Log dosyasına yazılamadı: {ex.Message}"); }
        }
    }
}

