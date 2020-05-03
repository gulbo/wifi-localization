#ifndef __SNIFFED_PACKET_H__
#define __SNIFFED_PACKET_H__

#include "esp_wifi_types.h"
#include "sys/socket.h"
#include "errors.h"
#include <iostream>
#include <string>

class SniffedPacket
{
    public:
        /** size constants **/
        const size_t MAC_ADDRESS_BYTES = 6;
        const size_t RSSI_BYTES  = 2; //da 1 byte lo estendo a 2 per poi fare la htons
        const size_t SSID_LENGTH_BYTES = 2;
        const size_t SSID_BYTES = 33; // 32 + \0
        const size_t TIMESTAMP_BYTES = 4;
        const size_t CHECKSUM_BYTES = 4;

        /**
         * @brief create a SniffedPacket passing the parameters
         */
        SniffedPacket(uint8_t* mac_address,
                      uint8_t ssid_length,
                      uint8_t* ssid,
                      int16_t rssi,
                      uint32_t timestamp,
                      uint8_t* checksum);

        /**
         * @brief create a SniffedPacket from an esp32 packet
         * @param wifi_pkt esp32 packet pointer
         */
        SniffedPacket(const wifi_promiscuous_pkt_t* wifi_pkt);
        
        /**
         * @brief convert the packet to printable format
         * @return
         */
        std::string toString() const;
        
        /**
         * @brief send the packet over a socket
         * @param socket
         */
        int send(int socket) const;

        /**
         * @brief send a given packet over a socket
         * @param packet
         * @param socket
         */
        static int sendSniffedPacket(const SniffedPacket& packet, int socket);
        
    private:
        uint8_t mac_address[6];
        uint8_t ssid_length;
        uint8_t ssid[33];
        int16_t rssi;
        uint32_t timestamp;
        uint8_t checksum[4];

        /** 
         * @brief set the SSID starting from a raw packet pointer
         */
        void setSSID_(const wifi_promiscuous_pkt_t* pkt);

        /** 
         * @brief set the MAC address starting from a raw packet pointer
         */
        void setMAC_(const wifi_promiscuous_pkt_t* pkt);

        /**
         * @brief get the current epoch in seconds
         */
        uint32_t getTimestamp_();
};

#endif