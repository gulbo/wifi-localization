#ifndef __CLIENT_H__
#define __CLIENT_H__

#include "sys/socket.h"
#include "sniffed_packet.h"
#include "protocol_code.h"
#include <list>

#define PROTO_NUM_LEN (4)

/**
 * @brief Client class that groups all the client interface to the server
 */
class Client
{
    public:

        static const uint16_t MAX_CONNECTION_RETRIES = 15;
        static const size_t MAX_MESSAGE_LENGTH = 128;

        Client(uint8_t board_id, std::string server_ip, int16_t server_port);

        ~Client();

        /** 
         * @brief connect to the server
         * @return is_connected
         */
        bool connect();

        /**
         * @brief close connection
         */
        void close();

        /**
         * @brief is connected to the server
         * @return
         */
        bool isConnected();

        /**
        * @brief initialize the protocol with the server
        *  1) send [HI boardid(4B) MAC_ADDR(6B)]
        *  2) rcv [OK 0 <list_of_mac_addr>]
        *  4) send [DE 0]
        *  5a) rcv [GO time]
        *  6a) rcv [RT] retry sniffing
        * @return epoch given by the server
        */
        uint32_t start();

        /**
         * @brief read a 4 bytes integer from the socket
         * @return successful
         */
        bool readInt(int32_t& value);

        /**
         * @brief read a 4 bytes unsigned integer from the socket
         * @return successful
         */
        bool readInt(uint32_t& value);

        /**
         * @brief send a 4 bytes unsigned integer on the socket
         * @return successful
         */
        bool sendInt(const uint32_t& value);

        /**
         * @brief send the board_id
         * @return successful
         */
        bool sendBoardID();

        /**
         * @brief send a list of packets
         *        first send [#packets]
         *        then send every packet one at a time
         * @note remove from the list the packets successfully sent, leave there the failed ones
         * @param packets list
         */
        void sendPackets(std::list<SniffedPacket>& packets);

        /**
         * @brief send a packet
         * @param packet
         * @return successful
         */
        bool sendPacket(const SniffedPacket& packet);
    
    private:
        uint8_t board_id_;
        std::string server_ip_;
        int16_t server_port_;
        struct sockaddr_in server_address_;
        int16_t socket_ = -1;
        bool is_connected_ = false;
        
        /**
         * @brief send [ HI idBoard mac_addr ]
         * @return successful
         */
        bool sendHi_();

        /**
         * @brief sends ["DE" #esp_found ]
         *        //TODO remove it when we change the protocol. Right now we cannot remove it yet 
         * @return successful
         */
        bool sendDE_(int32_t esp_found);

        /**
        * @brief read a message and returns its code
        * @param code output argument for the code
        * @return successful
        */
        bool readProtocolCode_(std::string& code);
        
};

#endif