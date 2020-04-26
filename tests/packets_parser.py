class PacketParser:
    def __init__(self, path):
        self.path = path
        self.packets_in_turn = [[]] # packets_in_turn[turn]-->packets

    def parse(self):
        with open(self.path, "r") as file:
            lines = file.readlines()

        for line in lines:
            if line != "\n":
                line = line.split(',')
                packet = dict()
                for element in line:
                    element = element.split(':')
                    key = element[0]
                    value = element[1]
                    packet[key] = self.parseElement_(key, value)
                 # add the new packet to the latest turn
                self.packets_in_turn[-1].append(packet)
            else: # when found an empty line, add a new turn of packets
                self.packets_in_turn.append([])
    
    def getTurnOfPackets(self, turn):
        if not self.packets_in_turn:
            return None

        return self.packets_in_turn[turn]

    def getMaxTurns(self):
        return len(self.packets_in_turn) - 1

    @staticmethod
    def getPacketMac(packet):
        return packet["MAC_DST"]

    @staticmethod
    def getPacketSSIDLen(packet):
        return packet["SSID_LEN"]

    @staticmethod
    def getPacketSSID(packet):
        return packet["SSID"]

    @staticmethod
    def getPacketRSSI(packet):
        return packet["RSSI"]

    @staticmethod
    def getPacketTimestamp(packet):
        return packet["TIMESTAMP"]

    @staticmethod
    def getPacketChecksum(packet):
        return packet["FCS"]

    @staticmethod
    def parseElement_(key, value):
        if not value:
            return b""
        if key == "MAC_DST": # fixed size 6
            result = bytes((int(value[0:2], 16),
                            int(value[2:4], 16),
                            int(value[4:6], 16),
                            int(value[6:8], 16),
                            int(value[8:10], 16),
                            int(value[10:12], 16)))
        elif key == "SSID_LEN": # fixed size 2
            result = int(value).to_bytes(2, "big")
        elif key == "SSID": # variable size, max 33
            result = bytes(value.strip(), "utf-8")
            #result += b"0" * (33 - len(result))
        elif key == "RSSI": # fixed size 2
            result = int(value).to_bytes(2, "big", signed=True)
        elif key == "TIMESTAMP": # store as int since the device does operations on it
            result = int(value)
        elif key == "FCS": # fixed size 4
            result = int(value, 16).to_bytes(4, "big")
        else:
            raise RuntimeError(f"Key not known: {key}")

        return result
