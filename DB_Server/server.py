import socket
import threading
import pymysql
import json
import hashlib
from datetime import datetime

# DB 연결 함수
def get_db_connection():
    try:
        return pymysql.connect(
            host='ajsj123.iptime.org',
            port=3306,
            user='parksubin',
            password='qkr!tnqls',
            database='nfc_lock_db',
            charset='utf8mb4',
            cursorclass=pymysql.cursors.DictCursor
        )
    except:
        return None

# 전역 DB 객체 생성
db = get_db_connection()
if not db:
    exit(1)  # DB 연결 실패 시 서버 종료

# UID + MAC → 해시로 변환
def hash_uid_mac(uid: str, mac: str) -> str:
    return hashlib.sha256((uid + mac).encode()).hexdigest()

# 클라이언트 요청 처리
def handle_client(client_socket, addr, port):
    global db
    try:
        # DB 재연결 처리
        if not db or not db.open:
            db = get_db_connection()
            if not db:
                client_socket.sendall(json.dumps({"error": "DB 연결 실패"}).encode())
                return

        data = client_socket.recv(1024)
        data_json = json.loads(data.decode())

        # 관리프로그램용 포트: 등록/삭제/조회
        if port == 9998:
            mode = data_json.get("mode")
            uid = data_json.get("uid", "").strip()
            beacon_mac = data_json.get("beacon_mac", "").strip()
            computer_mac = data_json.get("computer_mac", "").strip()
            auth_key = hash_uid_mac(uid, beacon_mac)

            if mode == "register":
                # UID + Beacon MAC을 해시하여 등록
                if not uid or not beacon_mac or not computer_mac:
                    client_socket.sendall(json.dumps({"error": "입력 누락"}).encode())
                    return
                with db.cursor() as cursor:
                    cursor.execute("SELECT id FROM access_control WHERE auth_key = %s", (auth_key,))
                    if cursor.fetchone():
                        response = {"error": "이미 등록된 해시입니다."}
                    else:
                        cursor.execute(
                            "INSERT INTO access_control (auth_key, computer_mac) VALUES (%s, %s)",
                            (auth_key, computer_mac)
                        )
                        db.commit()
                        response = {"status": "등록 완료"}
                client_socket.sendall(json.dumps(response).encode())

            elif mode == "delete":
                # UID + Beacon MAC 해시로 삭제
                if not uid or not beacon_mac:
                    client_socket.sendall(json.dumps({"error": "입력 누락"}).encode())
                    return
                with db.cursor() as cursor:
                    cursor.execute("DELETE FROM access_control WHERE auth_key = %s", (auth_key,))
                    db.commit()
                client_socket.sendall(json.dumps({"status": "삭제 완료"}).encode())

            elif mode == "delete_hash":
                # 해시 키 직접 전달받아 삭제
                auth_key = data_json.get("auth_key", "").strip()
                if not auth_key:
                    client_socket.sendall(json.dumps({"error": "해시값 누락"}).encode())
                    return
                with db.cursor() as cursor:
                    cursor.execute("DELETE FROM access_control WHERE auth_key = %s", (auth_key,))
                    db.commit()
                client_socket.sendall(json.dumps({"status": "삭제 완료"}).encode())

            elif mode == "list":
                # 전체 등록 목록 반환
                with db.cursor() as cursor:
                    cursor.execute("SELECT auth_key, computer_mac, created_at FROM access_control")
                    rows = cursor.fetchall()
                # 가독성 좋게 auth_key 일부만 표시, 시간 문자열 처리
                for row in rows:
                    row["auth_key_display"] = f"{row['auth_key'][:6]}...{row['auth_key'][-4:]}"
                    if row.get("created_at"):
                        row["created_at"] = str(row["created_at"])
                client_socket.sendall(json.dumps(rows).encode())

            else:
                client_socket.sendall(json.dumps({"error": "지원하지 않는 모드"}).encode())

        # 인증서버용 포트
        elif port == 9999:
            auth_key = data_json.get("uid_mac_hash", "").strip()
            print("AUTH: " + auth_key)
            if not auth_key:
                client_socket.sendall(json.dumps({"error": "필수 값 누락"}).encode())
                return
            with db.cursor() as cursor:
                cursor.execute("SELECT computer_mac FROM access_control WHERE auth_key = %s", (auth_key,))
                result = cursor.fetchone()
            client_socket.sendall(result["computer_mac"].encode() if result else b"Non")

    except Exception as e:
        client_socket.sendall(json.dumps({"error": f"서버 오류: {str(e)}"}).encode())

    finally:
        client_socket.close()

def start_server(port):
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind(('0.0.0.0', port))
    server_socket.listen()
    print(f"서버가 포트 {port}에서 실행 중입니다.")
    while True:
        client_socket, addr = server_socket.accept()
        threading.Thread(target=handle_client, args=(client_socket, addr, port)).start()

# GUI용(9998), 인증서버용(9999) 포트 각각 실행
for port in [9998, 9999]:
    threading.Thread(target=start_server, args=(port,)).start()
