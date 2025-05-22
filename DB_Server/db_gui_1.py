import socket
import json
import tkinter as tk
from tkinter import ttk, messagebox
import pymysql
import datetime

# 관리자 계정 정보
ADMIN_ID = "123"
ADMIN_PASSWORD = "123"

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

# 로그인 처리
def login():
    username = entry_username.get()
    password = entry_password.get()
    if username != ADMIN_ID or password != ADMIN_PASSWORD:
        messagebox.showerror("접근 거부", "잘못된 로그인 정보입니다.")
        return
    login_win.destroy()
    main_app()

# 로그인 GUI
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

    login_button = tk.Button(login_win, text="로그인", command=login)
    login_button.grid(row=2, column=0, columnspan=2, pady=10)

    login_win.bind('<Return>', lambda event: login())

    login_win.mainloop()

# NFC 태그 읽기 함수
def read_nfc_tag():
    try:
        client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        client.connect(('localhost', 9998))  # 관리 프로그램용 포트 9998
        client.sendall(json.dumps({"request": "new_nfc"}).encode())
        data = client.recv(1024).decode()
        response = json.loads(data)
        client.close()
        if "user_uid" in response and response["user_uid"] != "none":
            entry_uid.delete(0, tk.END)
            entry_uid.insert(0, response["user_uid"])
        else:
            messagebox.showerror("오류", "NFC 태그 정보를 읽을 수 없습니다.")
    except Exception as e:
        messagebox.showerror("오류", f"서버 연결 실패: {e}")

# 사용자 목록 가져오기
def fetch_users():
    with db.cursor() as cursor:
        cursor.execute("SELECT * FROM users")
        return cursor.fetchall()

# 테이블 새로고침
def refresh_table():
    for row in tree.get_children():
        tree.delete(row)
    for user in fetch_users():
        tree.insert('', 'end', values=(
            user['id'],
            user['name'],
            user['user_uid'],
            user['access_level'],
            user['registered_at']
        ))

# 사용자 선택 시 입력란 채우기
def on_user_select(event):
    selected = tree.focus()
    if not selected:
        return
    values = tree.item(selected, 'values')

    entry_name.delete(0, tk.END)
    entry_name.insert(0, values[1])

    entry_uid.delete(0, tk.END)
    entry_uid.insert(0, values[2])

    entry_level.delete(0, tk.END)
    entry_level.insert(0, str(values[3]))

# 사용자 정보 수정
def update_user():
    selected = tree.focus()
    if not selected:
        messagebox.showwarning("경고", "수정할 사용자를 선택하세요.")
        return
    user_id = tree.item(selected, 'values')[0]

    name = entry_name.get()
    uid = entry_uid.get()
    access = entry_level.get()

    try:
        access = int(access)
    except ValueError:
        messagebox.showerror("오류", "권한 등급은 숫자여야 합니다.")
        return

    if not name or not uid or uid == "none":
        messagebox.showwarning("입력 오류", "유효한 이름과 ATS를 입력해주세요.")
        return

    with db.cursor() as cursor:
        sql = "UPDATE users SET name=%s, user_uid=%s, access_level=%s WHERE id=%s"
        cursor.execute(sql, (name, uid, access, user_id))
    db.commit()
    refresh_table()
    messagebox.showinfo("성공", "사용자 정보가 수정되었습니다.")

# 사용자 삭제
def delete_user():
    selected = tree.focus()
    if not selected:
        messagebox.showwarning("경고", "삭제할 사용자를 선택하세요.")
        return
    user_id = tree.item(selected, 'values')[0]

    confirm = messagebox.askyesno("확인", "정말로 이 사용자를 삭제하시겠습니까?")
    if not confirm:
        return

    with db.cursor() as cursor:
        sql = "DELETE FROM users WHERE id=%s"
        cursor.execute(sql, (user_id,))
    db.commit()
    refresh_table()
    messagebox.showinfo("성공", "사용자가 삭제되었습니다.")

# 사용자 추가
def add_user():
    name = entry_name.get().strip()
    uid = entry_uid.get().strip()
    level = entry_level.get().strip()

    if not name or not uid or not level or uid == "none":
        messagebox.showwarning("입력 오류", "유효한 이름, ATS, 권한을 입력해주세요.")
        return

    try:
        level = int(level)
    except ValueError:
        messagebox.showwarning("입력 오류", "접근 권한은 숫자로 입력해주세요.")
        return

    try:
        with db.cursor() as cursor:
            cursor.execute("SELECT id FROM users WHERE user_uid = %s", (uid,))
            if cursor.fetchone():
                messagebox.showerror("오류", "이미 등록된 ATS입니다.")
                return
            sql = "INSERT INTO users (name, user_uid, access_level, registered_at) VALUES (%s, %s, %s, %s)"
            cursor.execute(sql, (name, uid, level, datetime.datetime.now()))
            db.commit()
            messagebox.showinfo("성공", "사용자 추가 완료!")
            entry_name.delete(0, tk.END)
            entry_uid.delete(0, tk.END)
            entry_level.delete(0, tk.END)
            refresh_table()
    except Exception as e:
        messagebox.showerror("DB 오류", str(e))

# 메인 GUI
def main_app():
    global root, tree, entry_name, entry_uid, entry_level

    root = tk.Tk()
    root.title("사용자 DB 관리 프로그램")

    columns = ('id', 'name', 'uid', 'access_level', 'registered_at')
    tree = ttk.Treeview(root, columns=columns, show='headings', height=10)
    tree.pack(pady=10)

    tree.heading('id', text='ID')
    tree.heading('name', text='이름')
    tree.heading('uid', text='ATS')
    tree.heading('access_level', text='권한')
    tree.heading('registered_at', text='등록일')

    tree.column('id', width=50, anchor='center')
    tree.column('name', width=100, anchor='center')
    tree.column('uid', width=150, anchor='center')
    tree.column('access_level', width=60, anchor='center')
    tree.column('registered_at', width=180, anchor='center')

    tree.bind('<<TreeviewSelect>>', on_user_select)

    frame = tk.Frame(root)
    frame.pack()

    tk.Label(frame, text="이름").grid(row=0, column=0)
    entry_name = tk.Entry(frame)
    entry_name.grid(row=0, column=1)

    tk.Label(frame, text="ATS").grid(row=1, column=0)
    entry_uid = tk.Entry(frame)
    entry_uid.grid(row=1, column=1)

    tk.Label(frame, text="접근 권한").grid(row=2, column=0)
    entry_level = tk.Entry(frame)
    entry_level.grid(row=2, column=1)

    tk.Button(root, text="사용자 추가", command=add_user).pack(pady=5)
    tk.Button(root, text="사용자 수정", command=update_user).pack(pady=5)
    tk.Button(root, text="사용자 삭제", command=delete_user).pack(pady=5)
    tk.Button(root, text="NFC 태그 읽기", command=read_nfc_tag).pack(pady=5)
    tk.Button(root, text="새로고침", command=refresh_table).pack(pady=5)

    refresh_table()
    root.mainloop()

login_dialog()