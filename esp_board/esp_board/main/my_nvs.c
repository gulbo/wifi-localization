#include "my_nvs.h"

int my_nvs_init(nvs_handle *handle){
    esp_err_t err = nvs_flash_init();
    fflush(stdout);
    if(err == ESP_ERR_NVS_NO_FREE_PAGES){
        printf("No free pages... trying to erase the partition...\n");
        fflush(stdout);

        const esp_partition_t* nvs_partition = esp_partition_find_first(ESP_PARTITION_TYPE_DATA, ESP_PARTITION_SUBTYPE_DATA_NVS, NULL);
        if(!nvs_partition){
            printf("NO NVS PARTITION FOUND!\n");
            fflush(stdout);
            return NVS_ERROR;
        }

        err = esp_partition_erase_range(nvs_partition, 0, nvs_partition->size);
        if(err != ESP_OK){
            printf("ERROR ON ERASE: unable to erase the partition\n");
            fflush(stdout);
            return NVS_ERROR;
        }

        //initialize the partition
        err = nvs_flash_init();
        if(err != ESP_OK){
            printf("FATAL ERROR: unable to open NVS!\n");
            fflush(stdout);
            return NVS_ERROR;
        }
    }
    err = nvs_open("storage", NVS_READWRITE, handle);
    if(err != ESP_OK){
        printf("FATAL ERROR: unable to open nvs\n");
        fflush(stdout);
        return NVS_ERROR;
    }
    printf("NVS opened\n");
    fflush(stdout);

    return 0;
}

int readnvs_i32(nvs_handle handle, char* key){
    int32_t id = -1;
    esp_err_t err = nvs_get_i32(handle, key, &id);

    if(err != ESP_OK){
        if(err == ESP_ERR_NVS_NOT_FOUND){
            printf("\nkey %s not found\n", ID_KEY);
        }
        else{
            printf("\nerror on nvs_get_i32 on key %s (%04X)\n", ID_KEY, err);
        }
        return NVS_ERROR;
    }

    return id;
}

char* readnvs_str(nvs_handle handle, char* key, char* buffer, size_t maxlen){
    esp_err_t err = nvs_get_str(handle, IP_KEY, buffer, &maxlen);
    if(err != ESP_OK){
        if(err == ESP_ERR_NVS_NOT_FOUND){
            printf("\nkey %s not found\n", IP_KEY);
        }
        else{
            printf("\nerror on nvs_get_str on key %s (%04X)\n", IP_KEY, err);
        }
        return NULL;
    }

    return buffer;
}