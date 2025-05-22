import socket
import threading
import pymysql
import json
import struct
from cryptography.hazmat.primitives.asymmetric import rsa, padding
from cryptography.hazmat.primitives import serialization, hashes
from cryptography.hazmat.primitives.ciphers.aead import AESGCM

# DB 연결
db = pymysql.connect(
    host='ajsj123.iptime.org',
    port=3306,
    user='parksubin',
    password='qkr!tnqls',
    database='nfc_lock_db',
    charset='utf8mb4',
    cursorclass=pymysql.cursors.DictCursor
)

# 최신 uid 값 저장
latest_ats = "new ats"

# 바이너리 패킷 구조
class PacketType:
    LOGIN = 0x0001
    LOGIN_OK = 0x0002
    KEY = 0x0011
    UID = 0x0013
    CONNECT = 0x0020

class Packet:
    def __init__(self, packet_type, iv=None, tag=None, data=None):
        self.packet_type = packet_type
        self.iv = iv if iv is not None else bytearray(12)
        self.tag = tag if tag is not None else bytearray(16)
        self.data = data if data is not None else bytearray()

    def to_bytes(self):
        parts = [
            struct.pack("<I", self.packet_type),
            struct.pack("<I", len(self.iv)) + self.iv,
            struct.pack("<I", len(self.tag)) + self.tag,
            struct.pack("<I", len(self.data)) + self.data,
        ]
        return b''.join(parts)

    @staticmethod
    def from_bytes(buffer):
        offset = 0
        packet_type = struct.unpack("<I", buffer[offset:offset + 4])[0]
        offset += 4

        def read_chunk():
            nonlocal offset
            length = struct.unpack("<I", buffer[offset:offset + 4])[0]
            offset += 4
            chunk = buffer[offset:offset + length]
            offset += length
            return chunk

        iv = read_chunk()
        tag = read_chunk()
        data = read_chunk()
        return Packet(packet_type, iv, tag, data)

class Crypto:
    def __init__(self):
        self.rsa = rsa.generate_private_key(public_exponent=65537, key_size=2048)
        self.aes_key = None

    def rsa_decrypt(self, ciphertext):
        return self.rsa.decrypt(
            ciphertext,
            padding.OAEP(mgf=padding.MGF1(algorithm=hashes.SHA256()), algorithm=hashes.SHA256(), label=None)
        )

    def aes_decrypt(self, iv, ciphertext, tag):
        aesgcm = AESGCM(self.aes_key)
        return aesgcm.decrypt(iv, ciphertext + tag, None)

    def set_aes_key(self, key):
        self.aes_key = key

    def get_public_key_bytes(self):
        return self.rsa.public_key().public_bytes(
            encoding=serialization.Encoding.DER,
            format=serialization.PublicFormat.SubjectPublicKeyInfo
        )


def handle_client(client_socket, addr, port):
    global latest_ats
    print(f"[접속됨] 클라이언트: {addr[0]}:{addr[1]} 포트: {port}")
    crypto = Crypto() if port == 39990 else None

    #인증서버용 포트 39990
    
    try:
        if port == 39990:
            data = client_socket.recv(1024)
            if data == b"REQUEST_PUBLIC_KEY":
                public_key = crypto.get_public_key_bytes()
                client_socket.sendall(public_key)
                return

            length = struct.unpack("!I", data[:4])[0]
            data = data[4:length + 4] if len(data) >= length + 4 else client_socket.recv(length)

            try:
                data_json = json.loads(data.decode('utf-8'))
                if data_json.get("source") == "auth_server":
                    user_uid = data_json.get("user_uid", "").strip()
                    print(f"[JSON ATS 확인] UID: {user_uid}")
                    with db.cursor() as cursor:
                        cursor.execute("SELECT * FROM users WHERE user_uid = %s", (user_uid,))
                        result = cursor.fetchone()
                    if result:
                        print(f"[ATS 유효] 사용자: {result.get('name')}")
                        response = json.dumps({
                            "valid": True,
                            "user_uid": user_uid,
                            "name": result["name"],
                            "access_level": result["access_level"]
                        })
                    else:
                        print("[ATS 무효] 사용자 없음")
                        response = json.dumps({
                            "valid": False,
                            "user_uid": user_uid,
                            "error": "사용자 없음"
                        })
                    latest_ats = user_uid
                    print(f"[ATS 갱신] {latest_ats}")
                    client_socket.sendall(response.encode('utf-8'))
                    return
            except (json.JSONDecodeError, UnicodeDecodeError):
                packet = Packet.from_bytes(data)

                if packet.packet_type == PacketType.LOGIN:
                    print(f"[LOGIN] 장치 로그인 시도")
                    response_packet = Packet(PacketType.LOGIN_OK)
                    response_bytes = response_packet.to_bytes()
                    client_socket.sendall(struct.pack("!I", len(response_bytes)) + response_bytes)
                    return

                elif packet.packet_type == PacketType.KEY:
                    aes_key = crypto.rsa_decrypt(packet.data)
                    crypto.set_aes_key(aes_key)
                    print("[KEY] AES 키 수신")
                    response = json.dumps({"status": "key_received"})
                    client_socket.sendall(response.encode('utf-8'))
                    return

                elif packet.packet_type == PacketType.UID:
                    user_uid = crypto.aes_decrypt(packet.iv, packet.data, packet.tag).decode('utf-8').strip()
                    print(f"[패킷 ATS 확인] UID: {user_uid}")
                    with db.cursor() as cursor:
                        cursor.execute("SELECT * FROM users WHERE user_uid = %s", (user_uid,))
                        result = cursor.fetchone()
                    if result:
                        print(f"[ATS 유효] 사용자: {result.get('name')}")
                        response = json.dumps({
                            "valid": True,
                            "user_uid": user_uid,
                            "name": result["name"],
                            "access_level": result["access_level"]
                        })
                    else:
                        print("[ATS 무효] 사용자 없음")
                        response = json.dumps({
                            "valid": False,
                            "user_uid": user_uid,
                            "error": "사용자 없음"
                        })
                    latest_ats = user_uid
                    print(f"[ATS 갱신] {latest_ats}")
                    client_socket.sendall(response.encode('utf-8'))

                    # CONNECT 패킷 전송
                    connect_packet = Packet(PacketType.CONNECT)
                    connect_bytes = connect_packet.to_bytes()
                    client_socket.sendall(struct.pack("!I", len(connect_bytes)) + connect_bytes)
                    return

        #관리프로그램용 포트 9998
        else:
            data = client_socket.recv(1024).decode('utf-8')
            if not data:
                return
            try:
                data_json = json.loads(data)
            except json.JSONDecodeError as e:
                print(f"[에러] JSON 파싱 실패: {e}")
                response = json.dumps({"error": "잘못된 JSON 형식"})
                client_socket.sendall(response.encode('utf-8'))
                return

            if data_json.get("request") == "new_nfc":
                print("[요청] 최신 ATS 반환")
                response = json.dumps({"user_uid": latest_ats})
            else:
                user_uid = data_json.get("user_uid", "").strip()
                print(f"[인증 시도] UID: {user_uid}")
                with db.cursor() as cursor:
                    cursor.execute("SELECT * FROM users WHERE user_uid = %s", (user_uid,))
                    result = cursor.fetchone()
                if result:
                    print(f"[인증 성공] 사용자: {result.get('name')}")
                    response = json.dumps({
                        "user_uid": result["user_uid"],
                        "name": result["name"],
                        "access_level": result["access_level"]
                    })
                else:
                    print("[인증 실패] 사용자 없음")
                    response = json.dumps({"user_uid": "none", "error": "사용자 없음"})

            client_socket.sendall(response.encode('utf-8'))

    except Exception as e:
        print(f"[에러] 클라이언트 처리 중 오류: {e}")
        response = json.dumps({"error": "서버 내부 오류"})
        client_socket.sendall(response.encode('utf-8'))
    finally:
        client_socket.close()

def start_server(port):
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind(('0.0.0.0', port))
    server_socket.listen()
    print(f"[대기 중] DB 서버가 포트 {port}에서 클라이언트를 기다립니다.")
    while True:
        client_socket, addr = server_socket.accept()
        threading.Thread(target=handle_client, args=(client_socket, addr, port)).start()

if __name__ == "__main__":
    threading.Thread(target=start_server, args=(39990,)).start() #인증서버
    threading.Thread(target=start_server, args=(9998,)).start() #관리 프로그램
