#include "mac_manager.h"
#include "pkt_info.h"

uint8_t mac_stored[MAX_MAC][6];
uint8_t flag_list[MAX_MAC];
int nmacs;

void init_mac_structures(){
    for(int i = 0; i < MAX_MAC; i++){
        for(int j = 0; j < 6; j++){
            mac_stored[i][j] = 0;
        }
    }

    for(int i = 0; i < MAX_MAC; i++){
        flag_list[i] = 0;
    }
}

int countEspFound(void){
    int count = 0;
    for(int i = 0; i < nmacs; i++){
        if(flag_list[i])
            count++;
    }

    return count;
}

int cmp_mac(const wifi_promiscuous_pkt_t* pkt, uint8_t *mac){
    uint8_t pkt_mac[6];

    extract_mac_src((void*) pkt->payload, pkt_mac);    
    for(int i = 0; i < 6; i++){
        if(mac[i] != pkt_mac[i])
            return 0;
    }
    return 1;
}

int recvmac(int s){
    int res;

    if(s < 0){
        printf("Invalid socket received\n");
        return INVALID_SOCKET;
    }

    printf("nmacs to receive: %d\n", nmacs);

    for(int i = 0; i < nmacs; i++){
        res = recv(s, mac_stored[i], 6, 0);
        if(res != 6){
            printf("Error on reading MAc Address from socket: read %d B\n", res);
            return RECV_ERROR;
        }
        printf("Received %d mac\n", i + 1);
    }
    printf("Stored all the macs\n");
    return 0;
}

void resetFlagList(){
    for(int i = 0; i < MAX_MAC; i++){
        flag_list[i] = 0;
    }
}