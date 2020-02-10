/*
    Progetto PDS a.a. 2017/2018
*/

#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "freertos/timers.h"
#include "freertos/semphr.h"
#include "freertos/event_groups.h"
#include "esp_wifi.h"
#include "esp_wifi_types.h"
#include "esp_system.h"
#include "esp_event.h"
#include "esp_event_loop.h"
#include "nvs_flash.h"
#include "driver/gpio.h"
#include "string.h"
#include "pkt_info.h"
#include "pkt_array.h"
#include "wifi.h"
#include <sys/time.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <time.h>
#include "my_nvs.h"
#include "mac_manager.h"

#define	WIFI_CHANNEL_MAX		(13)
#define	WIFI_CHANNEL_SWITCH_INTERVAL	(500)
#define DEBUG (1)
#define RETRY (15)
#define SNIFFER_CHANNEL (1)
#define STACK_SIZE (50 * 1024)
#define TIMER_SECONDS (61)
#define SNIFFING_MS (5000)
#define PING_FREQUENCY (1000)
#define MAX_LEN (100)
#define PROTO_HI ("HI")
#define PROTO_HI_CODE (17)
#define PROTO_OK ("OK")
#define PROTO_OK_CODE (18)
#define PROTO_RT ("RT")
#define PROTO_RT_CODE (19)
#define PROTO_DE ("DE")
#define PROTO_DE_CODE (20)
#define PROTO_GO ("GO")
#define PROTO_GO_CODE (21)
#define PROTO_MSG_LEN (2)
#define PROTO_NUM_LEN (4)
#define MAX_BOARDS (10)


typedef struct info_s{
    int idBoard;
    struct sockaddr_in server_addr;
} info_t;



static void config();
void createTaskAndTimer();
void wifi_sniffer_handler(void *buff, wifi_promiscuous_pkt_type_t type);
void sender_task(void *parameters);
void identification_task(void *parameters);
void timer_callback(TimerHandle_t xTimer);
void print_task_state(eTaskState state);
int initialize_global_info();
int init_proto(int s);
int sendHi(int s, int idBoard);
int sendPN(int s);
int readCode(int s);
int recvInt(int s);
void esp_identification_handler(void *buff, wifi_promiscuous_pkt_type_t type);
int settimestamp(int s, int sec);
int sendDE(int s, int espFound);
void printmac(uint8_t* mac);

info_t global_info;
int s;
nvs_handle handle;
TaskHandle_t taskHandle;
TaskHandle_t espIdentificationHandle;
TimerHandle_t timerHandle;
TimerHandle_t pingTimerHandle = NULL;
TimerHandle_t sniffingHandle = NULL;
SemaphoreHandle_t s1, flag_mutex, identificationTaskSemaphore;

static void config(){
    pkt_array_init();
    init_mac_structures();

    printf("creating semaphores and mutex...\n");
    flag_mutex = xSemaphoreCreateMutex();
    s1 = xSemaphoreCreateBinary();
    identificationTaskSemaphore = xSemaphoreCreateBinary();
    if(flag_mutex == NULL || s1 == NULL){
        printf("Error on creating mutex and semaphores\n");
        exit(-1);
    }

    int res = my_nvs_init(&handle);
    if(res < 0){
        printf("Error on nvs_initialization\n");
        printf("Terminating...\n");
        exit(-1);
    }

    int result = initialize_global_info();
    if(result < 0){
        printf("Error %d\n", result);
        exit(-1);
    }

    s = -1;
    printf("initializing tcip adapter and wifi config\n");
    wifi_config();

    /*s = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if(s == INVALID_SOCKET){
        printf("Error on socket\n");
    }
    result = connect(s, (struct sockaddr*) &(global_info.server_addr), sizeof(global_info.server_addr));
    if(result < 0){
        printf("Error on connect: %d\n", result);
        close(s);
        s = -1;
        exit(-1);
    }

    printf("Connection to server established... socket: %d\n", s);
    printf("invoking createTaskAndTimer\n");
    fflush(stdout);*/
    createTaskAndTimer();
    return;
}

void createTaskAndTimer(){
    BaseType_t xReturned;
    int x = 0;
    //creazione dei task
    printf("Creating tasks...\n");
    xReturned = xTaskCreate(&sender_task, "senderTask", STACK_SIZE, &x, 1, &taskHandle);
    if(xReturned != pdPASS){
        printf("Could not allocate required memory to create sender task!\n");
        exit(-1);
    }
    /*
    xReturned = xTaskCreate(&identification_task, "identificationTask", STACK_SIZE, &x, 1, &espIdentificationHandle);
    if(xReturned != pdPASS){
        printf("Could not allocate required memory to create identification task!\n");
        exit(-1);
    }
    */
    
    eTaskState state = eTaskGetState(taskHandle);
    printf("sender task: ");
    print_task_state(state);
    /*
    state = eTaskGetState(identification_task);
    printf("identification task: ");
    print_task_state(state);
    */

    //printf("espIdentificationTask created? %d\n", espIdentificationHandle != NULL);

    //creazione del timer
    printf("Creating timers...\n");
    printf("%d\n", pdMS_TO_TICKS(TIMER_SECONDS * 1000));
    timerHandle = xTimerCreate("sendTimer", (TickType_t) pdMS_TO_TICKS(TIMER_SECONDS * 1000), pdFALSE, (void*) 0, &timer_callback);
    if(timerHandle == NULL){
        printf("Could not create sendTimer timer\n");
        exit(-1);
    }
    /*
    if( xTimerStart(timerHandle, 0) != pdPASS )
    {
        printf("Error in xTimerStart\n");
    }
    */
}

void wifi_sniffer_handler(void *buff, wifi_promiscuous_pkt_type_t type){
    if (type != WIFI_PKT_MGMT)
		return;

	const wifi_promiscuous_pkt_t *ppkt = (wifi_promiscuous_pkt_t *)buff;
    unsigned int frameControl = ((unsigned int)ppkt->payload[1] << 8) + ppkt->payload[0];
    uint8_t frameSubType = (frameControl & 0b0000000011110000) >> 4;

    if(frameSubType != 4)
        return;
    
    insert_pkt(ppkt);
    pkt_info_display(&pkt_array[head - 1]);
}

void sender_task(void *parameters){
    ulTaskNotifyTake(pdTRUE, portMAX_DELAY);
    s = init_proto(s);

    printf("Starting the 1-minute timer\n");
    if( xTimerStart(timerHandle, 0) != pdPASS )
    {
        printf("Error in xTimerStart\n");
    }
    esp_wifi_set_promiscuous_rx_cb(&wifi_sniffer_handler);
    //esp_wifi_set_promiscuous_rx_cb(&wifi_sniffer_nullhandler);

    while(true){
        //attesa
        ulTaskNotifyTake(pdTRUE, portMAX_DELAY);

        printf("Sender task started\n");
        int result;

        result = ntohl(global_info.idBoard);
        result = send(s, (void *) &result, (size_t) sizeof(int), 0);
        if(result != sizeof(int)){
            printf("Error on send: %d (%d)\n", result, errno);
            if(errno == EPIPE){
                printf("Connection closed by server\n");
            }
            close(s);
        }
        result = array_send(s);
        if(result < 0){
            printf("Errore sulla send: %d\n", result);
            close(s);
        }

        printf("Waiting for timestamp from server...\n");
        result = settimestamp(s, 0);
        if(result <= 0){
            printf("Error on settimestamp (%d)\n", result);
        }

        printf("Sender task ended\n");

        pkt_array_init();
        //restart del timer
        if(xTimerReset(timerHandle, 0) != pdPASS){
            printf("Error on timer restart\n");
        }

        //sostituisco con la callback corretta
        esp_wifi_set_promiscuous_rx_cb(&wifi_sniffer_handler);
    }
}

/*
void identification_task(void *parameters){
    while(true){
        printf("identification task: going to sleep\n");
        ulTaskNotifyTake(pdTRUE, portMAX_DELAY);
        //xSemaphoreTake(identificationTaskSemaphore, portMAX_DELAY);
        printf("identification task: woke up\n");
        esp_wifi_set_promiscuous_rx_cb(&esp_identification_handler);
        printf("identification task: waiting for the time to wake me up\n");
        ulTaskNotifyTake(pdTRUE, portMAX_DELAY);
        //xSemaphoreTake(identificationTaskSemaphore, portMAX_DELAY);
        printf("identification task: woke up from timer, setting sniffer_handler to nullhandler");
        esp_wifi_set_promiscuous_rx_cb(&wifi_sniffer_nullhandler);
        printf("identification task: signaling semaphore s1");
        xSemaphoreGive(s1);
    }
}
*/

void timer_callback(TimerHandle_t xTimer){
    BaseType_t xResult;
    uint32_t ulCount;

    ulCount = (uint32_t) pvTimerGetTimerID(xTimer);

    if(ulCount == 0){
        //sostituisco la callback con una che non fa nulla
        esp_wifi_set_promiscuous_rx_cb(&wifi_sniffer_nullhandler);
        printf("Timer callback invoked: interrupting data acquisition\n");

        //inviare notifica al task
        xResult = xTaskNotifyGive(taskHandle);
        if(xResult != pdPASS){
            printf("xTaskNotifyGive failed\n");
        }
    }
    else if(ulCount == 1){
        esp_wifi_set_promiscuous_rx_cb(&wifi_sniffer_nullhandler);
        printf("Timer callback invoked: interrupting search for espBoards\n");

        printf("Stopping pingTimer...\n");
        if( xTimerStop(pingTimerHandle, 0) != pdPASS )
        {
            printf("Error in xTimerStop\n");
        }

        //xSemaphoreGive(s1);
        //xSemaphoreGive(identificationTaskSemaphore);
        //xResult = xTaskNotifyGive(espIdentificationHandle);
		xResult = xTaskNotifyGive(taskHandle);
        if(xResult != pdPASS){
            printf("xTaskNotifyGive failed\n");
        }
    }
    else if(ulCount == 2){
        printf("Timer callback for ping invoked...\n");
        printf("Sending PN message to socket\n");

        int res = sendPN(s);
        if(res != 2){
            printf("Error on sendPN... retry at next step, %d\n", res);
        }
        printf("Ending PN timer callback...\n");
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

//Inizializza i parametri globali idBoard, ipAddress del server e porta del server in global_info
int initialize_global_info(){
    int32_t id = -1, port;
    char buffer[MAX_LEN];
    char *result;

    id = readnvs_i32(handle, ID_KEY);
    if(id < 0){
        return NVS_ERROR;
    }

    result = readnvs_str(handle, IP_KEY, buffer, MAX_LEN);
    if(result == NULL || result != buffer){
        return NVS_ERROR;
    }

    port = readnvs_i32(handle, PORT_KEY);
    if(port < 0){
        return NVS_ERROR;
    }

    global_info.idBoard = id;
    global_info.server_addr.sin_family = AF_INET;
    if(inet_aton(buffer, &(global_info.server_addr.sin_addr)) < 0){
        printf("Error on inet_aton");
        exit(-1);
    }
    global_info.server_addr.sin_port = htons((int16_t) port);

    printf("Initialization...\n");
    printf("idBoard: %d\n", id);
    printf("server_ip: %s\n", buffer);
    printf("server_port: %d\n", port);
    printf("________________________");

    return 0;
}

int settimestamp(int s, int sec){
    uint32_t timestamp;
    struct timeval tval;
    int result;

    result = recv(s, (void*) &timestamp, sizeof(timestamp), 0);
    if(result < 0){
        printf("Error on receiving timestamp (%d)\n", errno);
        return RECV_ERROR;
    }
    timestamp = ntohl(timestamp);
    tval.tv_sec = timestamp;
    tval.tv_usec = 0;
    result = settimeofday(&tval, NULL);
    if(result != 0){
        printf("Error on settimeofday\n");
        return SETTIME_ERROR;
    }
    printf("Timestamp received: %ld\n", tval.tv_sec);
    
    return 1;
}

int init_proto(int sock){
    char buff[MAX_LEN];
    int res = 0;
    int count = 0;

    while(count < RETRY){
        sock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
        if(sock == INVALID_SOCKET || sock < 0){
            printf("Error on socket\n");
            exit(-1);
        }

        s = sock;
        printf("Trying to connect: %d\n", count);
        res = connect(sock, (struct sockaddr*) &(global_info.server_addr), sizeof(global_info.server_addr));
        if(res < 0){
            count++;
            printf("Error on connect: %d\n", res);
            close(sock);
        }
        else{
            break;
        }
    }
    
    if(count >= RETRY){
        printf("Could not connect to server... rebooting the system...\n");
        close(s);
        exit(-1);
    }

    printf("Connection to server established... socket: %d\n", s);
    printf("invoking createTaskAndTimer\n");
    fflush(stdout);

	printf("Init_proto start with socket: %d\n", s);

    if(sock < 0){
        printf("Invalid socket %d\n", s);
        exit(-1);
    }

	printf("Sending hi\n");
    res = sendHi(s, global_info.idBoard);    
    if(res < 0){
        printf("Error on sending init_proto: HELLO (%d)\n", errno);
        if(errno == EPIPE){
            printf("Connection closed by server\n");
        }
        return SEND_ERROR;
    }

	printf("Read code\n");
    res = readCode(s);
    if(res == PROTO_OK_CODE){
        printf("Received OK from server... Parsing the list of devices\n");
        nmacs = recvInt(s);

        if(nmacs < 0){
            printf("Error on nmacs\n");
            return RECV_ERROR;
        }
		printf("Number of devices to detect: %d\n", nmacs);
		
		if(nmacs > 0){
			res = recvmac(s);
			if(res < 0){
				printf("recvmac failed (%d)\n", res);
				return res;
			}

            pingTimerHandle = xTimerCreate("pingTimer", (TickType_t) pdMS_TO_TICKS(PING_FREQUENCY), pdTRUE, (void*) 2, &timer_callback);
            if(pingTimerHandle == NULL){
                printf("Could not create pingTimer timer\n");
                exit(-1);
            }

            sniffingHandle = xTimerCreate("espIdentifTimer", (TickType_t) pdMS_TO_TICKS(SNIFFING_MS), pdFALSE, (void*) 1, &timer_callback);
            if(sniffingHandle == NULL){
                printf("Could not create sniffingHandle timer\n");
                exit(-1);
            }
            
            if( xTimerStart(pingTimerHandle, 0) != pdPASS )
            {
                printf("Error in xTimerStart of pingTimerHandle\n");
            }
            if( xTimerStart(sniffingHandle, 0) != pdPASS )
            {
                printf("Error in xTimerStart of sniffingHandle\n");
            }
            resetFlagList();
        }
		
		//da este   re per il riconoscimento delle altre board
		//per ora salto il riconoscimento dei MAC delle altre board e spedisco sempre 0 nel DE

    }
    else{
        printf("Unknown message...\n");
        return PROTO_UNKNOWN_MSG;
    }
    while(1){
        //AGGIUNGERE LA PARTE RELATIVA ALLO SNIFFING e AL DE
        //printf("Sending SemaphoreGive to identificationTask\n");
        //xSemaphoreGive(identificationTaskSemaphore);
        //xTaskNotifyGive(espIdentificationHandle);
		int nFound = 0;
		printf("Starting DE cycle...\n");
		if(nmacs > 0){
            esp_wifi_set_promiscuous_rx_cb(&esp_identification_handler);
			//xSemaphoreTake(s1, portMAX_DELAY);
            ulTaskNotifyTake(pdTRUE, portMAX_DELAY);
			xSemaphoreTake(flag_mutex, portMAX_DELAY);

			printf("Counting esp found...\n");
			nFound = countEspFound();
            printf("Releasing flag_mutex\n");
            xSemaphoreGive(flag_mutex);
		}

        res = sendDE(sock, nFound);
        if(res < 0){
            printf("Error on sendDE (%d)\n", res);
            exit(-1);
        }
	
        res = readCode(sock);
        if(res == PROTO_GO_CODE){
			printf("Received GO message\n");
            printf("Destroying the timer...");
            if( pingTimerHandle != NULL && xTimerDelete(pingTimerHandle, 0) != pdPASS )
            {
                printf("Error in xTimerDestroy of pingTimerHandle\n");
            }
            if( sniffingHandle != NULL && xTimerDelete(sniffingHandle, 0) != pdPASS )
            {
                printf("Error in xTimerDestroy of sniffingHandle\n");
            }
			break;
		}
        else if(res == PROTO_RT_CODE){
            printf("RT received... retry ESP identification\n");
            printf("Resetting the flag list\n");
            resetFlagList();
            printf("Restarting the ping timer...\n");
            if( xTimerReset(pingTimerHandle, 0) != pdPASS )
            {
                printf("Error in xTimerStart of pingTimerHandle\n");
            }
            if( xTimerReset(sniffingHandle, 0) != pdPASS )
            {
                printf("Error in xTimerReset of sniffingHandle\n");
            }
        }
        else{
            printf("Unknown code received: %s\n", buff);
            return PROTO_UNKNOWN_MSG;
        }
    }

    printf("Waiting for timestamp...\n");
    res = settimestamp(sock, 0);
    if(res <= 0){
        printf("Error on settimestamp (%d)\n", res);
    }

    return sock;
}

int sendHi(int s, int idBoard){
    uint8_t mac_addr[6];
    esp_err_t err;
    char buff[MAX_LEN];

    err = esp_wifi_get_mac(ESP_IF_WIFI_STA, mac_addr);
    switch(err){
    case ESP_ERR_WIFI_NOT_INIT:
        printf("WiFi not initialized!\n");
        exit(-1);
        break;
    case ESP_ERR_INVALID_ARG:
        printf("Invalid argument\n");
        exit(-1);
        break;
    case ESP_ERR_WIFI_IF:
        printf("Invalid interface\n");
        exit(-1);
        break;
    case ESP_OK:
        printf("MAC_ADDR retrieved successfully\n");
        break;
    default:
        printf("Unknown error\n");
        exit(-1);
        break;
    }

    memcpy(buff, PROTO_HI, 2);
    int netIdBoard = htonl(idBoard);
    memcpy(&(buff[PROTO_MSG_LEN]), &netIdBoard, sizeof(netIdBoard));
    memcpy(&(buff[PROTO_MSG_LEN + PROTO_NUM_LEN]), mac_addr, 6*sizeof(uint8_t));

    int res = send(s, buff, PROTO_MSG_LEN + PROTO_NUM_LEN + 6, 0);
    if(res != PROTO_MSG_LEN + PROTO_NUM_LEN + 6)
        res = SEND_ERROR;
    return res;
}

int sendPN(int s){
    char msg[3] = "PN";

    int res = send(s, msg, 2, 0);
    printf("Send PN message returned %d\n", res);
    if(res != 2){
        printf("Error on send");
        return SEND_ERROR;
    }
    return res;
}

int readCode(int s){
    char buff[PROTO_MSG_LEN + 1];
    int res;

    if(s < 0){
        printf("Invalid socket received\n");
        return -1;
    }

    res = recv(s, buff, PROTO_MSG_LEN, 0);
    if(res != PROTO_MSG_LEN){
        printf("Error on recv\n");
        return RECV_ERROR;
    }

    buff[PROTO_MSG_LEN] = '\0';
	printf("Message received: %s\n", buff);
    if(strcmp(buff, PROTO_HI) == 0)
        return PROTO_HI_CODE;
    else if(strcmp(buff, PROTO_OK) == 0)
        return PROTO_OK_CODE;
    else if(strcmp(buff, PROTO_RT) == 0)
        return PROTO_RT_CODE;
    else if(strcmp(buff, PROTO_DE) == 0)
        return PROTO_DE_CODE;
    else if(strcmp(buff, PROTO_GO) == 0)
        return PROTO_GO_CODE;
    return PROTO_UNKNOWN_MSG;    
}

int recvInt(int s){
    int res, n;
    res = 0;
    n = -1;

    res = recv(s, &n, sizeof(n), 0);
    if(res != sizeof(n)){
        printf("Unexpected value received: read %d B from socket (%d)\n", res, errno);
        return RECV_ERROR;
    }

    n = ntohl(n);
    return n;
}

void esp_identification_handler(void *buff, wifi_promiscuous_pkt_type_t type){
    const wifi_promiscuous_pkt_t *pkt = (wifi_promiscuous_pkt_t *)buff;
    static int count = 0;
    uint8_t pkt_mac[8];

    extract_mac_src((void*) pkt->payload, pkt_mac);
    printf("pkt mac: ");
    printmac(pkt_mac);
    printf("\n");

    xSemaphoreTake(flag_mutex, portMAX_DELAY);
    for(int i = 0; i < nmacs; i++){
        printf("comparing macs:\n");
        printf("mac stored: ");
        printmac(mac_stored[i]);
        printf("\n");

        if(cmp_mac(pkt, mac_stored[i])){
            flag_list[i] = 1;
            count++;
            if(count == nmacs - 1){
                count = 0;
                esp_wifi_set_promiscuous_rx_cb(&wifi_sniffer_nullhandler);
            }
        }
    }
    xSemaphoreGive(flag_mutex);
}

int sendDE(int s, int espFound){
    char buff[MAX_LEN];
    int tmp = 0;
    int offset = 0;

    if(s < 0){
        printf("Invalid socket\n");
        return -1;
    }

	printf("Sending DE %d to server\n", espFound);
    memcpy(buff, PROTO_DE, PROTO_MSG_LEN);
    tmp = htonl(espFound);
    memcpy(&(buff[PROTO_MSG_LEN]), &tmp, sizeof(tmp));
    offset = PROTO_MSG_LEN + sizeof(tmp);
	/*
    for(int i = 0; i < nmacs; i++){
        if(!mac_flags[i]){
            memcpy(&(buff[offset]), search_macs[i], 6);
            offset += 6;
        }
    }
	*/

    tmp = send(s, buff, offset, 0);
    if(tmp != offset){
        printf("Error on send (%d)\n", tmp);
        return SEND_ERROR;
    }

    return tmp;
}

void printmac(uint8_t* mac){
    printf("MAC: %02x:%02x:%02x:%02x:%02x:%02x", mac[0],
            mac[1],
            mac[2],
            mac[3],
            mac[4],
            mac[5]);
}

int main(){
    timerHandle = NULL;
    taskHandle = NULL;
    espIdentificationHandle = NULL;

    config();
    wifi_sniffer_set_channel(SNIFFER_CHANNEL);
    xTaskNotifyGive(taskHandle);
    //vTaskStartScheduler();
    
    /*
    while (true) {
		vTaskDelay(WIFI_CHANNEL_SWITCH_INTERVAL / portTICK_PERIOD_MS);
		wifi_sniffer_set_channel(channel);
		channel = (channel % WIFI_CHANNEL_MAX) + 1;
    }
    */
}

extern "C" void app_main(void){
    main();
}