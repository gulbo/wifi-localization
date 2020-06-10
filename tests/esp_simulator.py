import json
import socket
import time
from packets_parser import PacketParser

class Board:
    PROTO_CODE_LEN = 2
    PROTO_CODE_HI = "HI"
    PROTO_CODE_GO = "GO"

    def __init__(self, id, server_ip, server_port):
        self.id = id
        self.mac = bytes((0,0,0,0,0,id))
        self.server_ip = server_ip
        self.server_port = server_port
        self.socket = None
        self.timestamp = 0
        self.debug_(f"New device. MAC: {self.mac}")


    def __del__(self):
        self.debug_("Socket closed")
        if self.socket is not None:
            self.socket.close()

    def connect(self):
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.socket.connect((self.server_ip, self.server_port))
        self.debug_("Socket connected")

    def sendHI(self):
        if self.socket is None:
            raise RuntimeError(self.debug_(f"{self.PROTO_CODE_HI} error! Not connected!"))
        
        code = self.PROTO_CODE_HI.encode(encoding='ASCII')
        message = code + bytes((0,0,0,self.id)) + self.mac
        self.socket.sendall(message)
        self.debug_(f"Sent {message}")

    def receiveHI(self):
        if self.socket is None:
            raise RuntimeError(self.debug_(f"{self.PROTO_CODE_HI} error! Not connected!"))
        
        code = self.getMsgCode_()
        if code != self.PROTO_CODE_HI:
            raise RuntimeError(self.debug_(f"{self.PROTO_CODE_HI} error! received {code}"))
        
        self.debug_(f"Received {code}")

    def debug_(self, message):
        debug_message = f"Board{self.id} {message}"
        print(debug_message)
        return debug_message

    def getMsgCode_(self):
        chunks = bytearray()
        bytes_received = 0
        while bytes_received < self.PROTO_CODE_LEN:
            chunk = self.socket.recv(min(self.PROTO_CODE_LEN - bytes_received, 2))
            if chunk == b'':
                raise RuntimeError(self.debug_("Socket connection broken"))
            chunks = chunks + chunk
            bytes_received = bytes_received + len(chunk)
        return chunks.decode(encoding='utf-8')

    def receiveInt_(self):
        chunks = bytearray()
        bytes_received = 0
        while bytes_received < 4: # int = 4bytes
            chunk = self.socket.recv(min(4 - bytes_received, 2))
            if chunk == b'':
                raise RuntimeError(self.debug_("Socket connection broken"))
            chunks = chunks + chunk
            bytes_received = bytes_received + len(chunk)
        return int.from_bytes(chunks, byteorder='big') #the network is big endian

    def receiveGO(self):
        if self.socket is None:
            raise RuntimeError(self.debug_(f"{self.PROTO_CODE_GO} error! Not connected!"))
        
        code = self.getMsgCode_()
        if code != self.PROTO_CODE_GO:
            raise RuntimeError(self.debug_(f"{self.PROTO_CODE_GO} error! received {code}"))

        self.timestamp = self.receiveInt_()
        self.debug_(f"Received {code}{self.timestamp}")

    def receiveTimestamp(self):
        if self.socket is None:
            raise RuntimeError(self.debug_(f"receive timestamp error! Not connected!"))
        self.timestamp = self.receiveInt_()
        self.debug_(f"Received timestamp {self.timestamp}")
    
    def sendPackets(self, packets):
        if self.socket is None:
            raise RuntimeError(self.debug_(f"SendPackets error! Not connected!"))

        # first send IDDevice and #packets
        self.socket.sendall(bytes((0,0,0,self.id)))
        self.socket.sendall(len(packets).to_bytes(4, "big"))

        turn_timestamp = 0
        for packet in packets:
            # adapt the timestamp to the one given by the server
            if turn_timestamp == 0:
                turn_timestamp = packet_parser.getPacketTimestamp(packet)
            new_timestamp = packet_parser.getPacketTimestamp(packet) - turn_timestamp + self.timestamp

            # send in order MAC RSSI SSIDLEN SSID TIMESTAMP CHECKSUM
            self.socket.sendall(packet_parser.getPacketMac(packet))
            self.socket.sendall(packet_parser.getPacketRSSI(packet))
            self.socket.sendall(packet_parser.getPacketSSIDLen(packet))
            self.socket.sendall(packet_parser.getPacketSSID(packet))
            self.socket.sendall(new_timestamp.to_bytes(4, "big"))
            self.socket.sendall(packet_parser.getPacketChecksum(packet))
        self.debug_(f"Packets sent")

devices = list()
with open('config.json') as config_file:
    config = json.load(config_file)
    for device_id in range(1, config['num_boards']+1):
        devices.append(Board(device_id,
                             config['server_ip'],
                             config['server_port']))

# connect
for device in devices:
    device.connect()

# send HI
for device in devices:
    device.sendHI()

# receive HI
for device in devices:
    device.receiveHI()

# receive GO
for device in devices:
    device.receiveGO()

packet_parser = PacketParser("packets.csv")
packet_parser.parse()
sending_turn = 0
while True:
    print(f"TURN {sending_turn}")

    # send packets
    for device in devices:
        packets = packet_parser.getTurnOfPackets(sending_turn)
        device.sendPackets(packets)

    # receive timestamp
    for device in devices:
        device.receiveTimestamp()

    # sleep before the next turn
    print("sleep")
    for i in range(1,config["sleep_rate"]+1):
        time.sleep(1)
        print(i, end="...", flush=True)
        print("")
        
    # next turn
    sending_turn += 1
    # if reaching the end of the file, start from the beginning
    if sending_turn > packet_parser.getMaxTurns():
        sending_turn = 0


    

