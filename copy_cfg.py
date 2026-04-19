import tkinter as tk
from tkinter import messagebox, filedialog
from tkinter.scrolledtext import ScrolledText
import shutil
import os
from datetime import datetime

LIST_FILE = "backup_list.txt"

def create_example_list():
    example_text = """# Укажи название игры в квадратных скобках
[DDNet]
C:/Users/huawe/AppData/Roaming/DDNet/assets

[Rust]
D:/RustServer/cfg/ThundraCFG.txt
"""
    with open(LIST_FILE, "w", encoding="utf-8") as f:
        f.write(example_text)

def load_advanced_templates():
    if not os.path.exists(LIST_FILE):
        create_example_list()
        return {}
    
    data = {}
    current_game = None
    
    with open(LIST_FILE, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith("#"): 
                continue
            
            if line.startswith("[") and line.endswith("]"):
                current_game = line[1:-1]
                data[current_game] = []
            elif current_game:
                # Меняем обратные слеши на прямые для надежности
                line = line.replace("\\", "/")
                data[current_game].append(line)
                
    return data

def log(message, color="black"):
    log_window.configure(state='normal')
    timestamp = datetime.now().strftime("%H:%M:%S")
    log_window.insert(tk.END, f"[{timestamp}] {message}\n", color)
    log_window.see(tk.END)
    log_window.configure(state='disabled')

def select_flash_dir():
    path = filedialog.askdirectory(title="Выберите папку на флешке")
    if path:
        entry_flash.delete(0, tk.END)
        entry_flash.insert(0, path)

def run_mass_backup():
    flash_dir = entry_flash.get()

    if not flash_dir:
        log("ОШИБКА: Укажите папку на флешке!", "red")
        return

    templates = load_advanced_templates()
    if not templates:
        log(f"ОШИБКА: Файл {LIST_FILE} пуст!", "red")
        return

    date_folder = datetime.now().strftime("AllConfigs_%Y-%m-%d_%H-%M")
    final_dest = os.path.join(flash_dir, date_folder)
    
    success_count = 0
    fail_count = 0

    log("=== СТАРТ СБОРА КОНФИГОВ И ПАПОК ===", "blue")

    try:
        for game_name, item_paths in templates.items():
            if not item_paths:
                continue

            game_dest_dir = os.path.join(final_dest, game_name)
            
            for path in item_paths:
                # Отрезаем слеши на конце, если они есть, чтобы правильно получить имя
                clean_path = path.rstrip("/")
                item_name = os.path.basename(clean_path)
                
                # Если это ПАПКА
                if os.path.isdir(clean_path):
                    dest_folder = os.path.join(game_dest_dir, item_name)
                    # copytree копирует всю папку со всем содержимым
                    shutil.copytree(clean_path, dest_folder, dirs_exist_ok=True)
                    log(f"Скопирована ПАПКА: {item_name} (все файлы внутри)", "green")
                    success_count += 1
                    
                # Если это ФАЙЛ
                elif os.path.isfile(clean_path):
                    os.makedirs(game_dest_dir, exist_ok=True)
                    shutil.copy2(clean_path, os.path.join(game_dest_dir, item_name))
                    log(f"Скопирован ФАЙЛ: {item_name}", "green")
                    success_count += 1
                    
                # Если путь не существует
                else:
                    log(f"Не найден путь: {clean_path}", "red")
                    fail_count += 1

        if success_count > 0:
            log(f"=== ЗАВЕРШЕНО: Успешно: {success_count} | Ошибок: {fail_count} ===", "blue")
            messagebox.showinfo("Успех", f"Бэкап завершен!\nСохранено в: {date_folder}")
        else:
            log("Ничего не найдено. Проверь пути в backup_list.txt", "red")

    except Exception as e:
        log(f"КРИТИЧЕСКАЯ ОШИБКА: {e}", "red")

# Интерфейс
root = tk.Tk()
root.title("Mass Config & Folder Exporter")
root.geometry("600x500")

tk.Label(root, text="Сборщик конфигов и папок в один клик", font=('Arial', 12, 'bold')).pack(pady=10)

f_flash = tk.Frame(root); f_flash.pack(fill="x", padx=15)
entry_flash = tk.Entry(f_flash, font=('Arial', 10)); entry_flash.pack(side="left", expand=True, fill="x", padx=5)
tk.Button(f_flash, text="Выбрать флешку", command=select_flash_dir).pack(side="right")

tk.Button(root, text="🔥 СОБРАТЬ ВСЕ ДАННЫЕ ИЗ ШАБЛОНА", command=run_mass_backup, bg="#007bff", fg="white", height=2, font=("Arial", 11, "bold")).pack(pady=15, fill="x", padx=15)

tk.Button(root, text="📝 Открыть шаблон файлов и папок", command=lambda: os.startfile(LIST_FILE) if os.path.exists(LIST_FILE) else create_example_list() or os.startfile(LIST_FILE)).pack(pady=5)

log_window = ScrolledText(root, height=12, state='disabled', font=("Consolas", 9))
log_window.pack(fill="both", expand=True, padx=15, pady=5)
log_window.tag_config("green", foreground="green")
log_window.tag_config("red", foreground="red")
log_window.tag_config("blue", foreground="blue")

log("Программа готова. Поддерживаются и файлы, и целые папки.")
root.mainloop()