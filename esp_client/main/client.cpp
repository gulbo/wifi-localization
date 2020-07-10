#include "client.h"
#include "esp_wifi.h"
#include <iostream>

Client::Client(uint8_t board_id, std::string server_ip, int32_t server_port){
    board_id_ = board_id;
    server_ip_ = server_ip;
    server_port_ = server_port;

    server_address_.sin_family = AF_INET;

    if(inet_aton(server_ip_.c_str(), &(server_address_.sin_addr)) < 0){
        throw std::runtime_error("Error parsing " + server_ip_);
    }

    server_address_.sin_port = htons(server_port);

    std::cout << "****** Client Interface ******" << std::endl;
    std::cout << "board_id: " << std::to_string(board_id) << std::endl;
    std::cout << "server_ip: " << server_ip << std::endl;
    std::cout << "server_port: " << std::to_string(server_port) << std::endl;
    std::cout << "******************************" << std::endl;
}

void Client::close()
{
    if (is_connected_)
    {
        is_connected_ = false;
        ::close(socket_);
    }
}

Client::~Client()
{
    close();
}

bool Client::connect()
{
    if (!is_connected_)
    {
        socket_ = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
        if (socket_ < 0)
        {
            throw std::runtime_error("Error creating the socket");
        }

        is_connected_ = true;
        if (::connect(socket_, (struct sockaddr*) &(server_address_), sizeof(server_address_)) < 0)
        {
            close();
        }
    }

    return is_connected_;
}

bool Client::isConnected()
{
    return is_connected_;
}

uint32_t Client::start()
{
    assert(is_connected_);

    std::cout << "Sending HI..." << std::endl;
    if (!sendHi_())
        throw std::runtime_error("Error sending Hi");
    std::cout << "HI sent" << std::endl;
    
    // wait for OK reponse from the server
    std::cout << "Waiting HI..." << std::endl;
    std::string code;
    if(!readProtocolCode_(code))
        throw std::runtime_error("Invalid code received: " + code);
    if(code != ProtocolCode::HI)
        throw std::runtime_error("Waiting for HI, received : " + code);
        
    std::cout << "Received HI" << std::endl;
    
    // wait for GO code
    std::cout << "Waiting for GO..." << std::endl;
    if(!readProtocolCode_(code))
        throw std::runtime_error("Invalid code received: " + code);

    if (code != ProtocolCode::GO)
        throw std::runtime_error("Waiting for GO, received instead: " + code);

    std::cout << "Received GO" << std::endl;

    // wait for time from the server
    std::cout << "Waiting to synchronize system time..." << std::endl;
    uint32_t epoch = 0;
    if(!readInt(epoch))
    {
        throw std::runtime_error("Error receiving time");
    }

    std::cout << "Time received: " << std::to_string(epoch) << std::endl;
    return epoch;
}

bool Client::sendHi_(){
    assert(is_connected_);

    uint8_t mac_addr[SniffedPacket::MAC_ADDRESS_BYTES];
    char buff[MAX_MESSAGE_LENGTH];

    esp_err_t err = esp_wifi_get_mac(ESP_IF_WIFI_STA, mac_addr);
    switch(err){
        case ESP_OK:
            std::cout << "MAC_ADDR retrieved successfully" << std::endl;
            break;
        case ESP_ERR_WIFI_NOT_INIT:
            throw std::runtime_error("esp_wifi_get_mac: WiFi not initialized!");
        case ESP_ERR_WIFI_IF:
            throw std::runtime_error("esp_wifi_get_mac: Invalid interface");
        case ESP_ERR_INVALID_ARG:
            throw std::runtime_error("esp_wifi_get_mac: Invalid argument");
        default:
            throw std::runtime_error("esp_wifi_get_mac: Unknown error");
    }
    // send [ HI IdBoard mac_addr ]
    memcpy(buff, ProtocolCode::HI.c_str(), 2);

    int32_t board_id_net = htonl(board_id_);
    memcpy(&(buff[ProtocolCode::CODE_LENGTH]), &board_id_net, sizeof(board_id_net));
    memcpy(&(buff[ProtocolCode::CODE_LENGTH + sizeof(int32_t)]), mac_addr, 6*sizeof(uint8_t));

    int bytes_sent = ::send(socket_, buff, ProtocolCode::CODE_LENGTH + sizeof(int32_t) + 6, 0);
    if(bytes_sent != ProtocolCode::CODE_LENGTH + sizeof(int32_t) + 6)
    {
        return false;
    }
    return true;
}

bool Client::readProtocolCode_(std::string& code){
    assert(is_connected_);

    char msg[ProtocolCode::CODE_LENGTH + 1];

    size_t bytes_received = ::recv(socket_, msg, ProtocolCode::CODE_LENGTH, 0);
    if(bytes_received != ProtocolCode::CODE_LENGTH){
        std::cout << "Error reading protocol code" << std::endl;
        return false;
    }
    msg[2] = '\0'; // add end string
    code = std::string(msg);

    return ProtocolCode::isValid(code);
}

bool Client::readInt(int32_t& value){
    assert(is_connected_);
    size_t bytes = ::recv(socket_, &value, sizeof(value), 0);
    if(bytes != sizeof(value))
    {
        if (errno == EPIPE)
        {
            close();
            throw std::runtime_error("Connection closed by the server");
        }
        return false;
    }

    value = ntohl(value);
    return true;
}

bool Client::readInt(uint32_t& value){
    assert(is_connected_);
    size_t bytes = ::recv(socket_, &value, sizeof(value), 0);
    if(bytes != sizeof(value))
    {
        if (errno == EPIPE)
        {
            close();
            throw std::runtime_error("Connection closed by the server");
        }
        return false;
    }

    value = ntohl(value);
    return true;
}

bool Client::sendInt(const uint32_t& value)
{
    assert(is_connected_);

    uint32_t value_host = ntohl(value);
    size_t bytes = ::send(socket_, (void *) &value_host, sizeof(value_host), 0);

    if(bytes != sizeof(value_host))
    {
        if (errno == EPIPE)
        {
            close();
            throw std::runtime_error("Connection closed by the server");
        }
        return false;
    }
    return true;
}

bool Client::sendBoardID()
{
    return sendInt(board_id_);
}

void Client::sendPackets(std::list<SniffedPacket>& packets)
{
    assert(is_connected_);

    // first send the total number of packets
    uint32_t packets_count = ntohl(packets.size());
    size_t bytes_sent = ::send(socket_, (void*) &packets_count, sizeof(packets_count), 0);
    if (bytes_sent != sizeof(packets_count))
    {
        std::cout << "Error sending number of available packets" << std::endl;
        std::cout << "Retrying at the next loop..." << std::endl;
        return;
    }

    // then send all the packets one by one
    uint32_t failed_count = 0;
    for (auto packet_it = std::begin(packets); packet_it != std::end(packets); )
    {
        std::cout << "Sending packet... ";
        // if successful delete the packet
        if (sendPacket(*packet_it))
        {
            packet_it = packets.erase(packet_it);
            std::cout << "SUCCESS" << std::endl;
        }
        else
        {
            ++packet_it;
            ++failed_count;
            std:: cout << "FAILED" << std::endl;
        }
    }

    std::cout << "Send packets complete." << std::endl;
    std::cout << "Total packets: " << std::to_string(packets_count) <<  std::endl;
    std::cout << "Failed: " << std::to_string(failed_count) <<  std::endl;
}

bool Client::sendPacket(const SniffedPacket& packet)
{
    assert(is_connected_);

    uint8_t buffer[SniffedPacket::MAC_ADDRESS_BYTES + 
                   SniffedPacket::RSSI_BYTES +
                   SniffedPacket::SSID_LENGTH_BYTES +
                   SniffedPacket::SSID_BYTES +
                   SniffedPacket::TIMESTAMP_BYTES +
                   SniffedPacket::CHECKSUM_BYTES];

    uint8_t *buf_ptr = buffer;

    memcpy(buf_ptr, packet.mac_address, SniffedPacket::MAC_ADDRESS_BYTES);
    buf_ptr += SniffedPacket::MAC_ADDRESS_BYTES;


    //aggiungo rssi nel buffer
    uint16_t network_rssi = htons(packet.rssi); 
    memcpy(buf_ptr, &network_rssi, sizeof(uint16_t));
    buf_ptr += sizeof(uint16_t);

    //NON trasmetto il terminatore
    uint16_t networkd_ssid_len = htons(packet.ssid_length);
    memcpy(buf_ptr, &networkd_ssid_len, sizeof(uint16_t));
    buf_ptr += sizeof(uint16_t);

    //copio l'ssid nel buffer se la sua lunghezza è != 0
    if(packet.ssid_length != 0){
        memcpy(buf_ptr, packet.ssid, packet.ssid_length);
        buf_ptr += packet.ssid_length;
        *buf_ptr = 0;
    }

    //copio timestamp nel buffer
    uint32_t network_timestamp = htonl((uint32_t) packet.timestamp);
    memcpy(buf_ptr, &network_timestamp, sizeof(uint32_t));
    buf_ptr += sizeof(uint32_t);

    //copio il checksum nel buffer
    memcpy(buf_ptr, packet.checksum, SniffedPacket::CHECKSUM_BYTES);
    buf_ptr += SniffedPacket::CHECKSUM_BYTES;

    //invio i dati sul socket connesso
    size_t bytes_sent = ::send(socket_, (void*) buffer, (size_t) (buf_ptr - buffer), 0);

    if(errno == EPIPE){
        is_connected_ = false;
        std::runtime_error("Connection dropped, broken pipe");
    }

    if(bytes_sent != (size_t) (buf_ptr - buffer)){
        return false;
    }

    return true;
}