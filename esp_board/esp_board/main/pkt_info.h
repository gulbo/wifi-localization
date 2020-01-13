#ifndef PKT_INFO_H
#define PKT_INFO_H

#include "freertos/FreeRTOS.h"
#include "esp_wifi.h"
#include "esp_wifi_types.h"
#include "string.h"
#include "sys/types.h"
#include "sys/socket.h"
#include "sys/time.h"
#include "netinet/in.h"
#include "unistd.h"
#include "errors.h"

#define MAC_LEN (6)
#define RSSI_LEN (2) //da 1 byte lo estendo a 2 per poi fare la htons
#define SSIDLEN_LEN (2)
#define SSID_LEN (33)
#define TIMESTAMP_LEN (4)
#define CHECKSUM_LEN (4)

typedef struct {
    uint8_t mac_addr[6];
    signed rssi;
    uint16_t len;
    char ssid[33];
    unsigned timestamp;
    uint8_t checksum[4];
} pkt_info;

pkt_info pkt_info_init_fromPkt(const wifi_promiscuous_pkt_t* pkt);
pkt_info pkt_info_init(uint8_t* mac_addr, signed rssi, int ssidLen, char* ssid, uint32_t timestamp, uint8_t* checksum);
void pkt_info_display(pkt_info* pkt);
int pkt_send(int socket, pkt_info* pkt);
void extract_ssid(void* payload, pkt_info* dst);
void extract_mac_src(void* payload, uint8_t* buf);

#endif