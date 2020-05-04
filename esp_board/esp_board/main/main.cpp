/*
    Progetto PDS a.a. 2017/2018
*/

#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "freertos/event_groups.h"
#include "esp_wifi.h"
#include "esp_wifi_types.h"
#include "esp_system.h"
#include "esp_event.h"
#include "esp_event_loop.h"
#include "nvs_flash.h"
#include "driver/gpio.h"
#include "string.h"
#include "wifi.h"
#include <sys/time.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <time.h>
#include "my_nvs.h"
#include "sniffed_packet.h"
#include <vector>
#include <string>
#include <iostream>

#define MAX_RETRY (15)
#define SNIFFER_CHANNEL (1)
#define STACK_SIZE (50 * 1024)
#define SNIFFING_TIME_SEC (60)
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

#define SERVER_IP "10.0.0.78"
#define SERVER_PORT 7999


std::vector<SniffedPacket> sniffed_packets;

typedef struct info_s{
    int idBoard;
    struct sockaddr_in server_addr;
} info_t;



static void config();
void createTask();
void wifi_sniffer_handler(void *buff, wifi_promiscuous_pkt_type_t type);
void sendTask(void *parameters);
void print_task_state(eTaskState state);
int initialize_global_info();
int init_proto(int s);
int sendHi(int s, int idBoard);
int sendPN(int s);
int readCode(int s);
int recvInt(int s);
int settimestamp(int s, int sec);
int sendDE(int s, int espFound);

info_t global_info; /* stores ID of this board and Server IP address and port */
int s; /* socket for the connection with the server */
nvs_handle handle;
TaskHandle_t send_task_handle;
const TickType_t send_task_delay = SNIFFING_TIME_SEC * 1000 / portTICK_PERIOD_MS;

/** @brief send all the array of packets sniffed
 *  first send [#packets]
 *  then send every packet of the array at a time
 */
int array_send(int socket){

    if(socket <= 0){
        printf("Invalid socket (%d)\n", socket);
        return INVALID_SOCKET;
    }

    int packets_count = ntohl(sniffed_packets.size());
    int res = send(socket, (void*)&packets_count, sizeof(packets_count), 0);

    for(int i = 0; i < sniffed_packets.size(); i++){
        res = SniffedPacket::sendSniffedPacket(sniffed_packets[i], socket);

        if(res < 0){
            printf("Packet at index %d returned %d while sending\n", i, res);
        }
    }

    return 0;
}

static void config(){
    // get Board ID, Server IP, Server Port
    printf("nvs_initialization..\n");
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

    BaseType_t xReturned;
    int x = 0;
    printf("Creating tasks...\n");
    // create and starts sendTask
    // TODO PROBABLY WE CAN REMOVE THE x PARAMETER PASSED
    xReturned = xTaskCreate(&sendTask, "sendTask", STACK_SIZE, &x, 1 /*priority*/, &send_task_handle);
    if(xReturned != pdPASS){
        printf("Could not allocate required memory to create sendTask!\n");
        exit(-1);
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

/** @brief the main task
 *  First initialize the protocol
 *  Then starts sniffing for send_task_delay
 *  At the end sends the sniffed packets and starts sniffing again
 *  @param parameters input parameters, not used
 */
void sendTask(void *parameters){
    // doesn't start until the config is conluded
    ulTaskNotifyTake(pdTRUE, portMAX_DELAY);
    s = init_proto(s);

    // every send_task_delay send:
    // --> id_board
    // --> array of devices found
    // then receive
    // <-- time from server
    while(true){
        // start sniffing
        printf("New sniffing phase: starting data acquisition\n");
        esp_wifi_set_promiscuous_rx_cb(&wifi_sniffer_handler);
        
        // sleep for sniffing
        vTaskDelay(send_task_delay);

        printf("New send phase: interrupting data acquisition\n");
        esp_wifi_set_promiscuous_rx_cb(&wifi_sniffer_nullhandler);

        int32_t id_board_net = ntohl(global_info.idBoard);
        size_t sent = send(s, (void *) &id_board_net, (size_t) sizeof(int32_t), 0);
        if(sent != sizeof(int)){
            printf("Error on send: %d (%d)\n", sent, errno);
            if(errno == EPIPE){
                printf("Connection closed by server\n");
            }
            close(s);
        }
        sent = array_send(s);
        if(sent < 0){
            printf("Errore sulla send: %d\n", sent);
            close(s);
            exit(-1);
        }

        printf("Waiting for timestamp from server...\n");
        sent = settimestamp(s, 0);
        if(sent <= 0){
            printf("Error on settimestamp (%d)\n", sent);
        }

        sniffed_packets.clear();
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

/** @brief initialize global_info with:
 *      idBoard of this esp32
 *      server IP Address and port
 */
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
    result = SERVER_IP;

    port = readnvs_i32(handle, PORT_KEY);
    port = SERVER_PORT;
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

/** @brief set the time received with the GO message
 *  this is needed to synchronize all the esp boards
 */
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

/** @brief initialize the protocol with the server
 *  1) send [HI boardid(4B) MAC_ADDR(6B)]
 *  2) rcv [OK #espboards list_of_mac_addr]
 *  3) sniffing phase: tries to sniff the esp boards. ends in 5 seconds OR when found all the boards
 *  4) send [DE #espboards_detected]
 *  5a) rcv [GO time]
 *  6a) rcv [RT] retry sniffing
 */
int init_proto(int sock){
    char buff[MAX_LEN];
    int res = 0;
    int count = 0;

    // try to connect to the server
    bool is_connected = false;
    while(count < MAX_RETRY && !is_connected){
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
            is_connected = true;
        }
    }
    
    if(count >= MAX_RETRY){
        printf("Could not connect to server... rebooting the system...\n");
        close(s);
        exit(-1);
    }

    printf("Connection to server established... socket: %d\n", s);
    fflush(stdout);

	printf("Init_proto start with socket: %d\n", s);

    // send [HI idBoard mac_addr]
	printf("Sending hi\n");
    res = sendHi(s, global_info.idBoard);    
    if(res < 0){
        printf("Error on sending init_proto: HELLO (%d)\n", errno);
        if(errno == EPIPE){
            printf("Connection closed by server\n");
        }
        return SEND_ERROR;
    }

    // wait for OK reponse from the server
    // read how many boards are there
	printf("Read code\n");
    int nmacs;
    res = readCode(s);
    if(res == PROTO_OK_CODE){
        printf("Received OK from server... Parsing the list of devices\n");
        nmacs = recvInt(s);

        if((nmacs < 0) || (nmacs > 0)){
            printf("Error on nmacs! 0!=%d\n", nmacs);
            printf("DE phase not implemented!");
            exit(1);
            return RECV_ERROR;
        }
    }
    else{
        printf("Unknown message...\n");
        exit(-1);
        return PROTO_UNKNOWN_MSG;
    }
    int nFound = 0;
    res = sendDE(sock, nFound);
    if(res < 0){
        printf("Error on sendDE (%d)\n", res);
        exit(-1);
    }
    // read GO code
    res = readCode(sock);
    if(res == PROTO_GO_CODE){
        printf("Received GO message\n");
    }
    else if(res == PROTO_RT_CODE){
        printf("RT received... retry ESP identification\n");
        printf("ERROR! ESP identification not implemented");
        exit(1);
    }
    else{
        printf("Unknown code received: %s\n", buff);
        exit(1);
        return PROTO_UNKNOWN_MSG;
    }

    printf("Waiting for timestamp...\n");
    res = settimestamp(sock, 0);
    if(res <= 0){
        printf("Error on settimestamp (%d)\n", res);
    }

    return sock;
}
    
/** send [ HI idBoard mac_addr ]
 */
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
    // send [ HI IdBoard mac_addr ]
    memcpy(buff, PROTO_HI, 2);
    // TODO no sense to send idBoard as an int over the network (4 bytes)
    // and have to do also all the htonl shit
    // we should think about sending it as a char (1 byte).
    int netIdBoard = htonl(idBoard);
    memcpy(&(buff[PROTO_MSG_LEN]), &netIdBoard, sizeof(netIdBoard));
    memcpy(&(buff[PROTO_MSG_LEN + PROTO_NUM_LEN]), mac_addr, 6*sizeof(uint8_t));

    int res = send(s, buff, PROTO_MSG_LEN + PROTO_NUM_LEN + 6, 0);
    if(res != PROTO_MSG_LEN + PROTO_NUM_LEN + 6)
        res = SEND_ERROR;
    return res;
}

/** @brief read a message from the socket and returns the code of the message
 */
int readCode(int s){
    char buff[PROTO_MSG_LEN + 1];
    int res;

    if(s < 0){
        printf("Invalid socket received\n");
        return -1;
    }

    res = recv(s, buff, PROTO_MSG_LEN, 0);
    if(res != PROTO_MSG_LEN){
        printf("Error on recv read code\n");
        // TODO try with std exceptions to handle this shit
        exit(-1);
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

/** @brief read a 4 bytes integer from the socket
 */
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

/** @brief
 *  sends ["DE" #esp_found ]
 *  //TODO remove it when we change the protocol. Right now we cannot remove it yet 
 */
int sendDE(int s, int32_t esp_found){
    uint8_t buff[MAX_LEN];

	printf("Sending DE %d to server\n", esp_found);
    memcpy(buff, PROTO_DE, PROTO_MSG_LEN);
    int32_t esp_found_net = htonl(esp_found);
    memcpy(&(buff[PROTO_MSG_LEN]), &esp_found_net, sizeof(esp_found_net));
    size_t message_length = PROTO_MSG_LEN + sizeof(esp_found_net);

    int res = send(s, buff, message_length, 0);
    if(res != message_length){
        printf("Error on send (%d)\n", res);
        exit(1);
        return SEND_ERROR;
    }

    return res;
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