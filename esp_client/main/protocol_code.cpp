#include "protocol_code.h"

#include "iostream"

const std::string ProtocolCode::HI = "HI";
const std::string ProtocolCode::GO = "GO";

bool ProtocolCode::isValid(const std::string& code)
{
    if (code.length() != CODE_LENGTH)
        return false;

    return (code == HI || code == GO);
}
