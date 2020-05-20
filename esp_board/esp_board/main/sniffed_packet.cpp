#include "sniffed_packet.h"
#include <sstream>
#include <chrono>
#include <cstring> //memcpy

SniffedPacket::SniffedPacket(const wifi_promiscuous_pkt_t* wifi_pkt){
    setMAC_(wifi_pkt);
    setSSID_(wifi_pkt);
    rssi = wifi_pkt->rx_ctrl.rssi;
    timestamp = getTimestamp_();
    memcpy(checksum, wifi_pkt->payload + wifi_pkt->rx_ctrl.sig_len - 4, 4);
}

SniffedPacket::SniffedPacket(uint8_t* mac_address,
                             uint8_t ssid_length,
                             uint8_t* ssid,
                             int16_t rssi,
                             uint32_t timestamp,
                             uint8_t* checksum){
    memcpy(&(this->mac_address), mac_address, 6);
    this->rssi = rssi;
    memcpy(&(this->ssid), ssid, ssid_length);
    this->timestamp = timestamp;
    memcpy(&(this->checksum), checksum, 4);
    this->ssid_length = ssid_length;
    this->ssid[ssid_length] = '\0';
}

std::string SniffedPacket::toString() const{
    std::stringstream ss;
    char mac[18];
    snprintf(mac, 18, "%02x%02x%02x%02x%02x%02x", mac_address[0],
                                                  mac_address[1],
                                                  mac_address[2],
                                                  mac_address[3],
                                                  mac_address[4],
                                                  mac_address[5]);
    char cksum[12];  
    snprintf(cksum, 12, "%02x%02x%02x%02x", checksum[0],
                                            checksum[1],
                                            checksum[2],
                                            checksum[3]);
    ss << "MAC:" << mac << ",";
    ss << "SSID_LENGTH:" << std::to_string(ssid_length) << ",";
    ss << "SSID:" << ssid << ",";
    ss << "RSSI:" << rssi << ",";
    ss << "TIMESTAMP:" << timestamp << ",";
    ss << "CHECKSUM:" << cksum;
    return ss.str();
}

void SniffedPacket::setSSID_(const wifi_promiscuous_pkt_t* pkt){
    void* payload = (void*)(pkt->payload);
    memcpy(&ssid_length, payload + 25, sizeof(ssid_length));

    if(ssid_length != 0)
        memcpy(ssid, payload + 26, ssid_length);

    ssid[ssid_length] = '\0';
}

void SniffedPacket::setMAC_(const wifi_promiscuous_pkt_t* pkt){
    memcpy(mac_address, (void*)(pkt->payload) + 10, 6);
}

uint32_t SniffedPacket::getTimestamp_()
{
    const auto epoch = std::chrono::system_clock::now().time_since_epoch();
    const auto seconds = std::chrono::duration_cast<std::chrono::seconds>(epoch);
    return seconds.count();
}
