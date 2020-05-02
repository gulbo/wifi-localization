#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <string.h>

#include "freertos/FreeRTOS.h"
#include "freertos/task.h"

#include "esp_partition.h"
#include "esp_err.h"
#include "nvs_flash.h"
#include "nvs.h"

#define MAX_LEN (100)
#define ID_KEY "id"
#define IP_KEY "server_ip"
#define PORT_KEY "port"

/** USAGE:
 * init_config ID SERVER PORT
 * ex. init_config 1 10.0.0.78 80
 * 
 * 
 */

nvs_handle my_handle;

void parse_command(char* command){
    char* token;
    token = strtok(command, " ");

    if(!token){
        printf("\nNo command found!\n");
        return;
    }

    if(strcmp(token, "init_config") == 0){
        int32_t id = -1;
        int32_t port = -1;

        char *param1 = strtok(NULL, " ");
        char *param2 = strtok(NULL, " ");
        char *param3 = strtok(NULL, " ");
        if(!param1 || !param2 || !param3){
            printf("\ninvalid parameters\n");
            return;
        }

        id = atoi(param1);
        port = atoi(param3);

        esp_err_t err = nvs_set_i32(my_handle, ID_KEY, id);
        if(err != ESP_OK){
            printf("\nerror on id nvs_set_i32 (%04X)\n", err);
            return;
        }
        
        err = nvs_set_str(my_handle, IP_KEY, param2);
        if(err != ESP_OK){
            printf("\nerror on ip nvs_set_str (%04X)\n", err);
            return;
        }

        err = nvs_set_i32(my_handle, PORT_KEY, port);
        if(err != ESP_OK){
            printf("\nerror on port nvs_set_i32 (%04X)\n", err);
            return;
        }

        err = nvs_commit(my_handle);
        if(err != ESP_OK){
            printf("\nerror on commit (%04X)\n", err);
            return;
        }

        printf("\ncommit done\n");
    }
    //show current config
    else if(strcmp(token, "show_config") == 0){
        int32_t id = -1;
        int32_t port = -1;
        char ipaddr[MAX_LEN];

        esp_err_t err = nvs_get_i32(my_handle, ID_KEY, &id);
        if(err != ESP_OK){
            if(err == ESP_ERR_NVS_NOT_FOUND){
                printf("\nkey %s not found\n", ID_KEY);
            }
            else{
                printf("\nerror on nvs_get_i32 on key %s (%04X)\n", ID_KEY, err);
            }
            return;
        }
        printf("key %s is %d\n", ID_KEY, id);
        size_t ipaddr_len = (size_t) MAX_LEN;

        err = nvs_get_str(my_handle, IP_KEY, ipaddr, &ipaddr_len);
        if(err != ESP_OK){
            if(err == ESP_ERR_NVS_NOT_FOUND){
                printf("\nkey %s not found\n", IP_KEY);
            }
            else{
                printf("\nerror on nvs_get_str on key %s (%04X)\n", IP_KEY, err);
            }
            return;
        }
        printf("key %s is %s\n", IP_KEY, ipaddr);

        err = nvs_get_i32(my_handle, PORT_KEY, &port);
        if(err != ESP_OK){
            if(err == ESP_ERR_NVS_NOT_FOUND){
                printf("\nkey %s not found\n", PORT_KEY);
            }
            else{
                printf("\nerror on nvs_get_i32 on key %s (%04X)\n", PORT_KEY, err);
            }
            return;
        }
        printf("key %s is %d\n", PORT_KEY, port);
    }
    //erase command
    else if(strcmp(token, "erase") == 0){
        printf("Erasing nvs partition...\n");
        fflush(stdout);

        esp_err_t err = nvs_erase_all(my_handle);
        if(err != ESP_OK){
            printf("\nerror erasing (%04X)\n", err);
            return;
        }

        err = nvs_commit(my_handle);
        if(err != ESP_OK){
            printf("\nerror on commit (%04X)\n", err);
            return;
        }
        printf("\ndone!\n");
    }
    //GETINT command
    else if(strcmp(token, "getint") == 0){
        char *param = strtok(NULL, " ");
        if(!param){
            printf("\ninvalid parameter!\n");
            return;
        }

        int32_t value = 0;
        esp_err_t err = nvs_get_i32(my_handle, param, &value);
        if(err != ESP_OK){
            if(err == ESP_ERR_NVS_NOT_FOUND){
                printf("\nkey %s not found\n", param);
                return;
            }
            else{
                printf("\nerror on nvs_get_i32 (%04X)\n", err);
            }  
            return;  
        }
        printf("value stored in NVS for key %s is: %d\n", param, value);
    }
    //GETSTRING command
    else if(strcmp(token, "getstring") == 0){
        char *param = strtok(NULL, " ");
        if(!param){
            printf("\nInvalid parameter\n");
            return;
        }

        char s[MAX_LEN];
        size_t str_size = 0;

        esp_err_t err = nvs_get_str(my_handle, param, s, &str_size);
        if(err != ESP_OK){
            if(err == ESP_ERR_NVS_NOT_FOUND){
                printf("\nkey %s not found\n", param);
            }
            else{
                printf("\nerror on nvs_get_i32 (%04X)\n", err);
            }
            return;
        }
        printf("value stored in NVS for key %s is: %s\n", param, s);
    }
    //SETINT command
    else if(strcmp(token, "setint") == 0){
        char *param1 = strtok(NULL, " ");
        char *param2 = strtok(NULL, " ");
        if(!param1 || !param2){
            printf("\nInvalid parameters\n");
            return;
        }

        int32_t value = atoi(param2);
        esp_err_t err = nvs_set_i32(my_handle, param1, value);
        if(err != ESP_OK){
            printf("\nerror on nvs_set_i32 (%04X)\n", err);
            return;
        }
        err = nvs_commit(my_handle);
        if(err != ESP_OK){
            printf("\nerror on commit (%04X)\n", err);
            return;
        }
        printf("value %d stored in NVS with key %s\n", value, param1);
    }
    //SETSTRING command
    else if(strcmp(token, "setstring") == 0){
        char *param1 = strtok(NULL, " ");
        char *param2 = strtok(NULL, " ");

        if(!param1 || !param2){
            printf("\nInvalid parameters\n");
            return;
        }

        esp_err_t err = nvs_set_str(my_handle, param1, param2);
        if(err != ESP_OK){
            printf("\nerror on nvs_set_str (%04X)\n", err);
            return;
        }
        err = nvs_commit(my_handle);
        if(err != ESP_OK){
            printf("\nerror on commit (%04X)\n", err);
            return;
        }
        printf("value %s stored in NVS with key %s\n", param1, param2);
    }
    else{
        printf("\nUnknown command!\n");
    }
    return;
}

void main_task(void* ctx){
    char line[MAX_LEN];
    int pos = 0;

    while(1){
        int c = getchar();
        if(c < 0){
            vTaskDelay(10/portTICK_PERIOD_MS);
            continue;
        }
        if(c == '\r') continue;
        if(c == '\n'){
            line[pos] = '\0';
			printf("\n");
			fflush(stdout);
            parse_command(line);

            pos = 0;
            printf("\nesp32> ");
            fflush(stdout);
        }
        else{
            putchar(c);
            line[pos] = c;
            pos++;

            if(pos == MAX_LEN){
                pos = 0;
                printf("\nline is full\n");

                printf("\nesp32> ");
                fflush(stdout);
            }
        }
    }
}

void app_main(void){
    esp_err_t err = nvs_flash_init();
    fflush(stdout);
    if(err == ESP_ERR_NVS_NO_FREE_PAGES){
        printf("No free pages... trying to erase the partition...\n");
        fflush(stdout);

        const esp_partition_t* nvs_partition = esp_partition_find_first(ESP_PARTITION_TYPE_DATA, ESP_PARTITION_SUBTYPE_DATA_NVS, NULL);
        if(!nvs_partition){
            printf("NO NVS PARTITION FOUND!\n");
            fflush(stdout);
            while(1){
                vTaskDelay(10/portTICK_PERIOD_MS);
            }
        }

        err = esp_partition_erase_range(nvs_partition, 0, nvs_partition->size);
        if(err != ESP_OK){
            printf("ERROR ON ERASE: unable to erase the partition\n");
            fflush(stdout);
            while(1){
                vTaskDelay(10/portTICK_PERIOD_MS);
            }
        }

        //initialize the partition
        err = nvs_flash_init();
        if(err != ESP_OK){
            printf("FATAL ERROR: unable to open NVS!\n");
            fflush(stdout);
            while(1){
                vTaskDelay(10/portTICK_PERIOD_MS);
            }
        }
    }
    err = nvs_open("storage", NVS_READWRITE, &my_handle);
    if(err != ESP_OK){
        printf("FATAL ERROR: unable to open nvs\n");
        fflush(stdout);
        while(1){
                vTaskDelay(10/portTICK_PERIOD_MS);
        }
    }
    printf("NVS opened\n");
    fflush(stdout);
    xTaskCreate(&main_task, "main_task", 2048, NULL, 5, NULL);
}
