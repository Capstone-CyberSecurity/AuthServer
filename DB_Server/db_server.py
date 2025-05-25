import socket
import threading
import json
import pymysql
import hashlib

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
    except Exception as e:
        print(f"[DB 연결 실패] {type(e).__name__}: {str(e)}")
        return None

# 전역 DB 객체 생성
db = get_db_connection()
if not db:
    print("초기 DB 연결 실패. 프로그램 종료.")
    exit(1)

# UID + 비콘 MAC 해시 처리
def hash_uid_mac(uid: str, mac: str) -> str:
    return hashlib.sha256((uid + mac).encode()).hexdigest()

# 인증서버 연결 전용 처리 함수 (포트 9999 전용)
def handle_auth_server(sock, addr):
    print(f"[연결됨] 인증서버 {addr[0]}:{addr[1]} → 연결 유지 시작")
    try:
        while True:
            # 인증서버가 보내는 데이터를 계속 수신
            data = sock.recv(1024)

            # 수신된 데이터가 없으면 (빈 바이트), 연결 종료로 판단-> 루프 탈출ㅇ
            if not data:
                print("[종료] 인증서버 연결 종료 감지")
                break

            # 데이터가 도착한 경우: JSON 파싱 시도
            try:
                data_json = json.loads(data.decode('utf-8'))
                print(f"[수신] JSON 파싱 성공: {data_json}")
            except Exception as e:
                print(f"[오류] JSON 파싱 실패: {str(e)}")
                sock.sendall(json.dumps({"error": "JSON 파싱 실패"}).encode('utf-8'))
                continue

            # uid_mac_hash 키 값 추출 (인증 요청 해시)
            auth_key = data_json.get("uid_mac_hash", "").strip()
            print(f"[요청] 인증 해시 수신: {auth_key}")

            # 값이 비어 있으면 경고 후 무시
            if not auth_key:
                print("[경고] 인증 해시 누락")
                sock.sendall(json.dumps({"error": "해시 누락"}).encode('utf-8'))
                continue

            if not db or not db.open:
                db = get_db_connection()

            # DB에서 해당 해시값(auth_key)에 대한 컴퓨터 mac 주소(computer_mac) 조회
            with db.cursor() as cursor:
                cursor.execute("SELECT computer_mac FROM access_control WHERE auth_key = %s", (auth_key,))
                result = cursor.fetchone()

            # 매칭 성공 시 MAC 주소 전송, 실패 시 Non 응답
            if result:
                mac = result['computer_mac']
                print(f"[DB] 사용자 매칭 성공 → MAC 반환: {mac}")
                sock.sendall(mac.encode('utf-8'))
            else:
                print("[DB] 사용자 매칭 실패 → Non 반환")
                sock.sendall(b"Non")

    except Exception as e:
        print(f"[에러] 인증서버 처리 중 예외 발생: {str(e)}")
    finally:
        pass
        # 루프 종료 시 소켓 정리
        #sock.close()
        #print("[정리] 인증서버 소켓 종료 완료")

# 서버 실행 함수 (GUI용/인증서버용 포트를 나눠서 처리)
def start_server(port):
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind(('0.0.0.0', port)) 
    server_socket.listen()
    print(f"[대기 중] 포트 {port}에서 연결 대기 중...")

    while True:
        client_socket, addr = server_socket.accept()  

        threading.Thread(target=handle_client, args=(client_socket, addr, port)).start()

# 서버 실행 함수 (GUI용/인증서버용 포트를 나눠서 처리)
def start_server_auth(port):
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind(('0.0.0.0', port)) 
    server_socket.listen()
    print(f"[대기 중] 포트 {port}에서 인증서버 연결 대기 중...")
    client_socket, addr = server_socket.accept() 
    print(f"[연결됨] 인증서버 {addr[0]}:{addr[1]} → 연결 유지 시작")
    while True:
        handle_auth_server(client_socket, addr)


# GUI용(9998), 인증서버용(9999) 포트 각각 실행
threading.Thread(target=start_server, args=(9998,)).start()
threading.Thread(target=start_server_auth, args=(9999,)).start()
