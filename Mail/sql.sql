create table main.MailFolderData
(
    Id               NVARCHAR(255) not null
        primary key,
    Name             NVARCHAR(255),
    Type             MEDIUMINT     not null,
    IsHidden         BOOLEAN       not null,
    TotalItemCount   INTEGER       not null,
    MailType         MEDIUMINT     not null,
    ParentFolderId   NVARCHAR(255),
    ChildFolderCount INTEGER       not null
);

create table main.MailMessageContentData
(
    MessageId      NVARCHAR(255) not null
        primary key,
    Content        TEXT,
    ContentPreview NVARCHAR(255),
    ContentType    MEDIUMINT     not null
);

create table main.MailMessageData
(
    MessageId               NVARCHAR(255) not null
        primary key,
    InferenceClassification NVARCHAR(128),
    FolderId                NVARCHAR(255),
    Title                   NVARCHAR(255),
    SentTime                DATETIME
);

create table main.MailMessageRecipientData
(
    Id            integer       not null
        constraint MailMessageRecipientData_pk
            primary key autoincrement,
    MessageId     NVARCHAR(255) not null,
    Name          NVARCHAR(255),
    Address       NVARCHAR(255) not null,
    RecipientType MEDIUMINT     not null
);

