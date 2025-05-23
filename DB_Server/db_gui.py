import socket
import json
import tkinter as tk
from tkinter import ttk, messagebox
import re

# 관리자 계정 정보
ADMIN_ID = "123"
ADMIN_PASSWORD = "123"

# MAC 주소를 하이픈 형식으로 변환 00-00-00-00
def normalize_mac_hyphen(mac: str) -> str:
    
    mac = re.sub(r'[^0-9A-Fa-f]', '', mac).upper()
    if len(mac) != 12:
        return ""
    return '-'.join([mac[i:i+2] for i in range(0, 12, 2)])

# 서버 요청 함수
def send_request(mode, payload=None):
    try:
        client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        client.connect(('127.0.0.1', 9998))
        if payload is None:
            payload = {"mode": mode}
        else:
            payload["mode"] = mode
        client.sendall(json.dumps(payload).encode())
        data = client.recv(4096).decode()
        client.close()
        return json.loads(data)
    except Exception as e:
        messagebox.showerror("서버 오류", str(e))
        return None

# 로그인 처리
def login():
    if entry_username.get() != ADMIN_ID or entry_password.get() != ADMIN_PASSWORD:
        messagebox.showerror("접근 거부", "잘못된 로그인 정보입니다.")
        return
    login_win.destroy()
    main_app()

# 로그인 창
def login_dialog():
    global entry_username, entry_password, login_win
    login_win = tk.Tk()
    login_win.title("관리자 로그인")
    tk.Label(login_win, text="관리자 ID:").grid(row=0, column=0, padx=10, pady=10)
    entry_username = tk.Entry(login_win)
    entry_username.grid(row=0, column=1, padx=10, pady=10)
    tk.Label(login_win, text="비밀번호:").grid(row=1, column=0, padx=10, pady=10)
    entry_password = tk.Entry(login_win, show='*')
    entry_password.grid(row=1, column=1, padx=10, pady=10)
    tk.Button(login_win, text="로그인", command=login).grid(row=2, column=0, columnspan=2, pady=10)
    login_win.bind('<Return>', lambda event: login())
    login_win.mainloop()

# 메인 애플리케이션 실행
def main_app():
    def load_data():
        """조회/ 서버에서 받아 테이블에 표시"""
        tree.delete(*tree.get_children())
        result = send_request("list")
        if isinstance(result, list):
            for row in result:
                tree.insert("", "end", values=(
                    row["auth_key"],  # 전체 해시값 표시
                    row["computer_mac"],
                    row["created_at"]
                ))
        else:
            msg = result.get("error", "데이터 수신 실패") if isinstance(result, dict) else "형식 오류"
            messagebox.showerror("조회 실패", msg)
        root.update()

    def register():
        """새 UID + MAC 조합 등록"""
        uid = entry_uid.get().strip()
        beacon_mac_raw = entry_beacon_mac.get().strip()
        computer_mac_raw = entry_com_mac.get().strip()

        beacon_mac = normalize_mac_hyphen(beacon_mac_raw)
        computer_mac = normalize_mac_hyphen(computer_mac_raw)

        if not uid or not beacon_mac or not computer_mac:
            messagebox.showwarning("입력 오류", "모든 항목을 정확히 입력해주세요.\nMAC 주소는 12자리 16진수여야 합니다.")
            return

        result = send_request("register", {
            "uid": uid,
            "beacon_mac": beacon_mac,
            "computer_mac": computer_mac
        })
        if result:
            messagebox.showinfo("등록 결과", result.get("status") or result.get("error"))
            load_data()

    def delete_selected():
        """선택한 등록 항목을 삭제"""
        selected = tree.selection()
        if not selected:
            messagebox.showwarning("선택 오류", "삭제할 항목을 선택하세요.")
            return
        auth_key = tree.item(selected[0])["values"][0]  # 첫 번째 열이 해시값
        result = send_request("delete_hash", {"auth_key": auth_key})
        if result:
            messagebox.showinfo("삭제 결과", result.get("status") or result.get("error"))
            load_data()

    root = tk.Tk()
    root.title("관리자 프로그램램")

    # 조회 테이블
    tree = ttk.Treeview(root, columns=("auth_key", "mac", "time"), show="headings")
    tree.heading("auth_key", text="해시값")
    tree.heading("mac", text="PC MAC")
    tree.heading("time", text="등록 시각")
    tree.column("auth_key", width=400, anchor="center")
    tree.column("mac", width=150, anchor="center")
    tree.column("time", width=200, anchor="center")
    tree.pack(padx=20, pady=10, fill="x")

    # 입력 폼
    form = tk.Frame(root)
    form.pack(padx=20, pady=10)
    tk.Label(form, text="UID").grid(row=0, column=0)
    entry_uid = tk.Entry(form)
    entry_uid.grid(row=0, column=1, padx=5)
    tk.Label(form, text="Beacon MAC").grid(row=1, column=0)
    entry_beacon_mac = tk.Entry(form)
    entry_beacon_mac.grid(row=1, column=1, padx=5)
    tk.Label(form, text="PC MAC").grid(row=2, column=0)
    entry_com_mac = tk.Entry(form)
    entry_com_mac.grid(row=2, column=1, padx=5)

    # 버튼 구성
    btn_frame = tk.Frame(root)
    btn_frame.pack(pady=10)
    tk.Button(btn_frame, text="인증 등록", command=register).pack(side="left", padx=10)
    tk.Button(btn_frame, text="선택 삭제", command=delete_selected).pack(side="left", padx=10)

    load_data()
    root.mainloop()

login_dialog()
