#include <WS2812FX.h>
#include <ArduinoOTA.h>
#include <ESP8266WiFi.h>
#include <Preferences.h>
#include <HX711_ADC.h>
#include <DNSServer.h>
#include <ESP8266WebServer.h>
#include <WiFiManager.h>
#include <ESP8266mDNS.h>
#include <PubSubClient.h>

//Config
Preferences preferences;
bool shouldSaveConfig = false;
char hostname[32] = "PONGCOASTER";
char mqtt_server[40];
char mqtt_port[6] = "1883";
char api_token[34] = "YOUR_API_TOKEN";

//WifiManager
WiFiManager wifiManager;

//MQTT
WiFiClient espClient;
PubSubClient client(espClient);
char weightTopic[50] = "weight/";
char voltageTopic[50] = "voltage/";

//LED
#define LED_COUNT 18
#define LED_PIN D3
WS2812FX ws2812fx = WS2812FX(LED_COUNT, LED_PIN, NEO_GRB);
unsigned long previousMillisLed = 0;
static String modes[] {"Power Off", "Rainbow"};


//Scale parametes
const int HX711_dout = D1; //mcu > HX711 dout pin, must be external interrupt capable!
const int HX711_sck = D2; //mcu > HX711 sck pin
HX711_ADC LoadCell(HX711_dout, HX711_sck); //DOUT, CLK
float calibration_factor = 1090;       //Assuming a calibration_factor
float weight;
double tareValue;
volatile boolean newDataReady;

//Voltage
#define VOLTAGE_WINDOW_SIZE 10
int voltageIndex = 0;
double voltageValue = 0;
double voltageSum = 0;
double voltageReadings[VOLTAGE_WINDOW_SIZE];
double voltageAvarage = 0;
unsigned long previousMillisVoltage = 0;

//interrupt routine:
void ICACHE_RAM_ATTR dataReadyISR() {
  if (LoadCell.update()) {
    newDataReady = 1;
  }
}

//callback notifying us of the need to save config
void saveConfigCallback () {
  Serial.println("Should save config");
  shouldSaveConfig = true;
}

void mqttCallback(char* topic, byte* payload, unsigned int length) {
  Serial.print("Message arrived [");
  Serial.print(topic);
  Serial.print("] ");
  for (int i = 0; i < length; i++) {
    Serial.print((char)payload[i]);
  }
  Serial.println();
}

void reconnect() {
  // Loop until we're reconnected
  while (!client.connected()) {
    Serial.print("Attempting MQTT connection...");
    // Create a random client ID
    String clientId = "ESP8266Client-";
    clientId += String(random(0xffff), HEX);
    // Attempt to connect
    if (client.connect(clientId.c_str())) {
      Serial.println("connected");
      // Once connected, publish an announcement...
      client.publish("outTopic", "hello world");
      // ... and resubscribe
      client.subscribe("inTopic");
    } else {
      Serial.print("failed, rc=");
      Serial.print(client.state());
      Serial.println(" try again in 5 seconds");
      // Wait 5 seconds before retrying
      delay(5000);
    }
  }
}

void setup() {
    Serial.begin(57600);
    preferences.begin("PongCoaster", false);
    preferences.getString("hostname", "PONGCOASTER").toCharArray(hostname, 32);
    preferences.getString("mqtt_server", "").toCharArray(mqtt_server, 40);
    preferences.getString("mqtt_port", "1883").toCharArray(mqtt_port, 6);
    preferences.getString("api_token", "YOUR_API_TOKEN").toCharArray(api_token, 34);

    WiFiManagerParameter custom_mqtt_server("server", "mqtt server", mqtt_server, 40);
    WiFiManagerParameter custom_mqtt_port("port", "mqtt port", mqtt_port, 6);
    WiFiManagerParameter custom_api_token("apikey", "API token", api_token, 32);
    WiFiManagerParameter custom_hostname("hostname", "Hostname", hostname, 32);

    wifiManager.setSaveConfigCallback(saveConfigCallback);

    //add all your parameters here
    wifiManager.addParameter(&custom_mqtt_server);
    wifiManager.addParameter(&custom_mqtt_port);
    wifiManager.addParameter(&custom_api_token);

    Serial.print("Hostname: ");
    Serial.println(hostname);
    
    bool result;
    result = wifiManager.autoConnect(hostname);
    if(!result) {
        Serial.println("Failed to connect");
    } 
    else {
        //if you get here you have connected to the WiFi    
        Serial.println("connected...yeey :)");
    }

    //read updated parameters
    strcpy(hostname, custom_hostname.getValue());
    strcpy(mqtt_server, custom_mqtt_server.getValue());
    strcpy(mqtt_port, custom_mqtt_port.getValue());
    strcpy(api_token, custom_api_token.getValue());

    Serial.println("The values in the file are: ");
    Serial.println("\thostname : " + String(hostname));
    Serial.println("\tmqtt_server : " + String(mqtt_server));
    Serial.println("\tmqtt_port : " + String(mqtt_port));
    Serial.println("\tapi_token : " + String(api_token));

    if(shouldSaveConfig) {
        preferences.putString("hostname", hostname);
        preferences.putString("mqtt_server", mqtt_server);
        preferences.putString("mqtt_port", mqtt_port);
        preferences.putString("api_token", api_token);
    }

    preferences.end();

    MDNS.begin(hostname);
    ArduinoOTA.begin();

    //Battery Input
    pinMode(A0, INPUT);

    //LED Setup
    ws2812fx.init();
    ws2812fx.setBrightness(100);
    ws2812fx.setSpeed(1);
    ws2812fx.setMode(FX_MODE_RAINBOW_CYCLE);
    ws2812fx.start();                        //Setup Function


    //Load Cell Setup
    LoadCell.begin();
    LoadCell.start(2000, true);
    if (LoadCell.getTareTimeoutFlag()) {
        Serial.println("Timeout, check MCU>HX711 wiring and pin designations");
        while (1);
    }
    else {
        LoadCell.setCalFactor(calibration_factor); // set calibration value (float)
        Serial.println("Startup is complete");
    }
    attachInterrupt(digitalPinToInterrupt(HX711_dout), dataReadyISR, FALLING);

    client.setServer(mqtt_server, atoi(mqtt_port));
    client.setCallback(mqttCallback);

    strcat(weightTopic, hostname);
    strcat(voltageTopic, hostname);

}

void loop() {
    ArduinoOTA.handle();
    MDNS.update();

    if (!client.connected()) {
        reconnect();
    }
    client.loop();

    unsigned long currentMillis = millis();

    //LED Handling
    if(currentMillis - previousMillisLed >= 1000/20) { //60fps
        previousMillisLed = currentMillis;
        ws2812fx.service();
    }

    //Read Load Cell
    // get smoothed value from the dataset:
    if (newDataReady) {
        weight = LoadCell.getData();      // get smoothed value from the dataset:
        newDataReady = 0;
        char weightString[50];
        sprintf(weightString, "%f", weight);
                Serial.println(weight);

        client.publish(weightTopic, weightString);
    }


    //Read Voltage
    if(currentMillis - previousMillisVoltage >= 2000) {
        previousMillisVoltage = currentMillis;
        voltageSum = voltageSum - voltageReadings[voltageIndex];       // Remove the oldest entry from the sum
        voltageValue = (analogRead(A0)/1023.0)*4.5;        // Read the next sensor value
        voltageReadings[voltageIndex] = voltageValue;           // Add the newest reading to the window
        voltageSum = voltageSum + voltageValue;                 // Add the newest reading to the sum
        voltageIndex = (voltageIndex+1) % VOLTAGE_WINDOW_SIZE;   // Increment the index, and wrap to 0 if it exceeds the window size
        voltageAvarage = voltageSum / VOLTAGE_WINDOW_SIZE;      // Divide the sum of the window by the window size for the result
        char voltageString[50];
        sprintf(voltageString, "%f", voltageAvarage);
        client.publish(voltageTopic, voltageString);
    }
}