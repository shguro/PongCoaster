#include <WS2812FX.h>
#include <ArduinoOTA.h>
#include <ESPUI.h>
#include <ESP8266WiFi.h>
#include <ESP8266mDNS.h>
#include <Preferences.h>
#include <HX711_ADC.h>
#include <DNSServer.h>

//Main stuff
Preferences preferences;

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
//#define SCK_DISABLE_INTERRUPTS 1

//Voltage
#define VOLTAGE_WINDOW_SIZE 10
int voltageIndex = 0;
double voltageValue = 0;
double voltageSum = 0;
double voltageReadings[VOLTAGE_WINDOW_SIZE];
double voltageAvarage = 0;
unsigned long previousMillisVoltage = 0;

//Network Configuration
const byte DNS_PORT = 53;
IPAddress apIP(192, 168, 4, 1);
DNSServer dnsServer;
char hostname_array[32];
String hostname;
String ssid;
String wifiPass;

//UI Elememnts
uint16_t mainTab;
uint16_t settingsTab;
uint16_t valueLabel;
uint16_t voltageLabel;
uint16_t modeSelect;
uint16_t hostnameText;
uint16_t ssidText;
uint16_t wifiPassText;
uint16_t configSaveButton;

//callbacks
void tareButtonCallback(Control* sender, int type) {
    switch (type) {
        case B_DOWN:
            LoadCell.tareNoDelay();
            break;

        case B_UP:
            break;
    }
}

void modeSelectCallback(Control* sender, int type) {
    String input = sender->value;
    if (sender->value.equals("Rainbow")) {
        ws2812fx.setBrightness(255);
        ws2812fx.setMode(FX_MODE_RAINBOW_CYCLE);
    } else if (sender->value.equals("Power Off")) {
        ws2812fx.setBrightness(0);
    }
}

void configSaveButtonCallback(Control* sender, int type) {   
    switch (type) {
    case B_DOWN:
        noInterrupts();
        hostname = ESPUI.getControl(hostnameText)->value;
        ssid = ESPUI.getControl(ssidText)->value;
        wifiPass = ESPUI.getControl(wifiPassText)->value;

        Serial.println(ssid);

        preferences.begin("PongCoaster", false);
        preferences.putString("hostname", hostname);
        preferences.putString("ssid", ssid);
        preferences.putString("wifiPass", wifiPass);
        preferences.end();
        Serial.print("Hostname: " + hostname + "\nSSID: " + ssid + "\n"+ "\nPass: " + wifiPass + "\n");

        delay(1000);
        ESP.restart();
    }
}

void textCallback(Control* sender, int type) {
    Serial.println(sender->value);
}

//interrupt routine:
void ICACHE_RAM_ATTR dataReadyISR() {
  if (LoadCell.update()) {
    newDataReady = 1;
  }
}

void connectWifi() {
	int connect_timeout;
	Serial.print("Load Wifi Settings\n");

    preferences.begin("PongCoaster", false);

    hostname = preferences.getString("hostname", "PONGCOASTER");
    ssid = preferences.getString("ssid", "PONGCOASTER");
    wifiPass = preferences.getString("wifiPass","DefaultPassword");
    Serial.print("Hostname: " + hostname + "\nSSID: " + ssid + "\n"+ "\nPass: " + wifiPass + "\n");

    preferences.end();

	WiFi.hostname(hostname);
	Serial.println("Begin wifi...");
    WiFi.begin(ssid, wifiPass);

    connect_timeout = 28; //7 seconds
    while (WiFi.status() != WL_CONNECTED && connect_timeout > 0) {
        delay(250);
        Serial.print(".");
        connect_timeout--;
    }
		
	if (WiFi.status() == WL_CONNECTED) {
		Serial.println(WiFi.localIP());
		Serial.println("Wifi started");

		if (!MDNS.begin(hostname)) {
			Serial.println("Error setting up MDNS responder!");
		}
	} else {
		Serial.println("\nCreating access point...");
		WiFi.mode(WIFI_AP);
		WiFi.softAPConfig(apIP, apIP, IPAddress(255, 255, 255, 0));
		WiFi.softAP(hostname);


		connect_timeout = 20;
		do {
			delay(250);
			Serial.print(",");
			connect_timeout--;
		} while(connect_timeout);
        dnsServer.start(DNS_PORT, "*", apIP);
	}
}

void setup() {
    Serial.begin(57600);
    connectWifi();
    ArduinoOTA.begin();

    //Battery Input
    pinMode(A0, INPUT);

    //LED Setup
    ws2812fx.init();
    ws2812fx.setBrightness(100);
    ws2812fx.setSpeed(1);
    ws2812fx.setMode(FX_MODE_RAINBOW_CYCLE);
    ws2812fx.start();                        //Setup Function

    //Setup ESPUi
    ESPUI.setVerbosity(Verbosity::Quiet);
    mainTab = ESPUI.addControl(ControlType::Tab, "Main", "Main");
    settingsTab = ESPUI.addControl(ControlType::Tab, "Settings", "Settings");

    //Main Tab
    ESPUI.addControl(ControlType::Button, "Tare scale", "Tare", ControlColor::Peterriver, mainTab, &tareButtonCallback);

    valueLabel = ESPUI.addControl(ControlType::Label, "Value in g:", "", ControlColor::Turquoise, mainTab);
    voltageLabel = ESPUI.addControl(ControlType::Label, "Battery Voltage:", "", ControlColor::Sunflower, mainTab);

    uint16_t modeSelect = ESPUI.addControl(ControlType::Select, "Mode Select", "Rainbow", ControlColor::Alizarin, mainTab, &modeSelectCallback);

	for(auto const& v : modes) {
		ESPUI.addControl(Option, v.c_str(), v, None, modeSelect);
	}

    //SettingsTab
    ssidText = ESPUI.addControl(ControlType::Text, "SSID", ssid, ControlColor::Carrot, settingsTab, &textCallback);
    wifiPassText = ESPUI.addControl(ControlType::Text, "Wifi Password", wifiPass, ControlColor::Carrot, settingsTab, &textCallback);
    hostnameText = ESPUI.addControl(ControlType::Text, "Hostname", hostname, ControlColor::Carrot, settingsTab, &textCallback);
    configSaveButton = ESPUI.addControl(ControlType::Button, "Save Configuration", "Save & Reboot", ControlColor::Carrot, settingsTab, &configSaveButtonCallback);

    //Start ESPUi
    int hostname_len = hostname.length() + 1;
    hostname.toCharArray(hostname_array,hostname_len);
    ESPUI.begin(hostname_array);

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
}

void loop() {
    ArduinoOTA.handle();
    if(WiFi.getMode() == WIFI_AP) {
        dnsServer.processNextRequest();
    } else {
        MDNS.update();
    }
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
        ESPUI.print(valueLabel, String(weight));
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

        ESPUI.print(voltageLabel, String(voltageAvarage, 2));
    }
}