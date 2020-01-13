#include "pkt_array.h"

pkt_info pkt_array[MAX_PKT];
int head;

void pkt_array_init(){
    head = 0;
}

int insert_pkt(const wifi_promiscuous_pkt_t* pkt){
    if(head >= MAX_PKT){
        return -1;
    }

    pkt_array[head] = pkt_info_init_fromPkt(pkt);
    head++;
    return head;
}

int array_process(proc_pkt func, void* ctx){
    for(int i = 0; i < head; i++){
        func(&(pkt_array[i]), ctx);
    }

    return 0;
}

int array_send(int socket){
    int res = 0;

    if(socket <= 0){
        printf("Invalid socket (%d)\n", socket);
        return INVALID_SOCKET;
    }

    res = ntohl(head);
    res = send(socket, (void*)&res, sizeof(res), 0);

    for(int i = 0; i < head; i++){
        res = pkt_send(socket, &(pkt_array[i]));

        if(res < 0){
            printf("Packet at index %d returned %d while sending\n", i, res);
        }
    }

    return 0;
}