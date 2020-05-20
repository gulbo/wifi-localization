#include "wifi.h"
#include "nvs_flash.h"

static wifi_country_t wifi_country = {.cc="IT", .schan=1, .nchan=13, .policy=WIFI_COUNTRY_POLICY_AUTO};
static const int CONNECTED_BIT = BIT0;

static esp_err_t event_handler(void *ctx, system_event_t *event);

void wifi_config(){
    wifi_config_t wifi_config = {
        .sta = {
            .ssid = SSID,
            .password = PASSWORD,
        },
    };
    printf("creating event group for wifi\n");
    wifi_event_group = xEventGroupCreate();
    
    nvs_flash_init();
    tcpip_adapter_init();
    ESP_ERROR_CHECK(esp_event_loop_init(event_handler, NULL));

    wifi_init_config_t cfg = WIFI_INIT_CONFIG_DEFAULT();
    ESP_ERROR_CHECK(esp_wifi_init(&cfg));
    ESP_ERROR_CHECK(esp_wifi_set_country(&wifi_country));
    ESP_ERROR_CHECK(esp_wifi_set_storage(WIFI_STORAGE_RAM));
    ESP_ERROR_CHECK(esp_wifi_set_mode(WIFI_MODE_STA));

    ESP_ERROR_CHECK(esp_wifi_set_config(ESP_IF_WIFI_STA, &wifi_config));
    printf("invoking esp_wifi_start\n");
    ESP_ERROR_CHECK(esp_wifi_start());

    printf("waiting wifi event groups\n");
    xEventGroupWaitBits(wifi_event_group, CONNECTED_BIT, false, true, portMAX_DELAY);

    tcpip_adapter_ip_info_t ip_info;
	ESP_ERROR_CHECK(tcpip_adapter_get_ip_info(TCPIP_ADAPTER_IF_STA, &ip_info));
	printf("IP Address:  %s\n", ip4addr_ntoa(&ip_info.ip));
	printf("Subnet mask: %s\n", ip4addr_ntoa(&ip_info.netmask));
	printf("Gateway:     %s\n", ip4addr_ntoa(&ip_info.gw));

    // All packets of the currently joined 802.11 network (with a specific SSID and channel) are captured
    esp_wifi_set_promiscuous(true);
    // Each time a packet is received, the registered callback function will be called
    esp_wifi_set_promiscuous_rx_cb(&wifi_sniffer_nullhandler);
}

void wifi_sniffer_nullhandler(void *buff, wifi_promiscuous_pkt_type_t type){
    return;
}

void wifi_sniffer_set_channel(uint8_t channel)
{
	esp_wifi_set_channel(channel, WIFI_SECOND_CHAN_NONE);
}

static esp_err_t event_handler(void *ctx, system_event_t *event)
{
    switch(event->event_id) {
    case SYSTEM_EVENT_STA_START:
        esp_wifi_connect();
        break;
	case SYSTEM_EVENT_STA_GOT_IP:
        xEventGroupSetBits(wifi_event_group, CONNECTED_BIT);
        break;
	case SYSTEM_EVENT_STA_DISCONNECTED:
		xEventGroupClearBits(wifi_event_group, CONNECTED_BIT);
        break;
	default:
        break;
    }
	return ESP_OK;
}