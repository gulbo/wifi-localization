#ifndef WIFI_H
#define WIFI_H

#include "freertos/FreeRTOS.h"
#include "freertos/event_groups.h"
#include "esp_wifi.h"
#include "esp_wifi_types.h"
#include "esp_system.h"
#include "esp_event.h"

void initializeWifi(const char* wifi_ssid, const char* wifi_password, uint8_t wifi_channel);
void wifiSnifferNullHandler(void *buff, wifi_promiscuous_pkt_type_t type);

#endif