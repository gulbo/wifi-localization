#include "protocol_code.h"

#include "iostream"

const std::string ProtocolCode::HI = "HI";
const std::string ProtocolCode::OK = "OK";
const std::string ProtocolCode::RT = "RT";
const std::string ProtocolCode::DE = "DE";
const std::string ProtocolCode::GO = "GO";

bool ProtocolCode::isValid(const std::string& code)
{
    if (code.length() != CODE_LENGTH)
        return false;

    return (code == HI || code == OK || code == RT || code == DE || code == GO);
}
