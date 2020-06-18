#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "freertos/event_groups.h"
#include "esp_wifi_types.h"
#include "esp_system.h"
#include "esp_event.h"
#include "driver/gpio.h"
#include "esp_wifi.h"
#include "nvs_flash.h"
#include "string.h"
#include <sys/time.h>
#include <sys/types.h>
#include "sniffed_packet.h"
#include "client.h"
#include <time.h>
#include <list>
#include <string>
#include <iostream>

#define WIFI_SSID "GULPO"
#define WIFI_PASSWORD "f117f117bagonghi"
#define WIFI_CHANNEL 1
#define SNIFFING_TIME_SEC 60
#define SERVER_IP "192.168.43.214"
#define SERVER_PORT 60006
#define BOARD_ID 2

std::list<SniffedPacket> sniffed_packets;
Client client{};
const TickType_t one_second_delay = 1000 / portTICK_PERIOD_MS;
EventGroupHandle_t wifi_event_group;

/**
 *  @brief empty callback for packet sniffing
 */
void wifiSnifferNullHandler(void *buf, wifi_promiscuous_pkt_type_t type){
    return;
}

/**
 *  @brief callback for every packet sniffed
 *  Inserts the packet into the sniffed_packets
 */
void wifiSnifferHandler(void *buf, wifi_promiscuous_pkt_type_t type){
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
 * @brief callback for wifi events handling
 * @note thanks to github.com/lucadentella/esp32-tutorial
 */
static esp_err_t event_handler(void *ctx, system_event_t *event)
{
    switch(event->event_id) {
    case SYSTEM_EVENT_STA_START:
        esp_wifi_connect();
        break;
	case SYSTEM_EVENT_STA_GOT_IP:
        xEventGroupSetBits(wifi_event_group, BIT0);
        break;
	case SYSTEM_EVENT_STA_DISCONNECTED:
		xEventGroupClearBits(wifi_event_group, BIT0);
        break;
	default:
        break;
    }
	return ESP_OK;
}

/**
 * @brief initialize wifi and connect
 * @param wifi_ssid
 * @param wifi_password
 * @param wifi_channel
 * @note thanks to github.com/lucadentella/esp32-tutorial
 */
void initializeWifi(const char* wifi_ssid, const char* wifi_password, uint8_t wifi_channel){
    // set LED OFF before connecting to wifi
    const gpio_num_t LED_GPIO =  GPIO_NUM_2;
    gpio_pad_select_gpio(LED_GPIO);
    gpio_set_direction(LED_GPIO, GPIO_MODE_OUTPUT);
    gpio_set_level(LED_GPIO, 0);
    
    static wifi_config_t wifi_config = {};
    strcpy((char*)wifi_config.sta.ssid, wifi_ssid);
    strcpy((char*)wifi_config.sta.password, wifi_password);
    std::cout << "Connecting to: " << wifi_ssid << std::endl;

    wifi_event_group = xEventGroupCreate();
    nvs_flash_init();
    tcpip_adapter_init();
    ESP_ERROR_CHECK(esp_event_loop_init(event_handler, NULL));

    wifi_init_config_t cfg = WIFI_INIT_CONFIG_DEFAULT();
    static wifi_country_t wifi_country = {.cc="IT", .schan=1, .nchan=13, .policy=WIFI_COUNTRY_POLICY_AUTO};
    ESP_ERROR_CHECK(esp_wifi_init(&cfg));
    ESP_ERROR_CHECK(esp_wifi_set_country(&wifi_country));
    ESP_ERROR_CHECK(esp_wifi_set_storage(WIFI_STORAGE_RAM));
    ESP_ERROR_CHECK(esp_wifi_set_mode(WIFI_MODE_STA));

    ESP_ERROR_CHECK(esp_wifi_set_config(ESP_IF_WIFI_STA, &wifi_config));
    ESP_ERROR_CHECK(esp_wifi_start());
    xEventGroupWaitBits(wifi_event_group, BIT0, false, true, portMAX_DELAY);

    tcpip_adapter_ip_info_t ip_info;
	ESP_ERROR_CHECK(tcpip_adapter_get_ip_info(TCPIP_ADAPTER_IF_STA, &ip_info));
    std::cout << "Connected!" << std::endl;
	std::cout << "IP Address:  " << ip4addr_ntoa(&ip_info.ip) << std::endl;
	std::cout << "Subnet mask: " << ip4addr_ntoa(&ip_info.netmask) << std::endl;
	std::cout << "Gateway:     " << ip4addr_ntoa(&ip_info.gw) << std::endl;
    
    // set LED on
    gpio_set_level(LED_GPIO, 1);

    // All packets of the currently joined 802.11 network (with a specific SSID and channel) are captured
    esp_wifi_set_promiscuous(true);
    // Each time a packet is received, the registered callback function will be called
    esp_wifi_set_promiscuous_rx_cb(&wifiSnifferNullHandler);
    esp_wifi_set_channel(wifi_channel, WIFI_SECOND_CHAN_NONE);
}

/**
 *  @brief set the time received by the server as the time of the system
 *         this is needed to synchronize all the esp boards
 *  @return success 
 */
bool setSystemTime(uint32_t epoch){
    struct timeval tval;
    tval.tv_sec = epoch;
    tval.tv_usec = 0;

    int16_t result = settimeofday(&tval, NULL);
    if(result != 0){
        return false;
    }

    return true;
}

/**
 * @brief the main task
 *  Starts sniffing for SNIFFING_TIME_SEC
 *  At the end sends the sniffed packets and starts sniffing again
 *  @param parameters input parameters, not used
 */
void sendTask(void *parameters){
    // every SNIFFING_TIME_SEC send:
    // --> id_board
    // --> array of devices found
    // then receive
    // <-- time from server
    while(true){
        // start sniffing
        std::cout << "New sniffing phase: starting data acquisition" << std::endl;
        esp_wifi_set_promiscuous_rx_cb(&wifiSnifferHandler);
        
        // ping while sniffing
        for (uint32_t ping = 1; ping <= SNIFFING_TIME_SEC; ping++)
        {
            vTaskDelay(one_second_delay);
            if (!client.sendInt(ping))
            {
                std::cout << "Error sending  ping" << std::endl;
                exit(1);;
            }
        }

        std::cout << "New send phase: interrupting data acquisition" << std::endl;
        esp_wifi_set_promiscuous_rx_cb(&wifiSnifferNullHandler);

        client.sendPackets(sniffed_packets);

        std::cout << "Waiting to receive time from server..." << std::endl;
        bool is_time_set = false;
        uint32_t epoch;
        if (client.readInt(epoch))
        {
            if(setSystemTime(epoch))
                is_time_set = true;
        }   

        if (is_time_set)
            std::cout << "Time set: " << std::to_string(epoch) << std::endl; 
        else
            std::cout << "Error setting the time. Continuing using last time received" << std::endl;
    }
}

void initializeClient()
{
    client = Client(BOARD_ID, SERVER_IP, SERVER_PORT);

    // try to connect to the server
    for(uint16_t tries = 1; tries <= Client::MAX_CONNECTION_RETRIES && !client.isConnected(); tries++)
    {
        std::cout << "Trying to connect: " << std::to_string(tries) << std::endl;
        client.connect();
    }
    
    if(!client.isConnected())
    {
        std::cout << "Failed to connect to the server" << std::endl;
        exit(1);
    }

    std::cout << "Connected to the server!" << std::endl;

    std::cout << "Starting client..." << std::endl;

    uint32_t epoch;
    try
    {
        epoch = client.start();
    }
    catch(const std::runtime_error& e)
    {
        std::cout << e.what() << std::endl;
        exit(1);
    }
    
    if(!setSystemTime(epoch))
        throw std::runtime_error("Error setting the first time from the server");

    std::cout << "Client Started!" << std::endl;
}

int main(){
    std::cout << "Initialize WIFI" << std::endl;
    initializeWifi(WIFI_SSID, WIFI_PASSWORD, WIFI_CHANNEL);

    std::cout << "Initialize Client" << std::endl;
    initializeClient();

    std::cout << "Create sniffing task" << std::endl;
    // create and starts sendTask
    TaskHandle_t send_task_handle;
    BaseType_t xReturned = xTaskCreate(&sendTask, "sendTask", 50 * 1024 /*stacksize*/, nullptr, 1 /*priority*/, &send_task_handle);
    if(xReturned != pdPASS){
        std::cout << "Error creating sendTask!" << std::endl;
        exit(1);
    }
}

extern "C" void app_main(void){
    main();
}