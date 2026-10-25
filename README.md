# Listik

Native Windows desktop sticky notes. UI languages: English, French, Russian and Ukrainian. No account, server or network access.

## Build and test

Requires Windows with .NET Framework 4.x and its bundled C# compiler. No NuGet dependencies.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\test.ps1
```

Run `bin/ListikSetup.exe` to open the installer. It displays the installation folder and offers Install or Update depending on whether the executable is already present; versions are not compared. Close Listik from the tray before updating. Notes and settings are preserved. After completion, the optional Launch Listik checkbox controls launching on Finish. The installer supports all four languages, creates a Start menu shortcut and does not require administrator access. `bin/Listik.exe` is the development build.

## Usage

- Type directly into a note. Drag the blank coloured toolbar to move; drag its edges to resize. Notes have no title text or native window frame.
- **+** creates a note. **×** deletes it permanently, immediately.
- **···** opens colour, text, checklist, image, opacity and window options.
- Double-click the blank toolbar to collapse or expand a note.
- Click a checkbox character to toggle a checklist item.
- Paste text or images with Ctrl+V, or drop image files onto the editor.
- The tray menu provides language selection, show/hide all, autostart and quit.
- Notes autosave after editing, approximately every 600 ms, and on normal exit.

## Source layout

- [Program](src/Program.cs): application entry point and single-instance guard.
- [NoteData](src/NoteData.cs): persisted fields; names remain compatible with existing notes.
- [NoteStore](src/NoteStore.cs): JSON loading and atomic saving.
- [NotesApp](src/NotesApp.cs): note lifecycle, autosave and desktop visibility; [tray menu](src/NotesApp.Tray.cs) is kept separately.
- [NoteWindow](src/NoteWindow.cs): editing, dragging, resizing and appearance; [note menu](src/NoteWindow.Menu.cs) contains menu construction.
- [Language](src/Language.cs): translations and language preference.
- [Native](src/Native.cs): Windows interop and atomic file replacement.
- [Setup](src/Setup.cs): installer interface; [installation operations](src/Setup.Installation.cs) copy the executable and create the shortcut.
- [DrawIcon](tools/DrawIcon.cs): original icon generation during the build.

Build scripts discover application and installer sources separately. No extra packages or SDK are needed. The shared formatting conventions are in [.editorconfig](.editorconfig).

Notes are stored in `%LOCALAPPDATA%/Listik/notes.json`; language preference is in the same directory. Writes use atomic file replacement. No archive, trash, backup, export, tags, search, reminders, themes or sync.

## Behaviour and current limits

- Notes remain visible when focus moves to Start, the tray or an application. Normal application windows cover notes naturally; uncovered portions remain visible. Desktop-only mode does not embed notes into Explorer.
- New notes default to desktop-only mode. Existing notes retain their saved visibility setting.
- Always-on-top and desktop-only are mutually exclusive per note.
- When the desktop has focus (including after Win+D), notes are restored without activation and raised above the desktop within the normal window layer, not the always-on-top layer. This is checked every 600 ms, so a brief transition is possible. Explicitly hiding all notes still takes priority. Only the explicit Always on top setting keeps notes above application windows.
- Windows font, colour and file dialogs follow the Windows display language; Listik menus follow the selected language.
- Inserting an image from a file uses the Windows clipboard and replaces its current image/text contents.
- Checklist items are editable checkbox characters rather than structured task objects.
- Monitor changes clamp notes into the available working area.
- The minimal installer copies the application and creates a Start menu shortcut; Windows Apps uninstall registration is not yet provided. To remove it, quit, disable autostart, remove `%LOCALAPPDATA%/Programs/Listik` and the Start menu shortcut. Notes remain in their separate data directory.

Automated smoke tests exercise rich-text/Unicode persistence, saved appearance, four menu languages, creation, permanent deletion, menu lifetime after closing, restoring minimized notes, temporary desktop elevation, and explicit hiding. Live Win+D interaction and disconnected-monitor behaviour still need manual verification.
