#include "Arduino.h"
#include <WiFi.h>
#include <Wire.h>
#include <LiquidCrystal_I2C.h>
#include <WiFiClientSecure.h>
#include <DHT.h>
#include <Preferences.h>
#include <time.h>

// Настройки LCD (20x4 символа)
#define LCD_COLS 20
#define LCD_ROWS 4

#define VALVE_PIN 16     // GPIO для управления краном (HIGH - открыт, LOW - закрыт)

LiquidCrystal_I2C LCD(0x27, LCD_COLS, LCD_ROWS);

// Датчик DHT11
DHT dht(GPIO_NUM_26, DHT11);

// Настройки WiFi
const char* ssid = "HomeWi-Fi(72)"; // Wokwi-GUEST  RT-GPON-AFF7 HomeWi-Fi(72)
const char* password = "R3GXR72F"; // - AY3urdXsAE R3GXR72F 

// Настройки NTP
const char* ntpServer = "pool.ntp.org";
const long gmtOffset_sec = 0;
const int daylightOffset_sec = 0;

// Настройки ThingSpeak
const char* thingspeakHost = "api.thingspeak.com";

// Каналы ThingSpeak
const long sensorDataChannelID = 2405797;
const String sensorDataWriteAPIKey = "PNXZHSN7YKFKMGBW";
const String sensorDataReadAPIKey = "VF7D6JDUS3WKZAO3";

const long boardModeChannelID = 2925691;
const String boardModeReadAPIKey = "FSAAE1JXYRQPAIDL";

const long valveStateChannelID = 2925696;
const String valveStateWriteAPIKey = "HYJF2C4SZR8BU3X8";
const String valveStateReadAPIKey = "FQ5E6QKU0T0IBRPA";

const long scheduleChannelID = 2925698;
const String scheduleReadAPIKey = "Q5EWB0V950NUB9VS";

// Датчики почвы
const int sensorPins[] = {36, 39, 34};
const int numSensors = 3;
int dryValues[3] = {4095, 4095, 4095};
int wetValues[3] = {0, 0, 0};

// Состояния калибровки
enum CalibrationState {
  NORMAL_MODE = 0,
  CALIBRATION_DRY = 1,
  CALIBRATION_WET = 2
};
CalibrationState currentState = NORMAL_MODE;
CalibrationState previousState = NORMAL_MODE;

// Номера полей ThingSpeak
const int FIELD_TEMP = 1;
const int FIELD_HUMIDITY = 2;
const int FIELD_SOIL = 3;
const int FIELD_CONTROL = 1;
const int FIELD_VALVE_STATE = 1;

Preferences preferences;
WiFiClientSecure client;
unsigned long lastUpdateTime = 0;
unsigned long lastCheckTime = 0;
const long updateInterval = 5000;
const long checkInterval = 5000;
const long scheduleLoadInterval = 5*60*1000;  // 1 час для загрузки с сервера
const long scheduleCheckInterval = 60000;   // 1 минута для проверки из кэша

// Переменные для управления краном
int valveState = 0;
int targetValveState = 0;
unsigned long valveOpenTime = 0;
bool valveNeedsClosing = false;
int valveDuration = 0;

// Структура для хранения расписания полива
struct WateringSchedule {
  int startTime;
  int duration;
};

WateringSchedule schedules[4];
unsigned long lastScheduleCheck = 0;


const int TIME_ZONE_OFFSET = 7 * 3600; 

// Структуры для работы с ThingSpeak
struct ThingSpeakData {
  int fieldNumber;
  String value;
};

struct ThingSpeakReadRequest {
  int fieldNumber;
  int* result;
};

// Универсальная функция для отправки данных
bool sendToThingSpeak(const ThingSpeakData data[], int dataCount, 
                     long channelID = sensorDataChannelID, 
                     const String& apiKey = sensorDataWriteAPIKey) {
  String postData = "";
  
  for (int i = 0; i < dataCount; i++) {
    if (i > 0) postData += "&";
    postData += "field" + String(data[i].fieldNumber) + "=" + data[i].value;
  }
  
  postData += "&api_key=" + apiKey;

  if (!client.connect(thingspeakHost, 443)) {
    Serial.println("Connection to ThingSpeak failed!");
    LCD.setCursor(0, 3);
    LCD.print("TS: Conn Fail");
    return false;
  }

  client.println("POST /update HTTP/1.1");
  client.println("Host: " + String(thingspeakHost));
  client.println("Connection: close");
  client.println("Content-Type: application/x-www-form-urlencoded");
  client.print("Content-Length: ");
  client.println(postData.length());
  client.println();
  client.print(postData);

  unsigned long timeout = millis();
  while (!client.available() && millis() - timeout < 5000) {
    delay(10);
  }

  bool success = false;
  while (client.available()) {
    String line = client.readStringUntil('\n');
    if (line.startsWith("HTTP/1.1") && line.indexOf("200") > 0) {
      success = true;
    }
  }

  client.stop();
  
  LCD.setCursor(0, 3);
  if (success) {
    LCD.print("TS: Send OK  ");
  } else {
    LCD.print("TS: Send Fail");
  }
  
  return success;
}

// Функция чтения нескольких полей
bool readMultipleThingSpeakFields(ThingSpeakReadRequest requests[], int requestCount,
                                long channelID, const String& apiKey) {
  if (!client.connect(thingspeakHost, 443)) {
    Serial.println("ОШИБКА: Не удалось подключиться");
    return false;
  }

  String request = String("GET /channels/") + channelID + 
                 "/feeds.json?results=1&api_key=" + apiKey + 
                 " HTTP/1.1\r\n" +
                 "Host: " + thingspeakHost + "\r\n" +
                 "Connection: close\r\n\r\n";
  
  client.print(request);

  unsigned long startTime = millis();
  while (!client.available() && (millis() - startTime) < 10000) {
    delay(100);
  }

  if (!client.available()) {
    Serial.println("ТАЙМАУТ: Ответ не получен");
    client.stop();
    return false;
  }

  String response = "";
  while (client.available()) {
    response += client.readStringUntil('\n');
  }
  client.stop();

  int feedsStart = response.indexOf("\"feeds\":[");
  if (feedsStart == -1) {
    Serial.println("ОШИБКА: Не найден массив feeds");
    return false;
  }

  for (int i = 0; i < requestCount; i++) {
    String fieldPattern = "\"field" + String(requests[i].fieldNumber) + "\":";
    int fieldPos = response.indexOf(fieldPattern, feedsStart);
    
    if (fieldPos == -1) {
      Serial.println("ОШИБКА: Не найдено поле field" + String(requests[i].fieldNumber));
      *(requests[i].result) = -1;
      continue;
    }

    fieldPos += fieldPattern.length();
    int valueStart = fieldPos;
    while (valueStart < response.length() && 
          (response.charAt(valueStart) == ' ' || 
           response.charAt(valueStart) == '\"')) {
      valueStart++;
    }

    int valueEnd = valueStart;
    while (valueEnd < response.length() && 
          response.charAt(valueEnd) != ',' && 
          response.charAt(valueEnd) != '}' &&
          response.charAt(valueEnd) != '\"') {
      valueEnd++;
    }

    String valueStr = response.substring(valueStart, valueEnd);
    valueStr.trim();

    if (valueStr.isEmpty() || valueStr == "null") {
      *(requests[i].result) = -1;
    } else {
      *(requests[i].result) = valueStr.toInt();
    }
  }

  return true;
}

void loadSchedulesFromEEPROM() {
  preferences.begin("watering-sched", true);
  for (int i = 0; i < 4; i++) {
    schedules[i].startTime = preferences.getInt(("time"+String(i)).c_str(), -1);
    schedules[i].duration = preferences.getInt(("dur"+String(i)).c_str(), 0);
    Serial.print("Loaded from EEPROM Schedule ");
    Serial.print(i);
    Serial.print(": Time=");
    Serial.print(schedules[i].startTime);
    Serial.print(", Duration=");
    Serial.println(schedules[i].duration);
  }
  preferences.end();
}

void saveSchedulesToEEPROM() {
  preferences.begin("watering-sched", false);
  for (int i = 0; i < 4; i++) {
    preferences.putInt(("time"+String(i)).c_str(), schedules[i].startTime);
    preferences.putInt(("dur"+String(i)).c_str(), schedules[i].duration);
  }
  preferences.end();
  Serial.println("Schedules saved to EEPROM");
}

void loadWateringSchedules() {
  int times[4], durations[4];
  
  ThingSpeakReadRequest requests[8];
  for (int i = 0; i < 4; i++) {
    requests[i*2] = {1 + i*2, &times[i]};
    requests[i*2+1] = {2 + i*2, &durations[i]};
  }

  if (readMultipleThingSpeakFields(requests, 8, scheduleChannelID, scheduleReadAPIKey)) {
    for (int i = 0; i < 4; i++) {
      schedules[i].startTime = times[i];
      schedules[i].duration = durations[i];
    }
    saveSchedulesToEEPROM();
  } else {
    Serial.println("Ошибка загрузки расписания, используем EEPROM данные");
  }
}

void setup() {
  Serial.begin(115200);
  LCD.init();
  LCD.backlight();
  dht.begin();

  // Загрузка калибровочных значений
  preferences.begin("soil-calib", false);
  for (int i = 0; i < numSensors; i++) {
    dryValues[i] = preferences.getInt(("dry"+String(i)).c_str(), 4095);
    wetValues[i] = preferences.getInt(("wet"+String(i)).c_str(), 0);
  }
  preferences.end();

  // Загрузка расписания из EEPROM при старте
  loadSchedulesFromEEPROM();

  client.setTimeout(10000);
  client.setInsecure();

  WiFi.mode(WIFI_STA);
  WiFi.disconnect(true);  // Удаляем сохранённые сети
  delay(1000);

  Serial.println("Scanning networks...");
  int n = WiFi.scanNetworks();
  if (n == 0) {
    Serial.println("No networks found!");
  } else {
    Serial.println("Available networks:");
    for (int i = 0; i < n; i++) {
      Serial.println(WiFi.SSID(i) + " (" + WiFi.RSSI(i) + " dBm)");
    }
  }

  WiFi.begin(ssid, password);
  Serial.print("Connecting to WiFi");
  Serial.println("SSID: " + String(ssid));
  Serial.println("Password: " + String(password));
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
    Serial.println(WiFi.status());
  }
  Serial.println("\nConnected! IP: " + WiFi.localIP().toString());
  
  configTime(gmtOffset_sec, daylightOffset_sec, ntpServer);
  
  LCD.setCursor(0, 0);
  LCD.print("WiFi Connected");

  pinMode(VALVE_PIN, OUTPUT);
  digitalWrite(VALVE_PIN, LOW); // Изначально кран закрыт

  // Первая загрузка расписания с сервера
  if (WiFi.status() == WL_CONNECTED) {
    loadWateringSchedules();
  }
}

void OpenValve(int duration) {
  digitalWrite(VALVE_PIN, HIGH); // Подаем сигнал на открытие
  Serial.println("Valve OPEN command sent");
  valveState = 1;
  valveOpenTime = millis();
  valveDuration = duration;
  valveNeedsClosing = (duration > 0);
  
  ThingSpeakData valveData[] = {
    {FIELD_VALVE_STATE, String(valveState)}
  };
  sendToThingSpeak(valveData, 1, valveStateChannelID, valveStateWriteAPIKey);
}

void CloseValve() {
  digitalWrite(VALVE_PIN, LOW); // Убираем сигнал - кран закрывается
  Serial.println("Valve CLOSE command sent");
  valveState = 0;
  valveNeedsClosing = false;
  
  ThingSpeakData valveData[] = {
    {FIELD_VALVE_STATE, String(valveState)}
  };
  sendToThingSpeak(valveData, 1, valveStateChannelID, valveStateWriteAPIKey);
}

int readSoilMoisture() {
  float avg = 0;
  for (int i = 0; i < numSensors; i++) {
    int rawValue = analogRead(sensorPins[i]);
    int moisture = map(rawValue, dryValues[i], wetValues[i], 0, 100);
    avg += constrain(moisture, 0, 100);
    delay(10);
  }
  return avg / numSensors;
}

void displaySensorData(float temp, float hum, int soil) {
  LCD.clear();

  LCD.setCursor(0, 0);
  LCD.print("Temp: ");
  LCD.print(temp, 1);
  LCD.print("C");
  
  LCD.setCursor(0, 1);
  LCD.print("Hum: ");
  LCD.print(hum, 1);
  LCD.print("%");
  
  LCD.setCursor(0, 2);
  LCD.print("Soil: ");
  LCD.print(soil);
  LCD.print("%");

  LCD.setCursor(0, 3);
  LCD.print("Valve: ");
  LCD.print(valveState ? "OPEN " : "CLOSED");
  if (valveState) {
    LCD.print(" ");
    unsigned long remaining = (valveDuration * 1000 - (millis() - valveOpenTime)) / 1000;
    LCD.print(remaining);
    LCD.print("s");
  }

  Serial.println("Почва: ");
  Serial.print(soil);
}

void handleStateChange() {
  if (currentState == previousState) return;

  LCD.clear();
  
  switch(currentState) {
    case CALIBRATION_DRY:
      LCD.clear();
      LCD.setCursor(0, 0);
      LCD.print("STEP 1/2: DRY CALIB");
      LCD.setCursor(0, 1);
      LCD.print("1. Place sensors in");
      LCD.setCursor(0, 2);
      LCD.print("DRY environment");
      LCD.setCursor(0, 3);
      LCD.print("2. And press 'Next'");
      break;

    case CALIBRATION_WET:
      for (int i = 0; i < numSensors; i++) {
        dryValues[i] = analogRead(sensorPins[i]);
      }
      LCD.clear();
      LCD.setCursor(0, 0);
      LCD.print("STEP 2/2: WET CALIB");
      LCD.setCursor(0, 1);
      LCD.print("DRY values SAVED!");
      LCD.setCursor(0, 2);
      LCD.print("1. Place in WATER");
      LCD.setCursor(0, 3);
      LCD.print("2. And press 'Done'");
      break;

    case NORMAL_MODE:
      if(previousState == CALIBRATION_WET) {
        for (int i = 0; i < numSensors; i++) {
          wetValues[i] = analogRead(sensorPins[i]);
        }
        
        preferences.begin("soil-calib", false);
        for (int i = 0; i < numSensors; i++) {
          preferences.putInt(("dry"+String(i)).c_str(), dryValues[i]);
          preferences.putInt(("wet"+String(i)).c_str(), wetValues[i]);
        }
        preferences.end();

        LCD.setCursor(0, 0);
        LCD.print("CALIBRATION COMPLETE!");
        LCD.setCursor(0, 1);
        LCD.print("All data saved");
        LCD.setCursor(0, 3);
        LCD.print("Auto exit in 3s");
        
        delay(3000);
      }
      break;
  }
  
  previousState = currentState;
}

void reconnectWiFi() {
  static unsigned long lastAttemptTime = 0;
  const unsigned long attemptInterval = 10000;
  
  if (WiFi.status() == WL_CONNECTED) return;
  
  if (millis() - lastAttemptTime >= attemptInterval) {
    LCD.clear();
    LCD.setCursor(0, 0);
    LCD.print("WiFi Disconnected!");
    LCD.setCursor(0, 1);
    LCD.print("Reconnecting...");
    
    WiFi.disconnect();
    WiFi.begin(ssid, password);
    
    unsigned long startTime = millis();
    while (WiFi.status() != WL_CONNECTED && millis() - startTime < 10000) {
      delay(500);
      LCD.setCursor(0, 2);
      LCD.print("Attempt in ");
      LCD.print((10000 - (millis() - startTime)) / 1000);
      LCD.print("s");
    }
    
    if (WiFi.status() == WL_CONNECTED) {
      LCD.clear();
      LCD.setCursor(0, 0);
      LCD.print("WiFi Connected!");
      LCD.setCursor(0, 1);
      LCD.print("IP: ");
      LCD.print(WiFi.localIP().toString());
      // При переподключении обновляем расписание
      loadWateringSchedules();
    } else {
      LCD.setCursor(0, 3);
      LCD.print("Failed! Retry...");
    }
    
    lastAttemptTime = millis();
  }
}

bool shouldSkipWatering(float temp, float hum, int soil) {
  time_t now = time(nullptr);
  struct tm *timeinfo;
  timeinfo = localtime(&now);
  int currentTimeSec = timeinfo->tm_hour * 3600 + timeinfo->tm_min * 60 + timeinfo->tm_sec;
  
  bool isDayTime = (currentTimeSec >= 6*3600 && currentTimeSec < 22*3600);
  
  if (isDayTime && temp > 30.0) return true;
  if (!isDayTime && temp < 12.0) return true;
  if (hum > 85.0) return true;
  if (soil > 75.0) return true;
  
  return false;
}

void checkManualValveControl() {
  int newValveState;
  ThingSpeakReadRequest request = {FIELD_VALVE_STATE, &newValveState};
  
  if (readMultipleThingSpeakFields(&request, 1, valveStateChannelID, valveStateReadAPIKey)) {
    if (newValveState != -1 && newValveState != targetValveState) {
      targetValveState = newValveState;
      
      if (targetValveState == 1 && valveState == 0) {
        OpenValve(0);
      } else if (targetValveState == 0 && valveState == 1) {
        CloseValve();
      }
    }
  }
}

void checkScheduledValveControl() {
  // Раз в час загружаем расписание с сервера
  if (millis() - lastScheduleCheck >= scheduleLoadInterval) {
    if (WiFi.status() == WL_CONNECTED) {
      loadWateringSchedules();
    }
    lastScheduleCheck = millis();
  }

  // Раз в минуту проверяем расписание из кэша
  static unsigned long lastMinuteCheck = 0;
  if (millis() - lastMinuteCheck < scheduleCheckInterval) return;
  lastMinuteCheck = millis();


  Serial.println("Current Watering Schedules:");
  for (int i = 0; i < 4; i++) {
    Serial.print("Schedule ");
    Serial.print(i);
    Serial.print(": Time=");
    
    if (schedules[i].startTime == -1) {
      Serial.print("NOT SET");
    } else {
      // Конвертация секунд в часы:минуты
      int hours = schedules[i].startTime / 3600;
      int minutes = (schedules[i].startTime % 3600) / 60;
      Serial.print(hours);
      Serial.print(":");
      if (minutes < 10) Serial.print("0");
      Serial.print(minutes);
    }
    
    Serial.print(", Duration=");
    Serial.print(schedules[i].duration);
    Serial.println(" seconds");
  }
  Serial.println("----------------------");

  
  time_t now = time(nullptr);
  struct tm *timeinfo = localtime(&now);
  int currentTimeSec = (timeinfo->tm_hour * 3600) + 
                      (timeinfo->tm_min * 60) + 
                      timeinfo->tm_sec + 
                      TIME_ZONE_OFFSET;
  
  for (int i = 0; i < 4; i++) {
    if (schedules[i].startTime == -1 || valveState == 1) continue;
    Serial.print("\nПроверка расписания #");
    Serial.println(i);

    int timeDiff = currentTimeSec - schedules[i].startTime;
    int absoluteDiff = abs(timeDiff);
    
    Serial.print(" - Время расписания: ");
    Serial.print(schedules[i].startTime / 3600);
    Serial.print(":");
    Serial.print((schedules[i].startTime % 3600) / 60);
    Serial.print(" (");
    Serial.print(schedules[i].startTime);
    Serial.println(" сек)");
    
    Serial.print(" - Разница времени: ");
    Serial.print(timeDiff);
    Serial.println(" сек");

    int hours = currentTimeSec / 3600;
    int minutes = (currentTimeSec % 3600) / 60;
    int seconds = currentTimeSec % 60;
    
    Serial.print(" - Текущее время: ");
    if (hours < 10) Serial.print("0");
    Serial.print(hours);
    Serial.print(":");
    if (minutes < 10) Serial.print("0");
    Serial.print(minutes);
    Serial.print(":");
    if (seconds < 10) Serial.print("0");
    Serial.println(seconds);

    if (abs(currentTimeSec - schedules[i].startTime) <= 300) {
      float temp = dht.readTemperature();
      float hum = dht.readHumidity();
      int soil = readSoilMoisture();
      
      if (!shouldSkipWatering(temp, hum, soil)) {
        OpenValve(schedules[i].duration);
        break;
      }
    }
  }
}

void checkValveState() {
  if (valveNeedsClosing && valveDuration > 0 && millis() - valveOpenTime >= valveDuration * 1000) {
    CloseValve();
  }
}

void loop() {
  reconnectWiFi();
  
  if (millis() - lastCheckTime > checkInterval) {
    checkManualValveControl();
    checkScheduledValveControl();
    checkValveState();
    
    int controlValue;
    ThingSpeakReadRequest request = {FIELD_CONTROL, &controlValue};
    if (readMultipleThingSpeakFields(&request, 1, boardModeChannelID, boardModeReadAPIKey)) {
      if (controlValue >= 0) {
        CalibrationState newState = static_cast<CalibrationState>(controlValue);
        if (newState != currentState) {
          currentState = newState;
          handleStateChange();
        }
      }
    }
    lastCheckTime = millis();
  }

  if(currentState == NORMAL_MODE){
    float temp = dht.readTemperature();
    float hum = dht.readHumidity();
    int soil = readSoilMoisture();
    
    displaySensorData(temp, hum, soil);
    
    if (millis() - lastUpdateTime > updateInterval) {
      ThingSpeakData sensorData[] = {
        {FIELD_TEMP, String(temp)},
        {FIELD_HUMIDITY, String(hum)},
        {FIELD_SOIL, String(soil)},
      };
      sendToThingSpeak(sensorData, 3, sensorDataChannelID, sensorDataWriteAPIKey);
      lastUpdateTime = millis();
    }
  }
  
  delay(2000);
}