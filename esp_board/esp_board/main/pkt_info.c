#include "pkt_info.h"

pkt_info pkt_info_init_fromPkt(const wifi_promiscuous_pkt_t* pkt){
    pkt_info res;
    struct timeval tval;

    extract_mac_src((void*)pkt->payload, res.mac_addr);
    res.rssi = pkt->rx_ctrl.rssi;
    extract_ssid((void*)pkt->payload, &res);

    if(gettimeofday(&tval, NULL) != 0){
        printf("Error on gettimeofday\n");
    }
    res.timestamp = tval.tv_sec;

    memcpy(res.checksum, pkt->payload + pkt->rx_ctrl.sig_len - 4, 4);
    return res;
}

pkt_info pkt_info_init(uint8_t* mac_addr, signed rssi, int ssidLen, char* ssid, uint32_t timestamp, uint8_t* checksum){
    pkt_info pkt;

    memcpy(&(pkt.mac_addr), mac_addr, 6);
    pkt.rssi = rssi;
    memcpy(&(pkt.ssid), ssid, ssidLen);
    pkt.timestamp = timestamp;
    memcpy(&(pkt.checksum), checksum, 4);
    pkt.len = ssidLen;
    pkt.ssid[ssidLen] = '\0';

    return pkt;
}

void pkt_info_display(pkt_info* pkt){
    if(pkt == NULL){
        printf("pkt received is NULL\n");
        return;
    }

    printf("MAC_DST: %02x:%02x:%02x:%02x:%02x:%02x SSID_LEN:%d SSID:%20s RSSI: %02d TIMESTAMP: %u FCS:%02x%02x%02x%02x\n",
            pkt->mac_addr[0],
            pkt->mac_addr[1],
            pkt->mac_addr[2],
            pkt->mac_addr[3],
            pkt->mac_addr[4],
            pkt->mac_addr[5],
            pkt->len,
            pkt->ssid,
            pkt->rssi,
            pkt->timestamp,
            pkt->checksum[0],
            pkt->checksum[1],
            pkt->checksum[2],
            pkt->checksum[3]);

    return;
}

int pkt_send(int socket, pkt_info* pkt){
    if(socket <= 0)
        return INVALID_SOCKET;

    if(pkt == NULL)
        return INVALID_PKT;

    uint8_t buffer[MAC_LEN + RSSI_LEN + SSIDLEN_LEN + SSID_LEN + TIMESTAMP_LEN + CHECKSUM_LEN];
    uint8_t *buf_ptr = buffer;
    uint16_t rssi = pkt->rssi;
    uint16_t ssid_len = pkt->len;
    uint16_t tmp1;
    uint32_t tmp2;

    //pkt_info_display(pkt);
    //aggiungo mac_addr nel buffer
    printf("new pkt sent__________");
    memcpy(buf_ptr, pkt->mac_addr, MAC_LEN);
    buf_ptr += MAC_LEN;
    printf("MAC_DST: %02x:%02x:%02x:%02x:%02x:%02x\n", buffer[0], buffer[1], buffer[2], buffer[3], buffer[4], buffer[5]);


    //aggiungo rssi nel buffer
    tmp1 = htons(rssi);
    memcpy(buf_ptr, &tmp1, sizeof(uint16_t));
    buf_ptr += sizeof(uint16_t);
    printf("rssi reverse: %02d\n", (int16_t) buffer[6]);

    //NON trasmetto il terminatore
    tmp1 = htons(ssid_len);
    memcpy(buf_ptr, &tmp1, sizeof(uint16_t));
    buf_ptr += sizeof(uint16_t);
    printf("ssid_len reverse: %02d\n", (int16_t) buffer[8]);

    //copio l'ssid nel buffer se la sua lunghezza è != 0
    if(ssid_len != 0){
        memcpy(buf_ptr, pkt->ssid, ssid_len);
        buf_ptr += ssid_len;
        *buf_ptr = 0;
        printf("ssid: %s\n", (char*) &(buffer[10]));
    }

    //copio timestamp nel buffer
    tmp2 = htonl((uint32_t) pkt->timestamp);
    memcpy(buf_ptr, &tmp2, sizeof(uint32_t));
    buf_ptr += sizeof(uint32_t);
    printf("timestamp reverse: %u\n", (int16_t) buffer[10 + ssid_len]);

    //copio il checksum nel buffer
    memcpy(buf_ptr, pkt->checksum, CHECKSUM_LEN);
    buf_ptr += CHECKSUM_LEN;
    printf("FCS:%02x%02x%02x%02x\n", buffer[10 + ssid_len + 2], buffer[10 + ssid_len + 3], buffer[10 + ssid_len + 4], buffer[10 + ssid_len + 5]);

    //printf("ssid len: %d - %s - ", ssid_len, pkt->ssid);
    //char tmpStampa[200];
    //memcpy(tmpStampa, buffer, (size_t) (buf_ptr - buffer));
    //tmpStampa[buf_ptr - buffer + 1] = '\0'; 
    //printf("%s\n", tmpStampa);

    //invio i dati sul socket connesso
    ssize_t result = send(socket, (void*) buffer, (size_t) (buf_ptr - buffer), 0);

    if(errno == EPIPE){
        return SOCKET_CLOSED;
    }

    if(result != (size_t) (buf_ptr - buffer)){
        return SEND_ERROR;
    }

    return (int) result;
}

void extract_ssid(void* payload, pkt_info* dst){
    uint8_t len = 0;
    memcpy(&len, payload + 25, sizeof(len));

    #ifdef DEBUG
    printf("%02x\n", len);
    #endif

    if(len != 0)
        memcpy(dst->ssid, payload + 26, len);
    dst->ssid[len] = '\0';
    dst->len = len;
    return;
}

void extract_mac_src(void* payload, uint8_t* buf){
    if(payload == NULL || buf == NULL){
        return;
    }

    memcpy(buf, payload + 10, 6);
    return;
}