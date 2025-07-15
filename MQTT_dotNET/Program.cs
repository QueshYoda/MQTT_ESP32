// M2Mqtt için gerekli using bildirimleri
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

using System; // Console.WriteLine için ekledik
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        // 1. Broker'ın IP adresi. Kendi Mosquitto IP adresinizi yazın.
        string brokerIp = "192.168.1.102";

        // 2. MqttClient nesnesi doğrudan oluşturulur.
        MqttClient client = new MqttClient(brokerIp);

        // 3. Bir mesaj geldiğinde tetiklenecek olan metodu (event handler) kaydet.
        client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;

        // 4. Benzersiz bir Client ID oluştur.
        string clientId = Guid.NewGuid().ToString();

        try
        {
            // 5. Broker'a bağlan.
            client.Connect(clientId);
            Console.WriteLine("MQTT Broker'a başarıyla bağlanıldı.");

            // 6. Abone olunacak konuyu (topic) ve hizmet kalitesini (QoS) belirle.
            string subscribeTopic = "esp32/distance/data"; // ESP32'nin yayın yaptığı konu
            byte qosLevel = MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE; // QoS 1

            // 7. Belirlenen konuya abone ol.
            client.Subscribe(new string[] { subscribeTopic }, new byte[] { qosLevel });
            Console.WriteLine($"'{subscribeTopic}' konusuna abone olundu. Mesajlar bekleniyor...");

            // =================================================================
            // YENİ EKLENEN KISIM: ESP32'ye VERİ GÖNDERME
            // =================================================================

            Console.WriteLine("\n----------------------------------------------------");
            Console.WriteLine("ESP32'ye mesaj göndermek için komut yazıp Enter'a basın.");
            Console.WriteLine("Örnek: LED_ON, LED_OFF, GET_STATUS");
            Console.WriteLine("Uygulamadan çıkmak için 'exit' yazın.");
            Console.WriteLine("----------------------------------------------------");

            string publishTopic = "esp32/command"; // ESP32'ye komut göndereceğimiz konu

            while (true)
            {
                string messageToSend = Console.ReadLine();

                if (messageToSend.ToLower() == "exit")
                {
                    break; // Döngüden çık ve uygulamayı sonlandır.
                }

                if (client.IsConnected && !string.IsNullOrEmpty(messageToSend))
                {
                    // Gönderilecek mesajı byte dizisine çevir
                    byte[] payload = Encoding.UTF8.GetBytes(messageToSend);

                    // Mesajı yayınla (Publish)
                    client.Publish(
                        publishTopic,    // Konu (Topic)
                        payload,         // Mesaj içeriği (Payload)
                        MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, // Hizmet Kalitesi (QoS)
                        false            // Mesajın saklanıp saklanmayacağı (Retain)
                    );

                    Console.WriteLine($"-> GÖNDERİLDİ: Konu='{publishTopic}', Mesaj='{messageToSend}'");
                }
                else if (!client.IsConnected)
                {
                    Console.WriteLine("HATA: Mesaj gönderilemedi. Broker bağlantısı kopmuş.");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Broker'a bağlanırken hata oluştu: {ex.Message}");
            Console.WriteLine("IP adresini ve broker'ın çalıştığını kontrol edin.");
        }

        Console.WriteLine("Uygulama kapatılıyor...");
        // Uygulama kapanırken bağlantıyı kes
        if (client != null && client.IsConnected)
        {
            client.Disconnect();
        }
    }

    // 3a. MqttMsgPublishReceived olayı tetiklendiğinde çalışacak olan metot.
    static void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
    {
        // Gelen mesajın içeriğini (payload) byte dizisinden string'e çevir
        var message = Encoding.UTF8.GetString(e.Message);
        var topic = e.Topic;

        // Gelen veriyi ekrana yazdır
        Console.WriteLine($"<- GELDİ: Konu='{topic}', Mesaj='{message}'");
    }
}