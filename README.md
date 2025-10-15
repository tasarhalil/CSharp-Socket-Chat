# C# Soket Tabanlı Çok Kullanıcılı Chat Uygulaması

Bu proje, bir okul ödevi kapsamında Visual Studio 2022 kullanılarak C# dili ile geliştirilmiş, soket programlama prensiplerine dayanan çok kullanıcılı bir anlık mesajlaşma uygulamasıdır.

## 📝 Açıklama

Uygulama, bir sunucu (server) ve bu sunucuya bağlanabilen çok sayıda istemciden (client) oluşur. Sunucu, istemciler arasındaki mesaj trafiğini yönetir ve tüm kullanıcılara mesajları eş zamanlı olarak iletir. Proje, TCP soketlerinin temellerini ve C#'ta çoklu iş parçacığı (multi-threading) yönetimini anlamak için pratik bir örnek teşkil eder.

## ✨ Özellikler

- **Merkezi Sunucu:** Tüm istemci bağlantılarını ve mesaj akışını yöneten bir sunucu uygulaması.
- **Çoklu İstemci Desteği:** Sunucuya aynı anda birden fazla istemci bağlanabilir.
- **Gerçek Zamanlı Mesajlaşma:** Bir istemcinin gönderdiği mesaj, anında sunucuya bağlı olan diğer tüm istemcilere iletilir.
- **Kullanıcı Adı Belirleme:** Her istemci sohbete katılmadan önce bir kullanıcı adı belirler.
- **Basit ve Anlaşılır Kod Yapısı:** Okul projesi olduğu için kodlar olabildiğince anlaşılır ve temel prensiplere uygun olarak yazılmıştır.

## 🛠️ Kullanılan Teknolojiler

- **Programlama Dili:** C#
- **Platform:** .NET Framework (veya .NET Core)
- **Ana Teknoloji:** System.Net.Sockets (TCP Listener/Client)
- **Geliştirme Ortamı:** Visual Studio 2022

## 🚀 Kurulum ve Çalıştırma

Projeyi yerel makinenizde çalıştırmak için aşağıdaki adımları izleyin:

1.  **Depoyu Klonlayın:**
    ```bash
    git clone [https://github.com/](https://github.com/)[tasarhalil]/[CSharp-Socket-Chat].git
    ```
2.  **Visual Studio'da Açın:**
    Proje klasöründeki `.sln` uzantılı dosyayı Visual Studio 2022 ile açın.

3.  **Sunucuyu Başlatın:**
    Solution Explorer'dan sunucu projesini (örn: `ChatServer`) "Startup Project" olarak ayarlayın ve çalıştırın (F5).

4.  **İstemcileri Başlatın:**
    - Sunucu çalışırken, istemci projesini (örn: `ChatClient`) sağ tıklayıp `Debug -> Start New Instance` seçeneği ile yeni bir pencerede başlatın.
    - Birden fazla kullanıcıyla test etmek için bu adımı tekrarlayarak istediğiniz kadar istemci penceresi açabilirsiniz.

## 👤 Yazar

**Halil Taşar**

Bu proje, Fırat Üniversitesi - Nesne Tabanlı Programlama dersi kapsamında bir ödev olarak geliştirilmiştir.
