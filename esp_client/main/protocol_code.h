#ifndef __PROTOCOL_CODE_H__
#define __PROTOCOL_CODE_H__

#include <string>

/**
 * @brief struct that holds the protocol specific constants and methods
 */
struct ProtocolCode
{
    static const std::string HI;
    static const std::string OK;
    static const std::string RT;
    static const std::string DE;
    static const std::string GO;
    static const size_t CODE_LENGTH = 2;

    /** 
     * @brief check if a code is a valid protocol code
     * @return is valid
     */
    static bool isValid(const std::string& code);

};

#endif // __PROTOCOL_CODE_H__