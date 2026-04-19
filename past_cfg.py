import tkinter as tk
from tkinter import messagebox, filedialog, ttk
from tkinter.scrolledtext import ScrolledText
import shutil
import os
from datetime import datetime

LIST_FILE = "backup_list.txt"

def load_advanced_templates():
    if not os.path.exists(LIST_FILE):
        return {}
    data = {}
    current_game = None
    with open(LIST_FILE, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith("#"): continue
            if line.startswith("[") and line.endswith("]"):
                current_game = line[1:-1]
                data[current_game] = []
            elif current_game:
                data[current_game].append(line.replace("\\", "/"))
    return data

def log(message, color="black"):
    log_window.configure(state='normal')
    timestamp = datetime.now().strftime("%H:%M:%S")
    log_window.insert(tk.END, f"[{timestamp}] {message}\n", color)
    log_window.see(tk.END)
    log_window.configure(state='disabled')

def select_source(is_folder=False):
    """Выбор источника: либо файл, либо папка"""
    if is_folder:
        path = filedialog.askdirectory(title="Выберите ПАПКУ на флешке")
    else:
        path = filedialog.askopenfilename(title="Выберите ФАЙЛ на флешке")
    
    if path:
        entry_source.delete(0, tk.END)
        entry_source.insert(0, path)
        log(f"Источник выбран: {os.path.basename(path)}")

def run_insert():
    source_path = entry_source.get()
    selected_game = combo_games.get()
    backup_root = "C:/ConfigBackups/"

    if not source_path or not selected_game:
        log("ОШИБКА: Выберите источник и игру!", "red")
        return

    destinations = game_data.get(selected_game, [])
    success_count = 0
    
    try:
        is_source_dir = os.path.isdir(source_path)
        item_name = os.path.basename(source_path.rstrip("/"))

        for dest_path in destinations:
            # Определяем финальную точку назначения
            if os.path.isdir(dest_path) or dest_path.endswith("/"):
                final_dest = os.path.join(dest_path, item_name)
            else:
                final_dest = dest_path

            # --- ШАГ 1: БЭКАП ---
            if os.path.exists(final_dest):
                ts = datetime.now().strftime("%Y%m%d_%H%M%S")
                bp = os.path.join(backup_root, ts, selected_game)
                os.makedirs(bp, exist_ok=True)
                
                # Если на ПК по этому пути папка
                if os.path.isdir(final_dest):
                    shutil.copytree(final_dest, os.path.join(bp, item_name), dirs_exist_ok=True)
                # Если на ПК файл
                else:
                    shutil.copy2(final_dest, os.path.join(bp, item_name))
                log(f"Бэкап старого {item_name} выполнен", "blue")

            # --- ШАГ 2: ВСТАВКА (ЗАМЕНА) ---
            # Если копируем ПАПКУ
            if is_source_dir:
                if os.path.exists(final_dest):
                    shutil.rmtree(final_dest) # Удаляем старую папку перед заменой
                shutil.copytree(source_path, final_dest)
                log(f"Папка {item_name} успешно заменена в {selected_game}", "green")
            
            # Если копируем ФАЙЛ
            else:
                os.makedirs(os.path.dirname(final_dest), exist_ok=True)
                shutil.copy2(source_path, final_dest)
                log(f"Файл {item_name} успешно заменен в {selected_game}", "green")
            
            success_count += 1

        messagebox.showinfo("Успех", f"Операция завершена! Обновлено мест: {success_count}")
    except Exception as e:
        log(f"ОШИБКА: {e}", "red")

# Настройка интерфейса
game_data = load_advanced_templates()
root = tk.Tk()
root.title("Universal Inserter (Files & Folders)")
root.geometry("600x600")

# Блок выбора источника
tk.Label(root, text="1. Источник (с флешки):", font=('Arial', 9, 'bold')).pack(pady=5)
f_src = tk.Frame(root); f_src.pack(fill="x", padx=15)
entry_source = tk.Entry(f_src); entry_source.pack(side="left", expand=True, fill="x", padx=5)

# Две кнопки выбора
btn_file = tk.Button(f_src, text="Файл", command=lambda: select_source(False)).pack(side="left", padx=2)
btn_folder = tk.Button(f_src, text="Папка", command=lambda: select_source(True)).pack(side="left", padx=2)

# Выбор игры
tk.Label(root, text="2. Выберите игру:", font=('Arial', 9, 'bold')).pack(pady=10)
f_combo = tk.Frame(root); f_combo.pack(fill="x", padx=15)
combo_games = ttk.Combobox(f_combo, values=list(game_data.keys()), state="readonly")
combo_games.pack(side="left", expand=True, fill="x", padx=5)
tk.Button(f_combo, text="🔄", command=lambda: [globals().update(game_data=load_advanced_templates()), combo_games.config(values=list(game_data.keys())), log("Список обновлен", "blue")]).pack(side="right")

# Кнопка запуска
tk.Button(root, text="🔥 ЗАМЕНИТЬ ДАННЫЕ В ИГРЕ", command=run_insert, bg="#28a745", fg="white", height=2, font=("Arial", 11, "bold")).pack(pady=20, fill="x", padx=20)

# Логи
log_window = ScrolledText(root, height=15, state='disabled', font=("Consolas", 9))
log_window.pack(fill="both", expand=True, padx=15, pady=10)
log_window.tag_config("green", foreground="green"); log_window.tag_config("red", foreground="red"); log_window.tag_config("blue", foreground="blue")

log("Скрипт готов. Выберите файл ИЛИ папку на флешке.")
root.mainloop()