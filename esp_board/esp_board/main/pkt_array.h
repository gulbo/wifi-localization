#ifndef PKT_ARRAY_H
#define PKT_ARRAY_H

#include "freertos/FreeRTOS.h"
#include "esp_wifi.h"
#include "esp_wifi_types.h"
#include "string.h"
#include "pkt_info.h"
#include "sys/types.h"
#include "sys/socket.h"
#include "sys/time.h"
#include "netinet/in.h"
#include "unistd.h"
#include "errors.h"
#define MAX_PKT 1000

typedef int (*proc_pkt)(pkt_info* pkt, void* ctx);

extern pkt_info pkt_array[MAX_PKT];
extern int head;

void pkt_array_init();
int insert_pkt(const wifi_promiscuous_pkt_t* pkt);
int array_process(proc_pkt func, void* ctx);
int array_send(int socket);

#endif