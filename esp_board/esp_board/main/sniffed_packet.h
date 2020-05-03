#ifndef __SNIFFED_PACKET_H__
#define __SNIFFED_PACKET_H__

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
#include <iostream>

class SniffedPacket
{
    public:
        const size_t MAC_ADDRESS_BYTES = 6;
        const size_t RSSI_BYTES  = 2; //da 1 byte lo estendo a 2 per poi fare la htons
        const size_t SSID_LENGTH_BYTES = 2;
        const size_t SSID_BYTES = 33; // 32 + \0
        const size_t TIMESTAMP_BYTES = 4;
        const size_t CHECKSUM_BYTES = 4;

        SniffedPacket(uint8_t* mac_addr, uint8_t ssidLen, uint8_t* ssid, int16_t rssi, uint32_t timestamp, uint8_t* checksum);

        SniffedPacket(const wifi_promiscuous_pkt_t* wifi_pkt);
        
        std::string toString() const;

        int send(int socket) const;

        static int sendSniffedPacket(const SniffedPacket& packet, int socket);
        
    private:
        uint8_t mac_addr[6];
        uint8_t ssid_len;
        uint8_t ssid[33];
        int16_t rssi;
        uint32_t timestamp;
        uint8_t checksum[4];

        void setSSID_(const wifi_promiscuous_pkt_t* pkt);
        void setMAC_(const wifi_promiscuous_pkt_t* pkt);
        uint32_t getTimestamp_();
};

#endif