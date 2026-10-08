# ERD: піддомен Swaps

Піддомен складається з трьох API, кожен має власну базу. Між базами немає FOREIGN KEY: зв'язок забезпечують ідентифікатори (`SwapId`, `SkillId`, `UserId`) та локальні копії, які оновлюються подіями.

## Swaps (Проєкт №1, SQL Server, ADO.NET + Dapper), база QPQ_Swaps

```mermaid
erDiagram
    Swaps ||--|| SwapDetails : "1:1"
    Swaps ||--o{ SwapStatusHistory : "1:N"
    Swaps ||--o{ SwapSkills : "M:N"
    Skills ||--o{ SwapSkills : "M:N"

    Skills {
        int Id PK
        nvarchar Name
        datetime2 CreatedAt
        datetime2 UpdatedAt
        bit IsDeleted
        datetime2 DeletedAt
        rowversion RowVersion
    }
    Swaps {
        int Id PK
        uniqueidentifier InitiatorId
        uniqueidentifier PartnerId
        nvarchar Status
        datetime2 CreatedAt
        uniqueidentifier CreatedBy
        datetime2 UpdatedAt
        uniqueidentifier UpdatedBy
        bit IsDeleted
        datetime2 DeletedAt
        rowversion RowVersion
    }
    SwapDetails {
        int SwapId PK, FK
        nvarchar Message
        nvarchar Location
        datetime2 ProposedDate
        int DurationMinutes
        datetime2 CreatedAt
        datetime2 UpdatedAt
        rowversion RowVersion
    }
    SwapSkills {
        int SwapId PK, FK
        int SkillId PK, FK
        nvarchar Role PK
        datetime2 CreatedAt
    }
    SwapStatusHistory {
        bigint Id PK
        int SwapId FK
        nvarchar OldStatus
        nvarchar NewStatus
        uniqueidentifier ChangedByUserId
        nvarchar Comment
        datetime2 ChangedAt
    }
```

## Sessions (Проєкт №2, SQL Server, EF Core), база QPQ_Sessions

```mermaid
erDiagram
    AcceptedSwaps ||--o{ Sessions : "1:N"
    SessionFormats ||--o{ Sessions : "1:N"
    Sessions ||--o| SessionOutcomes : "1:1"
    Sessions ||--o{ SessionSkills : "M:N"
    Skills ||--o{ SessionSkills : "M:N"

    SessionFormats {
        tinyint Id PK
        nvarchar Code UK
        nvarchar Name
    }
    Skills {
        int Id PK
        nvarchar Name
        datetime2 CreatedAt
        datetime2 UpdatedAt
        bit IsDeleted
        datetime2 DeletedAt
        rowversion RowVersion
    }
    AcceptedSwaps {
        int SwapId PK
        uniqueidentifier InitiatorId
        uniqueidentifier PartnerId
        nvarchar Status
        datetime2 CreatedAt
        datetime2 UpdatedAt
        rowversion RowVersion
    }
    Sessions {
        int Id PK
        int SwapId FK
        tinyint FormatId FK
        datetime2 ScheduledAt
        int DurationMinutes
        nvarchar Link
        nvarchar Location
        nvarchar Status
        uniqueidentifier CreatedByUserId
        datetime2 CreatedAt
        datetime2 UpdatedAt
        bit IsDeleted
        datetime2 DeletedAt
        rowversion RowVersion
    }
    SessionSkills {
        int SessionId PK, FK
        int SkillId PK, FK
        int PlannedMinutes
        datetime2 AddedAt
    }
    SessionOutcomes {
        int SessionId PK, FK
        nvarchar Summary
        int ActualDurationMinutes
        datetime2 CompletedAt
        rowversion RowVersion
    }
```

## Messages (Проєкт №3, MongoDB), база QPQ_Messages

```mermaid
erDiagram
    conversations ||--o{ messages : "conversationId (reference)"
    conversations ||--o{ attachments : "conversationId (reference)"
    messages }o--o{ attachments : "attachments.attachmentId (reference)"
    conversations ||--|{ participants : "embedded"
    conversations ||--o| lastMessage : "embedded snapshot"

    conversations {
        objectId _id PK
        number swapId UK
        string swapStatus
        array participants
        object lastMessage
        date createdAt
        date updatedAt
    }
    participants {
        string userId
        string role
        date lastReadAt
    }
    lastMessage {
        objectId messageId
        string senderId
        string preview
        date sentAt
    }
    messages {
        objectId _id PK
        objectId conversationId
        string senderId
        string type
        string text
        string caption
        array attachments
        string event
        object details
        date createdAt
        date editedAt
        bool isDeleted
        date deletedAt
    }
    attachments {
        objectId _id PK
        objectId conversationId
        string uploadedBy
        string fileName
        string url
        string contentType
        number sizeBytes
        date uploadedAt
    }
```

Документи в `messages` мають різні набори полів залежно від `type`: `text` (`text`, `editedAt`), `file` (`caption`, `attachments`), `system` (`event`, `details`).

## Зв'язки між базами

| Поле | Де використовується | Звідки береться | Як зв'язано |
|---|---|---|---|
| `UserId` (GUID) | `InitiatorId`, `PartnerId`, `CreatedBy`, `UpdatedBy`, `ChangedByUserId`, `CreatedByUserId`, `senderId`, `uploadedBy`, `participants.userId` | Identity (claim `sub`) | лише значення, без таблиці користувачів |
| `SkillId` | `Skills.Id` у Swaps і Sessions | Catalog | локальні копії `Skills`, оновлюються подією `SkillChanged` |
| `SwapId` | `AcceptedSwaps.SwapId` у Sessions, `conversations.swapId` у Messages | Swaps | локальні копії прийнятих обмінів, оновлюються подіями `SwapAccepted` і `SwapCompleted` |