# ConfigBackupTool — архитектура (в разработке)

## Статус
Реализовано: слой путей (нормализация + резолв + известные корни), domain-модели манифеста,
GameDefinition/PathEntry (домен списка игр для бэкапа), RestoreMode, IEncryptionService/
AesEncryptionService (AES-256, PBKDF2, встроенная сверка SHA256 после шифрования),
IBackupManifestRepository/JsonManifestRepository (шифрование manifest.json целиком через
IEncryptionService), IBackupManager/BackupManager (оркестрация экспорта), IRestoreManager/
RestoreManager + IRestoreConflictResolver (оркестрация восстановления), IGameDefinitionRepository/
JsonGameDefinitionRepository (games.json рядом с exe, список игр для Export — снимает блокер
для ExportForm), живой построчный прогресс в BackupManager.Export/RestoreManager.Import через
необязательный IProgress<string>, composition root (App/Program.cs) + MainForm (тонкий хаб,
3 кнопки), ExportForm (чекбоксы игр, FolderBrowserDialog, пароль дважды, живой лог,
Task.Run + IProgress) — подключена к MainForm, кнопка "Экспорт" открывает её по-настоящему,
ImportForm + три модальных диалога (AmbiguityForm/ManualRootForm/ConflictForm) и
WinFormsRestoreConflictResolver — подключена к MainForm, кнопка "Восстановление" открывает её
по-настоящему.
Появился ConfigBackupTool.csproj — сам проект теперь WinForms-приложение (net8.0-windows,
UseWindowsForms), а не только переиспользуемая библиотека без точки входа.
SettingsForm (шаг 6) реализована и подключена к MainForm: две вкладки — "Известные корни"
(known_roots.json через IKnownRootsRepository) и "Игры" (games.json через
IGameDefinitionRepository, включая список путей на игру). Правки живут в памяти рабочих
копий списков до явного нажатия "Сохранить"; закрытие с несохранёнными правками (крестиком
или кнопкой "Закрыть") спрашивает через MessageBox, сохранить ли их — тот же принцип
"не терять данные молча", что и в остальных формах. Три вспомогательных модальных диалога:
TextInputForm (общий, одна строка ввода — имя игры при добавлении/переименовании),
KnownRootEditForm (Marker + RootPath с "Обзор..." на папку), PathEntryEditForm (путь файла
или папки, "Обзор..." на оба варианта плюс ручной ввод — нужен для путей, которых пока нет
на этой машине).
Не реализовано: ICredentialStore (DPAPI, "запомнить пароль на этом ПК") — следующий шаг.

## Структура

```
ConfigBackupTool/                          — основная библиотека (класть в WinForms-проект)
  Domain/
    BackupItem.cs                          — модель элемента бэкапа (AnchorType, BackupEntryType, BackupItem)
    BackupManifest.cs                      — корень manifest.json (BackupManifest, GameManifestEntry)
    ManifestItem.cs                        — элемент манифеста: файл/папка (ManifestItem, ManifestFileEntry)
    GameDefinition.cs / PathEntry.cs       — вход для BackupManager: список игр и их путей для бэкапа
    RestoreMode.cs                         — режим восстановления: ReplaceAll / AddMissingOnly / AskOnConflict
  Paths/
    IPathNormalizer.cs / PathNormalizer.cs — абсолютный путь -> BackupItem (anchor + tail)
    IPathResolver.cs   / PathResolver.cs   — BackupItem -> абсолютный путь на текущей машине
    IKnownRootsRepository.cs               — контракт хранилища известных корней (Steam/Epic и т.п.)
    JsonKnownRootsRepository.cs            — JSON-реализация (файл known_roots.json рядом с exe)
  Encryption/
    IEncryptionService.cs / AesEncryptionService.cs — AES-256, ключ из пароля через PBKDF2,
                                            случайные Salt/IV на вызов, Encrypt сам проверяет
                                            себя (encrypt -> decrypt -> сверка SHA256), кидает
                                            InvalidOperationException при расхождении
    EncryptionResult.cs                    — DTO результата шифрования (CipherText/Salt/IV/OriginalHash, всё byte[] кроме хэша)
  Manifest/
    IBackupManifestRepository.cs / JsonManifestRepository.cs — чтение/запись manifest.json,
                                            шифрование целиком через IEncryptionService
    EncryptedManifestFile.cs               — внутренний DTO формата файла на диске: Salt/IV
                                            открытым текстом (base64) + CipherText (base64)
  Games/
    IGameDefinitionRepository.cs / JsonGameDefinitionRepository.cs — список GameDefinition для
                                            Export, games.json рядом с exe (по аналогии с
                                            JsonKnownRootsRepository). Отдельная папка, а не
                                            Domain/, — там только модели, репозитории живут в
                                            своих feature-папках (как Paths/, Manifest/).
                                            GetAll() на битом/отсутствующем файле не падает:
                                            нет файла или пустой → пустой список; JSON битый →
                                            InvalidOperationException с путём к файлу; null-
                                            коллекция Paths у отдельной игры (ручная правка
                                            games.json) — подчищается до пустого списка, чтобы
                                            ExportForm не проверял на null на каждый элемент.
  Managers/
    IBackupManager.cs / BackupManager.cs   — оркестрация экспорта: детект установки игры целиком
                                            → нормализация путей → шифрование+запись под GUID-
                                            именами (flat, без зеркалирования папок) → сборка и
                                            сохранение manifest.json. Export() возвращает List<string>
                                            лога (не отдельный ILogger). Отказ на уровне одного файла —
                                            пропуск с строкой лога, вся игра/операция продолжаются.
    IRestoreManager.cs / RestoreManager.cs — оркестрация восстановления: Load(manifest) сразу по паролю
                                            → детект присутствия игры "тихим" Resolve (auto-первый
                                            кандидат) → для DriveRelative: gameRoot — первый успешно
                                            зарезолвленный элемент (реальный ambiguityResolver), при
                                            0 кандидатов у всех DriveRelative-элементов — один вызов
                                            manualRootResolver на игру; для UserProfile/AppDataRoaming
                                            путь вычисляется напрямую (SpecialFolder+Tail), Resolve не
                                            используется (он требует существования, а цель может быть
                                            новой) → diff по хэшу на каждый файл (включая файлы внутри
                                            Directory-элементов независимо) → RestoreMode решает
                                            конфликт (ReplaceAll/AddMissingOnly/AskOnConflict через
                                            IRestoreConflictResolver) → safety-бэкап в C:/ConfigBackups/
                                            <timestamp восстановления>/<GameName>/<имя файла> перед
                                            любой заменой; если safety-бэкап не удался — файл НЕ
                                            заменяется вообще (только строка в лог, оригинал остаётся).
                                            Import() возвращает List<string> лога.
    Обе сигнатуры (Export/Import) принимают последним необязательным параметром
    IProgress<string> progress = null — при null поведение как раньше (лог только в
    возвращаемом List<string> целиком по завершении); если передан, каждая строка лога
    уходит в progress.Report(...) в момент добавления, для построчного вывода в UI по ходу
    операции (см. п.7 архитектурных решений). Внутри обоих менеджеров это реализовано через
    общий приватный хелпер Report(log, progress, message), который делает и log.Add, и
    progress?.Report — остальная логика (детект, diff, safety-бэкап, резолв корня и т.д.) не
    менялась.
    IRestoreConflictResolver.cs            — Resolve(gameName, targetPath, currentLastWriteTime,
                                            currentFileSize) -> RestoreConflictAction (Replace/Keep),
                                            вызывается только при AskOnConflict и реальном расхождении хэша
  App/
    Program.cs                             — composition root (без DI-контейнера): собирает
                                            JsonKnownRootsRepository, JsonGameDefinitionRepository,
                                            PathNormalizer, PathResolver, AesEncryptionService,
                                            JsonManifestRepository, BackupManager и передаёт их
                                            в MainForm. IRestoreManager здесь НЕ собирается —
                                            ждёт WinForms-реализацию IRestoreConflictResolver
                                            (ConflictForm, шаг 5); вместо готового RestoreManager
                                            в MainForm передаются составные части (pathResolver/
                                            encryptionService/manifestRepository), из которых
                                            ImportForm на шаге 5 сама соберёт RestoreManager.
    MainForm.cs                            — тонкий хаб, 3 кнопки (Экспорт/Восстановление/
                                            Настройки), без бизнес-логики. Собрана в коде, без
                                            отдельного Designer.cs. Все три кнопки открывают
                                            свои формы по-настоящему (Export/Import/Settings).
    SettingsForm.cs                        — настройки: вкладка "Известные корни"
                                            (known_roots.json) + вкладка "Игры" (games.json,
                                            список игр и путей на игру). Рабочие копии в памяти,
                                            запись на диск только по кнопке "Сохранить";
                                            FormClosing спрашивает про несохранённые правки.
    TextInputForm.cs                       — общий модальный диалог "заголовок + подсказка +
                                            одна строка ввода", используется для имени игры.
    KnownRootEditForm.cs                   — модальный диалог добавления/правки одного
                                            известного корня (Marker + RootPath).
    PathEntryEditForm.cs                   — модальный диалог добавления/правки одного пути
                                            игры (файл или папка, плюс ручной ввод).
    ExportForm.cs                          — диалог экспорта: CheckedListBox игр из
                                            IGameDefinitionRepository.GetAll() (сопоставление
                                            выбранных — по индексу, параллельно с _allGames, а не
                                            хранением GameDefinition в Items), FolderBrowserDialog
                                            на базовую папку (Backup_yyyyMMdd_HHmmss/ создаётся
                                            внутри неё в момент нажатия "Начать", не в момент
                                            выбора папки — таймстамп отражает реальный запуск),
                                            пароль дважды с проверкой совпадения, TextBox-лог.
                                            OnStartClick — async void, await Task.Run(() =>
                                            _backupManager.Export(...)) — continuation возвращается
                                            на UI-поток через захваченный SynchronizationContext,
                                            Progress<string> маршалит Report(...) туда же — ручной
                                            Invoke/BeginInvoke не нужен. FormClosing отменяется
                                            (e.Cancel = true), пока экспорт выполняется, чтобы
                                            Task.Run не осиротел.
    ImportForm.cs                          — диалог восстановления: FolderBrowserDialog на папку
                                            СЕССИИ бэкапа (не на файл manifest.json — он
                                            ожидается внутри неё как session/manifest.json),
                                            пароль один раз, переключатели RestoreMode, живой лог
                                            (тот же паттерн async void + Task.Run + Progress<string>,
                                            что и в ExportForm). Сама собирает RestoreManager из
                                            переданных pathResolver/encryptionService/
                                            manifestRepository + new WinFormsRestoreConflictResolver
                                            (this) — раньше это было невозможно в composition root
                                            (шаг 3), не было WinForms-реализации резолвера.
                                            ВАЖНО: ambiguityResolver (ResolveAmbiguity) и
                                            manualRootResolver (ResolveManualRoot) — синхронные
                                            делегаты, которые RestoreManager дёргает прямо из
                                            фонового Task.Run и ждёт немедленного возврата
                                            значения; Progress<T> для этого не годится (он
                                            однонаправленный), поэтому оба метода проверяют
                                            InvokeRequired и делают Invoke(...) с диалогом внутри,
                                            блокируя фоновый поток до ответа пользователя. Тот же
                                            принцип — в WinFormsRestoreConflictResolver.
    AmbiguityForm.cs                       — модальный диалог выбора из нескольких кандидатов
                                            (ListBox + Выбрать/Отмена). Автовыбор "самого нового"
                                            не делается никогда — та же гарантия, что и в
                                            PathResolver до всякого UI.
    ManualRootForm.cs                      — обёртка вокруг FolderBrowserDialog с подсказкой по
                                            GameName+Tail референсного BackupItem, когда обычный
                                            резолв дал 0 кандидатов у всех DriveRelative-элементов
                                            игры. "Пропустить игру" возвращает null — вызывающий
                                            код (RestoreManager) пропускает такие элементы с
                                            строкой в лог.
    ConflictForm.cs                        — модальный диалог при RestoreMode.AskOnConflict:
                                            показывает targetPath, currentLastWriteTime,
                                            currentFileSize, кнопки Заменить/Оставить как есть.
                                            Закрытие крестиком трактуется как Keep (то же правило
                                            "при неопределённости не заменяем", что и в
                                            safety-бэкапе).
    WinFormsRestoreConflictResolver.cs      — IRestoreConflictResolver для UI: держит ссылку на
                                            владеющий Control (ImportForm), Resolve() делает
                                            Invoke(...) и показывает ConflictForm, если вызвано не
                                            с UI-потока.
  ConfigBackupTool.csproj                  — сам проект теперь WinForms-приложение
                                            (net8.0-windows, UseWindowsForms=true, OutputType
                                            WinExe), не только библиотека классов.

ConfigBackupTool.NormalizerTest/           — консольный тест, независим от WinForms
  Program.cs
  ConfigBackupTool.NormalizerTest.csproj
```

Таргет: .NET 8. ConfigBackupTool/ теперь запускаемое WinForms-приложение (не только
переиспользуемая библиотека классов, как было до этого шага); ConfigBackupTool.NormalizerTest/ —
отдельный тестовый консольный проект, ссылается на исходники соседней папки по относительному
пути.

## Как запустить тест прямо сейчас

Единственное, что реально можно "потрогать руками" на этом этапе — консольный тест
`PathNormalizer` на вашем настоящем `backup_list.txt` (PathResolver, KnownRoots и манифест
пока без UI/точки входа, чисто библиотечный код для следующих шагов).

1. Распакуйте архив куда угодно, структура папок `ConfigBackupTool/` и
   `ConfigBackupTool.NormalizerTest/` должна остаться **рядом**, на одном уровне —
   `.csproj` ссылается на исходники соседней папки по относительному пути.
2. Убедитесь, что установлен .NET 8 SDK (`dotnet --version` должен показать 8.x;
   если не установлен — https://dotnet.microsoft.com/download/dotnet/8.0).
3. В терминале:
   ```
   cd ConfigBackupTool.NormalizerTest
   dotnet run -- "C:\путь\к\вашему\backup_list.txt"
   ```
   (можно просто перетащить файл backup_list.txt в окно терминала после `dotnet run -- `,
   так путь подставится автоматически с кавычками)
4. На экране должен появиться разбор каждого пути: `anchor` (UserProfile/AppDataRoaming/
   DriveRelative), `drive`, `type` (File/Directory — определяется реальной проверкой на
   диске, в отличие от прошлого прогона в песочнице) и `tail`.

Если `dotnet run` ругается на что-то в `ConfigBackupTool.NormalizerTest.csproj` —
пришлите текст ошибки, поправим точечно.

## Порядок разрешения пути в PathResolver.Resolve

1. UserProfile / AppDataRoaming — напрямую через Environment.SpecialFolder, без перебора дисков.
2. DriveRelative:
   a. Известные корни (IKnownRootsRepository) — если Tail начинается с маркера известного корня
      и `<корень>/<tail>` существует.
   b. OriginalPath как есть (диск не менялся).
   c. Перебор всех логических дисков: `<диск>/<tail>` и `<диск>/*/<tail>` (один уровень вложенности).
   d. Если найдено 0 — вызывающий код должен предложить выбрать путь вручную (FolderBrowserDialog).
   e. Если найдено >1 — вызывается ambiguityResolver (делегат), который должен спросить пользователя.
      Автоматический выбор "самого нового/большого" НЕ делается никогда.

## Формат known_roots.json

```json
[
  { "Marker": "steamapps/common/", "RootPath": "D:/SteamLibrary" },
  { "Marker": "Epic Games/", "RootPath": "C:/Games/Epic" }
]
```

## Допущения, которые стоит держать в голове

- PathNormalizer.Normalize определяет EntryType (File/Directory) через File.Exists/Directory.Exists,
  поэтому вызывать её нужно только для путей, существование которых уже проверено вызывающим кодом
  (как и в текущей Python-версии).
- JsonKnownRootsRepository использует System.Text.Json — встроен в .NET Core 3+/.NET 5+.
  Если итоговый WinForms-проект будет на .NET Framework 4.8, понадобится NuGet-пакет System.Text.Json.
- Стиль кода: приватные поля _camelCase, минимум LINQ, интерфейсы для каждого слоя,
  явные == null / != null вместо `is null`.

## Подтверждённые архитектурные решения (для контекста, чтобы не потерялось)

- **Детект "установлена ли игра"** — на уровне игры целиком: если ни один путь/папка
  из списка игры не существует, игра пропускается полностью (при экспорте — не создаётся
  папка/записи в манифесте, в лог одна строка "не установлена"; при восстановлении —
  "тихий" резолв первым найденным кандидатом, без диалогов).
- **gameRoot переиспользуется** внутри одной игры при восстановлении — резолвим один раз,
  остальные элементы подставляются как `gameRoot + Tail`, не гоняем PathResolver.Resolve
  (с диалогами неоднозначности) для каждого файла отдельно.
- **Diff-aware restore**: если файл на диске существует — сверяем SHA256 с `OriginalHash`
  из манифеста. Совпал — пропуск. Не совпал — поведение зависит от `RestoreMode`
  (ReplaceAll / AddMissingOnly / AskOnConflict, выбирается один раз перед стартом восстановления).
  Файла нет — вставляем без вопросов всегда, независимо от режима.
- **Safety-бэкап** в `C:/ConfigBackups/` делается перед ЛЮБОЙ заменой существующего файла,
  включая режим ReplaceAll.
- **Directory-элементы диффятся по файлам внутри** (не одним агрегированным хэшем на всю
  папку) — в манифесте у Directory-элемента список `Files[]`, каждый со своим хэшем.
- **manifest.json шифруется целиком**, пароль запрашивается в начале Restore до показа
  списка игр.
- **Имена файлов на флешке обезличены (GUID + .enc)**, плоская структура, без зеркалирования
  оригинальных путей/имён — соответствие есть только внутри зашифрованного манифеста.

## Формат manifest.json на диске

Файл — открытый JSON-объект с тремя полями (сам объект не зашифрован, секрет — внутри):
```json
{
  "Salt": "base64...",
  "IV": "base64...",
  "CipherText": "base64..."
}
```
`CipherText` — это AES-зашифрованный JSON настоящего `BackupManifest`. Salt/IV не секрет,
поэтому хранятся открыто рядом с шифротекстом; пароль пользователя нужен, чтобы получить ключ
через PBKDF2 и расшифровать `CipherText`.

## Дальше по плану

1. ICredentialStore (DPAPI, "запомнить пароль на этом ПК") — единственный оставшийся пункт.

~~2. WinForms UI: ExportForm, ImportForm, форма настроек известных корней, диалоги
конфликтов~~ — закрыто (шаги 4–6).
