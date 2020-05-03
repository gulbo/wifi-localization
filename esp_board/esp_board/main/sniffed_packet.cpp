#include "sniffed_packet.h"
#include <sstream>
#include <chrono>

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
    snprintf(cksum, 12, "%02x%02x%02x%02x", mac_address[0],
                                            mac_address[1],
                                            mac_address[2],
                                            mac_address[3]);
    ss << "MAC:" << mac << ",";
    ss << "SSID_LENGTH:" << std::to_string(ssid_length) << ",";
    ss << "SSID:" << ssid << ",";
    ss << "RSSI:" << rssi << ",";
    ss << "TIMESTAMP:" << timestamp << ",";
    ss << "CHECKSUM:" << cksum;
    return ss.str();
}

int SniffedPacket::send(int socket) const{
    if(socket <= 0)
        return INVALID_SOCKET;

    uint8_t buffer[MAC_ADDRESS_BYTES + RSSI_BYTES + SSID_LENGTH_BYTES + SSID_BYTES + TIMESTAMP_BYTES + CHECKSUM_BYTES];
    uint8_t *buf_ptr = buffer;

    //display();

    //aggiungo mac_address nel buffer
    printf("new pkt sent__________");
    memcpy(buf_ptr, mac_address, MAC_ADDRESS_BYTES);
    buf_ptr += MAC_ADDRESS_BYTES;
    printf("MAC_DST: %02x:%02x:%02x:%02x:%02x:%02x\n", buffer[0], buffer[1], buffer[2], buffer[3], buffer[4], buffer[5]);


    //aggiungo rssi nel buffer
    uint16_t network_rssi = htons(rssi); 
    memcpy(buf_ptr, &network_rssi, sizeof(uint16_t));
    //tmp1 = htons(rssi);
    //memcpy(buf_ptr, &tmp1, sizeof(uint16_t));
    buf_ptr += sizeof(uint16_t);
    printf("rssi reverse: %02d\n", (int16_t) buffer[6]);

    //NON trasmetto il terminatore
    uint16_t networkd_ssid_len = htons(ssid_length);
    memcpy(buf_ptr, &networkd_ssid_len, sizeof(uint16_t));
    // tmp1 = htons(ssid_length);
    // memcpy(buf_ptr, &tmp1, sizeof(uint16_t));
    buf_ptr += sizeof(uint16_t);
    printf("ssid_length reverse: %02d\n", (int16_t) buffer[8]);

    //copio l'ssid nel buffer se la sua lunghezza è != 0
    if(ssid_length != 0){
        memcpy(buf_ptr, ssid, ssid_length);
        buf_ptr += ssid_length;
        *buf_ptr = 0;
        printf("ssid: %s\n", (char*) &(buffer[10]));
    }

    //copio timestamp nel buffer
    uint32_t network_timestamp = htonl((uint32_t) timestamp);
    memcpy(buf_ptr, &network_timestamp, sizeof(uint32_t));
    buf_ptr += sizeof(uint32_t);
    printf("timestamp reverse: %u\n", (int16_t) buffer[10 + ssid_length]);

    //copio il checksum nel buffer
    memcpy(buf_ptr, checksum, CHECKSUM_BYTES);
    buf_ptr += CHECKSUM_BYTES;
    printf("FCS:%02x%02x%02x%02x\n", buffer[10 + ssid_length + 2], buffer[10 + ssid_length + 3], buffer[10 + ssid_length + 4], buffer[10 + ssid_length + 5]);

    //invio i dati sul socket connesso
    ssize_t result = ::send(socket, (void*) buffer, (size_t) (buf_ptr - buffer), 0);

    if(errno == EPIPE){
        return SOCKET_CLOSED;
    }

    if(result != (size_t) (buf_ptr - buffer)){
        return SEND_ERROR;
    }

    return (int) result;
}

int SniffedPacket::sendSniffedPacket(const SniffedPacket& packet, int socket){
    return packet.send(socket);
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
