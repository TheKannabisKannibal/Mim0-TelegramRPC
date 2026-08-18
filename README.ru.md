# Mim0 | TelegramRPC

**Музыка из Telegram → Discord Rich Presence для Windows**

[🇬🇧 English](README.md) · **🇷🇺 Русский**

Mim0 — лёгкое приложение в системном трее, которое читает текущую Windows Media Session и публикует информацию о треке в Discord Rich Presence.

## ✨ Возможности

- 🎵 Название трека и исполнитель
- 🖼️ Динамическая обложка, если Windows предоставляет миниатюру
- ⏱️ Прогресс воспроизведения в реальном времени
- ⏸️ Отображение паузы
- 🔄 Автоматическое переключение треков
- 🔌 Автоматическое переподключение Discord
- 🔎 Определение Telegram/AyuGram/ExteraGram
- 🌐 Опциональная поддержка других плееров Windows Media Session
- ⚙️ Окно настроек с собственными шаблонами Rich Presence
- 🔔 Управление через системный трей
- 🧰 Встроенная диагностика для GitHub Issues
- 🚀 Опциональный запуск вместе с Windows
- 📦 Portable-версия + установщик Inno Setup
- ℹ️ Окно «О программе» с текущей версией

## 🖼️ Скриншоты

Mim0 показывает музыку, которая играет в Telegram, прямо в профиле Discord: название трека, исполнителя, обложку и прогресс воспроизведения.

### Discord Rich Presence

<p align="center">
  <img src="docs/screenshots/discord-profile-1.png" alt="Профиль Discord с Mim0 Rich Presence" width="420">
  <img src="docs/screenshots/discord-profile-2.png" alt="Mim0 Rich Presence с другим треком" width="420">
</p>

### Детали активности Discord

<p align="center">
  <img src="docs/screenshots/discord-activity.png" alt="Активность Discord Mim0" width="850">
</p>

### Системный трей

<p align="center">
  <img src="docs/screenshots/tray-menu.png" alt="Меню Mim0 в системном трее" width="320">
</p>

### Настройки

<p align="center">
  <img src="docs/screenshots/settings.png" alt="Настройки Mim0" width="650">
</p>

## 🖥️ Требования

- Windows 10/11 x64
- Discord Desktop
- Telegram, AyuGram, ExteraGram или другой плеер, поддерживающий Windows Media Session

Релизная сборка полностью автономная, поэтому конечным пользователям **не нужно устанавливать .NET 8**.

## 📥 Скачать

Откройте **Releases** и скачайте один из вариантов:

- `Mim0-TelegramRPC-vX.Y.Z-win-x64.zip` — portable-версия
- `Mim0.TelegramRPC.Setup.exe` — обычный установщик Windows

Установщик может добавить Mim0 в автозагрузку Windows и создать ярлык на рабочем столе.

## ⚙️ Настройки

Нажмите правой кнопкой мыши по иконке Mim0 в трее и выберите **Настройки**.

Доступные параметры:

- Показ/скрытие обложки трека
- Показ/скрытие прогресса воспроизведения
- Показ состояния паузы
- Режим только Telegram или все совместимые Windows Media Sessions
- Запуск вместе с Windows
- Собственные шаблоны Details и State

Поддерживаемые переменные:

```text
{title}
{artist}
{source}
```

Пример:

```text
Details: {title}
State: 🎧 {artist}
```

Настройки хранятся в:

```text
%APPDATA%\Mim0\TelegramRPC\settings.json
```

## 🎮 Настройка Discord

Приложение использует публичный Discord Application ID, встроенный в исходный код. Это **не bot token и не секретный ключ**.

Для резервного изображения Discord-приложение должно содержать Rich Presence asset с точным ключом:

```text
default
```

Во время тестирования держите **Discord Desktop** запущенным.

## 🖼️ Обложки и приватность

Windows Media Session может предоставлять миниатюру текущего трека. Discord Rich Presence не может использовать произвольный локальный файл как внешнее изображение, поэтому при включённой обложке Mim0 временно загружает её в Litterbox.

- Загружается только текущая обложка.
- Для файла запрашивается срок хранения один час.
- Сообщения Telegram, чаты, контакты и аудио не загружаются.
- Обложки можно отключить в настройках.

Если вы не хотите загружать изображения обложек, отключите **Показывать обложку трека**.

## 🛠️ Сборка из исходников

Требуется **.NET 8 SDK** и Windows.

```text
build.bat
```

Или:

```powershell
dotnet restore TelegramDiscordRPC.csproj
dotnet publish TelegramDiscordRPC.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Результат:

```text
bin\Release\net8.0-windows10.0.17763.0\win-x64\publish\Mim0.TelegramRPC.exe
```

## 🤖 GitHub Actions

Push в `main` запускает проверку Windows-сборки.

Push тега, например:

```text
v1.5.0
```

автоматически собирает portable ZIP и установщик и публикует оба файла в GitHub Release.

Workflow получает версию релиза из тега, поэтому его можно использовать для следующих версий.

## 🐛 Решение проблем

### RPC не появляется

1. Убедитесь, что запущен Discord Desktop.
2. Запустите Mim0.
3. Запустите трек в Telegram.
4. Подождите несколько секунд.
5. Нажмите правой кнопкой по иконке Mim0 → **Проверить сейчас**.
6. При необходимости выберите **Переподключить Discord**.

### Telegram играет, но Mim0 ничего не видит

Откройте настройки и убедитесь, что включён параметр **Использовать только Telegram-плееры**. Если ваш клиент Telegram использует другой идентификатор Windows Media Session, отключите этот параметр и попробуйте поиск среди всех совместимых сессий.

### Нужно сообщить об ошибке

В меню трея выберите **Скопировать диагностику**, затем вставьте результат в GitHub Issue. Не вставляйте личные данные или приватные токены.

## 📁 Структура проекта

```text
Mim0-TelegramRPC/
├── .github/workflows/ci-release.yml
├── docs/screenshots/
├── Program.cs
├── Settings.cs
├── SettingsForm.cs
├── TelegramDiscordRPC.csproj
├── installer.iss
├── build.bat
└── README.md
```

## 📜 Лицензия

Проект распространяется по лицензии из `LICENSE`. Она разрешает личное и некоммерческое использование; распространение и коммерческое использование ограничены без разрешения Mim0.

## 👤 Автор

**Mim0**

GitHub: https://github.com/TheKannabisKannibal/Mim0-TelegramRPC
