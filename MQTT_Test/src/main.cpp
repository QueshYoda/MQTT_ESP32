#include <Arduino.h>
#include <WiFi.h>
#include <PubSubClient.h>
#include <Wire.h>

// Sensör Pinleri
#define Echo_PIN 25
#define Trig_PIN 26

// LED Pin
#define LED_PIN 2 


// WiFi Bilgileri
const char* ssid = "Boooh";
const char* password = "pisssslik";

// MQTT Bilgileri
const char* mqtt_server = "192.168.1.102";
const int mqtt_port = 1883;
const char* mqtt_pub_topic = "esp32/distance/data";  // Veri GÖNDERİLECEK konu
const char* mqtt_sub_topic = "esp32/command";        // Komut DİNLENECEK konu 
String MAC;


WiFiClient espClient;
PubSubClient client(espClient);

// MQTT'den mesaj geldiğinde çalışacak fonksiyon
void callback(char* topic, byte* payload, unsigned int length) {
  Serial.print("Mesaj geldi, konu: ");
  Serial.println(topic);

  // Gelen mesajı string'e çevir
  String message;
  for (int i = 0; i < length; i++) {
    message += (char)payload[i];
  }
  Serial.print("Mesaj: ");
  Serial.println(message);

  // Gelen mesaja göre işlem yap
  if (message == "LED_ON") {
    Serial.println("LED ON");
    digitalWrite(LED_PIN, HIGH);
  } else if (message == "LED_OFF") {
    Serial.println("LED OFF");
    digitalWrite(LED_PIN, LOW);
  }
}

void wifiConnect() {
  Serial.print("Connecting to ");
  Serial.println(ssid);
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(" . ");
  }
  Serial.println("\nWiFi Connected :)");
  Serial.print("IP address: ");
  Serial.println(WiFi.localIP());
  Serial.print("MAC address: ");
  MAC = WiFi.macAddress();
  Serial.println(MAC);
}

void reconnect() {
  while (!client.connected()) {
    Serial.print("Attempting MQTT connection...");
    String clientId = "ESP32Client-" + String(random(0xffff), HEX);
    if (client.connect(clientId.c_str())) {
      Serial.println("connected");
      // Bağlantı kurulunca komut konusuna abone ol
      client.subscribe(mqtt_sub_topic);
      Serial.print("Subscribed to: ");
      Serial.println(mqtt_sub_topic);
    } else {
      Serial.print("failed, rc=");
      Serial.print(client.state());
      Serial.println(" try again in 5 seconds");
      delay(5000);
    }
  }
}

void setUpDistance() {
  pinMode(Trig_PIN, OUTPUT);
  pinMode(Echo_PIN, INPUT);
}

u_int64_t ReadDistance() {
  digitalWrite(Trig_PIN, LOW);
  delayMicroseconds(2);
  digitalWrite(Trig_PIN, HIGH);
  delayMicroseconds(10);
  digitalWrite(Trig_PIN, LOW);
  u_int64_t duration = pulseIn(Echo_PIN, HIGH);
  u_int64_t distance = duration / 58.2;
  return distance;
}

void setup() {
  Serial.begin(115200);
  pinMode(LED_PIN, OUTPUT); 
  setUpDistance();
  wifiConnect();
  
  // Sunucu ve callback fonksiyonunu ayarla
  client.setServer(mqtt_server, mqtt_port);
  client.setCallback(callback); 
}

void loop() {
  if (!client.connected()) {
    reconnect();
  }
  client.loop(); // MQTT dinlemesini aktif tutar

  // Veriyi oku
  u_int64_t data = ReadDistance();

  // Gönderilecek mesajı hazırla
  String buffer = String(data) + "cm MAC:" + String(MAC);

  // MQTT konusuna yayınla
  client.publish(mqtt_pub_topic, buffer.c_str());
  
  Serial.print("Published: ");
  Serial.println(buffer);
  
  delay(5000);
}