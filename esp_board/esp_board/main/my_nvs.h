#ifndef MY_NVS_H
#define MY_NVS_H

#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "nvs_flash.h"
#include "string.h"
#include "errors.h"

#define ID_KEY "id"
#define IP_KEY "server_ip"
#define PORT_KEY "port"

#if defined (__cplusplus)
extern "C" {
#endif

int my_nvs_init(nvs_handle *handle);
int readnvs_i32(nvs_handle handle, char* key);
char* readnvs_str(nvs_handle handle, char* key, char* buffer, size_t maxlen);

#if defined (__cplusplus)
}
#endif

#endif