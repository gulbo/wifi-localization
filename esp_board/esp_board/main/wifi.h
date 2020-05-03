#ifndef WIFI_H
#define WIFI_H

#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "freertos/timers.h"
#include "freertos/event_groups.h"
#include "esp_wifi.h"
#include "esp_wifi_types.h"
#include "esp_system.h"
#include "esp_event.h"
#include "esp_event_loop.h"
#include "string.h"

//#define SSID "GULPO"
//#define PASSWORD "f117f117bagonghi"

#define SSID "Help! I am Trapped in a Router!!"
#define PASSWORD "sfsu1996"


#define SUBTYPE_MASK (0x00F0)
#define SUBTYPE_SHIFT (4)

#if defined (__cplusplus)
extern "C" {
#endif

EventGroupHandle_t wifi_event_group;

void wifi_config();
void wifi_sniffer_nullhandler(void *buff, wifi_promiscuous_pkt_type_t type);
void wifi_sniffer_set_channel(uint8_t channel);
const char* wifi_sniffer_packet_type2str(wifi_promiscuous_pkt_type_t type);
int check_probe_req(uint16_t frame_ctrl);

#if defined (__cplusplus)
}
#endif

#endif