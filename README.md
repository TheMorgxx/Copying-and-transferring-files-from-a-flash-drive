Language : Russian
📂 Config Manager (Export & Import)
Универсальный инструмент на Python для быстрого резервного копирования и восстановления игровых конфигов (файлов и папок) с использованием флешки.

🌟 Основные функции
Массовый экспорт: Собирает все указанные в шаблоне файлы и папки в одну директорию на флешке с меткой времени.

Массовый импорт: Рассылает обновленный файл или папку с флешки по всем путям, указанным в шаблоне для конкретной игры.

Авто-бэкап: Перед заменой старых данных скрипт автоматически сохраняет их в C:/ConfigBackups/.

Универсальный шаблон: Один файл backup_list.txt управляет всеми путями.

🛠 Инструкция по настройке (backup_list.txt)
Создайте файл backup_list.txt в корневой папке проекта. Используйте следующий формат:

Plaintext
[Название игры]
C:/Путь/К/Файлу/config.cfg
C:/Путь/К/Папке/с/ассетами
Пример:

Plaintext
[DDNet]
C:/Users/Admin/AppData/Roaming/DDNet/assets
C:/Users/Admin/AppData/Roaming/DDNet/settings.cfg

[Rust]
D:/Steam/steamapps/common/Rust/cfg/client.cfg
🚀 Как использовать
Убедитесь, что установлен Python (версия 3.6+).

Для сбора данных на флешку: Запустите скрипт экспорта, выберите папку на флешке и нажмите "Собрать".

Для переноса данных в игру: Запустите скрипт импорта, выберите нужный файл/папку на флешке, выберите игру из списка и нажмите "Заменить".

Совет: Чтобы запускать скрипты без появления черного окна консоли, смените расширение файлов с .py на .pyw.

📂 Структура проекта
export_tool.py — скрипт для сбора файлов на флешку.

import_tool.py — скрипт для рассылки файлов в папки игр.

backup_list.txt — ваш персональный список путей.

⚠️ Важное примечание
Если вы используете Portable Python (embeddable), убедитесь, что в нем установлена библиотека tkinter, иначе графический интерфейс не запустится. Рекомендуется использовать стандартную установку Python с официального сайта.

📝 Лицензия
Свободное использование для личных целей.

Language : English
Here is a solid, professional README.md in English, optimized for GitHub. It includes a clear structure, installation instructions, and usage tips.

📂 Config & Folder Management Tool
A universal Python-based solution for gamers and power users to quickly export and import game configurations, scripts, and assets between a PC and a flash drive.

🌟 Key Features
Mass Export: Collects multiple files and folders into a timestamped directory on your flash drive with one click.

Mass Import/Insertion: Automatically distributes files/folders from your flash drive to all local paths defined in your template.

Automatic Backups: Before overwriting any data on your PC, the tool creates a safety backup in C:/ConfigBackups/.

Unified Template: Manage all your paths in a single, easy-to-edit backup_list.txt file.

Directory Support: Handles both individual files and entire folders (e.g., assets, mods, maps).

🛠 Setup & Configuration
1. Requirements
Python 3.x (Standard installation recommended).

Tkinter (Included in standard Python; required for the GUI).

2. The Template (backup_list.txt)
Create a file named backup_list.txt in the root directory. Use square brackets for the Game/Category name and list the full paths below it.

Example:

Plaintext
[DDNet]
C:/Users/YourName/AppData/Roaming/DDNet/assets
C:/Users/YourName/AppData/Roaming/DDNet/settings.cfg

[Rust]
D:/Games/Rust/cfg/ThundraCFG.txt

[Terraria]
C:/Users/YourName/Documents/My Games/Terraria/ModLoader/Mods
🚀 How to Use
Exporting (Saving to Flash Drive)
Run the Export Tool.

Select your flash drive's destination folder.

Click "Collect All Data".

The tool will copy every file/folder listed in your template into a new folder named AllConfigs_YYYY-MM-DD.

Importing (Updating your PC)
Run the Import Tool.

Select the file or folder on your flash drive you want to transfer.

Select the target Game/Category from the dropdown list.

Click "Replace Data".

The tool will backup the old files and replace them with the new ones.

📂 Project Structure
export_script.py — The GUI for gathering files onto your flash drive.

import_script.py — The GUI for distributing files from your flash drive to your PC.

backup_list.txt — Your personal database of file paths.

💡 Pro Tips
Run without Console: Rename your scripts from .py to .pyw to launch the GUI without the black terminal window.

Portable Use: While you can use a portable Python version, ensure it includes tkinter. Standard Python installation is recommended for the best experience.

Slashes: Always use forward slashes (/) or double backslashes (\\) in your backup_list.txt to avoid path errors.

📝 License
Free to use and modify for personal use. Feel free to contribute or adapt it for your own gaming setup!
