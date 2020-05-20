/*
    Progetto PDS a.a. 2017/2018
*/

#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "freertos/event_groups.h"
#include "esp_wifi_types.h"
#include "esp_system.h"
#include "esp_event.h"
#include "esp_event_loop.h"
#include "driver/gpio.h"
#include "string.h"
#include "wifi.h"
#include <sys/time.h>
#include <sys/types.h>
#include "sniffed_packet.h"
#include "client.h"
#include <time.h>
#include <list>
#include <string>
#include <iostream>

#define SNIFFER_CHANNEL (1)
#define STACK_SIZE (50 * 1024)
#define SNIFFING_TIME_SEC (60)

#define SERVER_IP "10.0.0.78"
#define SERVER_PORT 7999
#define BOARD_ID 1

std::list<SniffedPacket> sniffed_packets;
Client client(BOARD_ID, SERVER_IP, SERVER_PORT);

static void config();
void createTask();
void wifi_sniffer_handler(void *buff, wifi_promiscuous_pkt_type_t type);
void sendTask(void *parameters);
void print_task_state(eTaskState state);
bool setTime(uint32_t epoch);

TaskHandle_t send_task_handle;
const TickType_t send_task_delay = SNIFFING_TIME_SEC * 1000 / portTICK_PERIOD_MS;

static void config(){
    printf("initializing tcip adapter and wifi config\n");
    wifi_config();

    BaseType_t xReturned;
    int x = 0;
    printf("Creating tasks...\n");
    // create and starts sendTask
    // TODO PROBABLY WE CAN REMOVE THE x PARAMETER PASSED
    xReturned = xTaskCreate(&sendTask, "sendTask", STACK_SIZE, &x, 1 /*priority*/, &send_task_handle);
    if(xReturned != pdPASS){
        printf("Could not allocate required memory to create sendTask!\n");
        exit(1);
    }
    
    eTaskState state = eTaskGetState(send_task_handle);
    printf("sendTask: ");
    print_task_state(state);
    return;
}

/**
 *  @brief callback for every packet sniffed
 *  Inserts the packet into the sniffed_packets
 */
void wifi_sniffer_handler(void *buf, wifi_promiscuous_pkt_type_t type){
    // parse only valid packets
    if (buf == nullptr) 
        return;

    // Check that it is a management frame
    // Management frames are used by stations to join and leave a BSS (Basic Service Sets)
    if (type != WIFI_PKT_MGMT)
		return;

	const wifi_promiscuous_pkt_t* wifi_pkt = (wifi_promiscuous_pkt_t*) buf;
    
    // check that the SubType is "Reassociation response"
    uint16_t frameControl = ((uint16_t)wifi_pkt->payload[1] << 8) + wifi_pkt->payload[0];
    uint8_t frameSubType = (frameControl & 0b0000000011110000) >> 4;
    if(frameSubType != 4)
        return;
    
    // add packet to the sniffed packets
    sniffed_packets.emplace_back(wifi_pkt);
    std::cout << sniffed_packets.back().toString() << std::endl;
}

/**
 * @brief the main task
 *  First initialize the protocol
 *  Then starts sniffing for send_task_delay
 *  At the end sends the sniffed packets and starts sniffing again
 *  @param parameters input parameters, not used
 */
void sendTask(void *parameters){
    // doesn't start until the config is conluded
    ulTaskNotifyTake(pdTRUE, portMAX_DELAY);

    // try to connect to the server
    for(uint16_t tries = 1; tries <= Client::MAX_CONNECTION_RETRIES && !client.isConnected(); tries++){

        std::cout << "Trying to connect: " << std::to_string(tries) << std::endl;
        client.connect();
    }
    
    if(!client.isConnected()){
        std::cout << "Failed to connect to the server" << std::endl;
        exit(1);
    }

    std::cout << "Connected to the server!" << std::endl;

    std::cout << "Starting client..." << std::endl;
    uint32_t epoch = client.start();
    if(!setTime(epoch))
        throw std::runtime_error("Error setting the first time from the server");
    std::cout << "Client Started!" << std::endl;

    // every send_task_delay send:
    // --> id_board
    // --> array of devices found
    // then receive
    // <-- time from server
    while(true){
        // start sniffing
        std::cout << "New sniffing phase: starting data acquisition" << std::endl;
        esp_wifi_set_promiscuous_rx_cb(&wifi_sniffer_handler);
        
        // sleep for sniffing
        vTaskDelay(send_task_delay);

        std::cout << "New send phase: interrupting data acquisition" << std::endl;
        esp_wifi_set_promiscuous_rx_cb(&wifi_sniffer_nullhandler);

        if (!client.sendBoardID())
        {
            std::cout << "Error sending Board ID" << std::endl;
            exit(1);
        }

        client.sendPackets(sniffed_packets);

        std::cout << "Waiting to receive time from server..." << std::endl;
        bool is_time_set = false;
        uint32_t epoch;
        if (client.readInt(epoch))
        {
            if(setTime(epoch))
                is_time_set = true;
        }   

        if (is_time_set)
            std::cout << "Time set: " << std::to_string(epoch) << std::endl; 
        else
            std::cout << "Error setting the time. Continuing using last time received" << std::endl;
    }
}

void print_task_state(eTaskState state){
    switch(state){
        case eReady:
            printf("task is in Ready state\n");
        break;
        case eRunning:
            printf("task is in running state\n");
        break;
        case eBlocked:
            printf("task is in blocked state\n");
        break;
        case eSuspended:
            printf("task is in suspended state\n");
        break;
        case eDeleted:
            printf("task is in deleted state\n");
        break;
    }
}

/**
 *  @brief set the time received by the server as the time of the system
 *         this is needed to synchronize all the esp boards
 */
bool setTime(uint32_t epoch){
    struct timeval tval;
    tval.tv_sec = epoch;
    tval.tv_usec = 0;

    int16_t result = settimeofday(&tval, NULL);
    if(result != 0){
        return false;
    }

    return true;
}

int main(){
    send_task_handle = NULL;

    config();
    wifi_sniffer_set_channel(SNIFFER_CHANNEL);

    // notifies sendTask that the configuration has terminated
    // it won't start before this
    xTaskNotifyGive(send_task_handle);
}

extern "C" void app_main(void){
    main();
}