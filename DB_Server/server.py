import socket
import pymysql
import json

# DB 연결결
db = pymysql.connect(
    #host='ajsj123.iptime.org',
    host='192.168.10.4',
    port=3306,
    user='parksubin',
    password='qkr!tnqls',
    database='nfc_lock_db',
    charset='utf8mb4',
    cursorclass=pymysql.cursors.DictCursor
)

latest_ats = "new ats"

HOST = '0.0.0.0'
PORT = 9999

server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server_socket.bind((HOST, PORT))
server_socket.listen()

print(f"[대기 중] DB 서버가 포트 {PORT}에서 클라이언트를 기다립니다.")

while True:
    client_socket, addr = server_socket.accept()
    print(f"[접속됨] 클라이언트: {addr[0]}:{addr[1]}")

    try:
        data = client_socket.recv(1024).decode('utf-8')
        data_json = json.loads(data)

        print(f"[수신됨] 데이터: {data_json}")

        # 인증 서버가 보낸 최신 ATS 값 수신
        if data_json.get("source") == "auth_server":
            latest_ats = data_json.get("user_uid", latest_ats)
            print(f"[ATS 갱신] {latest_ats}")
            response = json.dumps({"status": "received"})

        # GUI에서 새로운 ATS 요청 시
        elif data_json.get("request") == "new_nfc":
            print("[요청] 최신 ATS 반환")
            response = json.dumps({"user_uid": latest_ats})

        # 일반 사용자 인증 요청
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

    finally:
        client_socket.close()
