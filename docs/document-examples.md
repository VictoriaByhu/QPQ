# Приклади документів (Messages, MongoDB)

База `QPQ_Messages`. Приклади відповідають тестовим даним у `db/p3/seed.js` і проходять валідацію `$jsonSchema` з `db/p3/collections.js`. Формат запису: MongoDB Extended JSON.

## Колекція conversations

Розмова за прийнятим обміном. Містить вбудовані `participants` і знімок `lastMessage`, посилається на обмін через `swapId`. Поле `swapStatus` денормалізоване з бази Swaps.

```json
{
  "_id": { "$oid": "670000000000000000000c01" },
  "swapId": 2,
  "swapStatus": "Accepted",
  "participants": [
    { "userId": "7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02", "role": "Initiator", "lastReadAt": { "$date": "2026-10-02T10:35:00Z" } },
    { "userId": "b19e6a33-2c5d-47f8-8d14-9a0b3e6f5c03", "role": "Partner", "lastReadAt": { "$date": "2026-10-02T10:40:00Z" } }
  ],
  "lastMessage": {
    "messageId": { "$oid": "670000000000000000000a03" },
    "senderId": "7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02",
    "preview": "Чудово! Ось план першого заняття з англійської.",
    "sentAt": { "$date": "2026-10-02T10:30:00Z" }
  },
  "createdAt": { "$date": "2026-10-02T09:00:00Z" },
  "updatedAt": null
}
```

## Колекція messages

Документи мають різні набори полів залежно від `type`. Усі посилаються на розмову через `conversationId`.

Текстове повідомлення (`type: text`):

```json
{
  "_id": { "$oid": "670000000000000000000a01" },
  "conversationId": { "$oid": "670000000000000000000c01" },
  "senderId": "7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02",
  "type": "text",
  "text": "Привіт! Коли тобі зручно зустрітися для обміну?",
  "editedAt": null,
  "createdAt": { "$date": "2026-10-02T10:00:00Z" },
  "isDeleted": false,
  "deletedAt": null
}
```

Повідомлення з файлом (`type: file`): гібридне вкладення, посилання `attachmentId` плюс вбудована назва `fileName`.

```json
{
  "_id": { "$oid": "670000000000000000000a03" },
  "conversationId": { "$oid": "670000000000000000000c01" },
  "senderId": "7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02",
  "type": "file",
  "caption": "Чудово! Ось план першого заняття з англійської.",
  "attachments": [
    { "attachmentId": { "$oid": "670000000000000000000b01" }, "fileName": "english-lesson-plan.pdf" }
  ],
  "createdAt": { "$date": "2026-10-02T10:30:00Z" },
  "isDeleted": false,
  "deletedAt": null
}
```

Системне повідомлення (`type: system`), яке створює подія зі Swaps:

```json
{
  "_id": { "$oid": "670000000000000000000a07" },
  "conversationId": { "$oid": "670000000000000000000c01" },
  "senderId": "b19e6a33-2c5d-47f8-8d14-9a0b3e6f5c03",
  "type": "system",
  "event": "SwapAccepted",
  "details": { "swapId": 2 },
  "createdAt": { "$date": "2026-10-02T09:00:00Z" },
  "isDeleted": false,
  "deletedAt": null
}
```

## Колекція attachments

Метадані файлу зберігаються окремо, а повідомлення посилаються на них.

```json
{
  "_id": { "$oid": "670000000000000000000b01" },
  "conversationId": { "$oid": "670000000000000000000c01" },
  "uploadedBy": "7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02",
  "fileName": "english-lesson-plan.pdf",
  "url": "https://files.example.com/swaps/2/english-lesson-plan.pdf",
  "contentType": "application/pdf",
  "sizeBytes": 184320,
  "uploadedAt": { "$date": "2026-10-02T10:29:00Z" }
}
```

## Що перевіряє валідація

| Поле | Правило |
|---|---|
| `senderId`, `uploadedBy`, `participants.userId` | рядок у форматі GUID |
| `swapId` | число не менше 1 |
| `swapStatus` | Accepted, Completed або Cancelled |
| `participants` | рівно два елементи з роллю Initiator або Partner |
| `messages.type` | `text`, `file` або `system`, набір полів перевіряється для кожного типу окремо |
| `text` | рядок від 1 до 4000 символів |
| `attachments` у повідомленні | від 1 до 10 елементів з `attachmentId` та `fileName` |
| `event` | SwapAccepted, SwapCompleted або SwapCancelled |
| додаткові поля | не допускаються |