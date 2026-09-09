# Sekkati

[日本語 (Japanese)](README.JP.md)

A scratchpad notepad for impatient people. Pop it up, jot it down, and tuck it away.

## Features

- **Instant access via hotkey** — Bring it up anytime with an OS shortcut
- **Auto-save on blur** — Automatically saves if there is content and closes when losing focus
- **No filename required** — File paths and names are automatically generated based on timestamps
- **Timeline browsing** — Quick retrospective reading by "Today", "Yesterday", and more
- **Background resident** — Stays active in the background, preventing multiple instances

## Requirements / Environment

- Ubuntu (Primary target)
- Windows (Supported)
- .NET 10 / Avalonia UI

## Setup

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run --project Sekkati/Sekkati.csproj
```

### Hotkey Registration (Ubuntu / GNOME)

Go to **Settings** → **Keyboard** → **Custom Shortcuts**, and configure as follows:

| Setting | Value |
|---------|-------|
| Name | Sekkati |
| Command | `/path/to/Sekkati` |
| Shortcut | Any (e.g., `Super+Alt+X`) |

### Hotkey Registration (Windows / AutoHotkey)

```ahk
; AutoHotkey v2
#!x:: {
    Run "C:\path\to\Sekkati.exe"
}
```

## Usage

1. Summon the window with your hotkey.
2. Jot down your notes.
3. Click outside the window → The note is automatically saved and the window hides.
4. If nothing is entered, no file is saved.

### Browsing Past Notes

Select a time period from the **Browse** menu to view notes in reverse chronological order:

| Menu | Range |
|------|-------|
| Past 1 Hour | Notes from the last 1 hour |
| Today | Notes from today (since 00:00) |
| Yesterday | Notes from yesterday |
| This Week | Notes from Monday to now |
| Past 1 Month | Notes from the last 30 days |
| Past 1 Year | Notes from the last 1 year |

## Storage Location

`~/Documents/Sekkati/yyyyMMddHHmmss.txt`
