// #ifndef MAC_MANAGER_H
// #define MAC_MANAGER_H

// #include "freertos/FreeRTOS.h"
// #include "esp_wifi.h"
// #include "esp_wifi_types.h"
// #include "string.h"
// #include "sys/types.h"
// #include "sys/socket.h"
// #include "sys/time.h"
// #include "netinet/in.h"
// #include "unistd.h"
// #include "errors.h"

// #define MAX_MAC (15)

// #if defined (__cplusplus)
// extern "C" {
// #endif

// typedef uint8_t** mac_list;
// typedef uint8_t* macflag_list;

// extern uint8_t mac_stored[MAX_MAC][6];
// extern uint8_t flag_list[MAX_MAC];
// extern int nmacs;

// void init_mac_structures();
// int countEspFound(void);
// int cmp_mac(const wifi_promiscuous_pkt_t* pkt, uint8_t *mac);
// int recvmac(int s);
// void resetFlagList();

// #if defined (__cplusplus)
// }
// #endif

// #endif