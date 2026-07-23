PRAGMA journal_mode = WAL;

CREATE TABLE IF NOT EXISTS History (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TimestampLocal TEXT NOT NULL,
    OriginalPath TEXT NOT NULL,
    NewPath TEXT NOT NULL,
    OriginalName TEXT NOT NULL,
    NewName TEXT NOT NULL,
    RuleName TEXT NULL,
    Sha256Hash TEXT NULL,
    ActionType INTEGER NOT NULL,
    Status INTEGER NOT NULL,
    ErrorMessage TEXT NULL,
    IsAutoApplied INTEGER NOT NULL,
    CanUndo INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS Settings (
    [Key] TEXT PRIMARY KEY,
    [Value] TEXT NOT NULL
);

-- Settings bevat een JSON AppSettings-payload.
-- Op Windows wordt deze waarde met DPAPI opgeslagen met prefix 'dpapi:'.

CREATE TABLE IF NOT EXISTS Rules (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    ExtensionEquals TEXT NULL,
    FileNameContains TEXT NULL,
    SourceFolderContains TEXT NULL,
    AutoApply INTEGER NOT NULL,
    Priority INTEGER NOT NULL,
    Category INTEGER NOT NULL,
    DestinationFolder TEXT NULL,
    RenameTemplate TEXT NULL
);
