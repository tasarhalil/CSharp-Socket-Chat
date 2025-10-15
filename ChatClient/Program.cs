using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChatClient
{
    class Client
    {
        static void Main(string[] args)
        {
            // Kullanıcıdan sunucuya bağlanmak için bilgileri al
            Console.Write("Kullanıcı adınızı girin: ");
            string username = Console.ReadLine();

            // 127.0.0.1 (localhost) IP adresi ve 5000 portuna bağlanıyoruz.
            string serverIp = "127.0.0.1";
            int port = 5000;

            TcpClient client = null;

            try
            {
                client = new TcpClient(serverIp, port);
                Console.WriteLine($"Sunucuya bağlanıldı. ({serverIp}:{port})");

                NetworkStream stream = client.GetStream();

                // Sunucuya ilk olarak kullanıcı adını gönder
                byte[] usernameBytes = Encoding.UTF8.GetBytes(username);
                stream.Write(usernameBytes, 0, usernameBytes.Length);

                // Sunucudan gelen mesajları dinlemek için yeni bir thread başlat
                Thread receiveThread = new Thread(() => ReceiveMessages(stream));
                receiveThread.Start();

                Console.WriteLine("Mesajlarınızı yazıp Enter'a basabilirsiniz. Çıkış için 'exit' yazın.");

                // Kullanıcıdan mesaj alıp sunucuya gönder
                while (true)
                {
                    string messageToSend = Console.ReadLine();
                    if (messageToSend.ToLower() == "exit")
                    {
                        break;
                    }
                    byte[] buffer = Encoding.UTF8.GetBytes(messageToSend);
                    stream.Write(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Sunucuya bağlanılamadı: " + ex.Message);
            }
            finally
            {
                // Uygulama kapanırken istemciyi düzgünce kapat
                if (client != null)
                {
                    client.Close();
                }
            }
        }

        private static void ReceiveMessages(NetworkStream stream)
        {
            byte[] buffer = new byte[1024];
            int byteCount;

            try
            {
                // Sunucudan sürekli olarak mesajları dinle ve ekrana yazdır
                while ((byteCount = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, byteCount);
                    Console.WriteLine(receivedMessage);
                }
            }
            catch (Exception)
            {
                // Sunucu bağlantısı koptuğunda bu bloğa düşer.
                Console.WriteLine("Sunucu ile bağlantı kesildi.");
                Console.WriteLine("Çıkmak için bir tuşa basın...");
                // Not: Burada uygulamayı sonlandırmak için ek mantık eklenebilir.
            }
        }
    }
}

