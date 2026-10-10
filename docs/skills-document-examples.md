# Приклади документів (Matching, MongoDB)

База `QPQ_Matching`. Приклади відповідають тестовим даним у `db/p3/matching.seed.js` і проходять валідацію `$jsonSchema` з `db/p3/matching.collections.js`. Формат запису: MongoDB Extended JSON.

## Колекція userSkillIndex

Локальна копія записів UserSkills (read-модель для підбору пар). Документи мають різні набори полів залежно від `type`: поле `experienceYears` можливе лише в `Offer`. Мови та вікна доступності вбудовані.

Пропозиція (`type: Offer`):

```json
{
  "_id": { "$oid": "670000000000000000000d01" },
  "userSkillId": 1,
  "userId": "3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01",
  "skillId": 1,
  "skillName": "Програмування C#",
  "type": "Offer",
  "level": 4,
  "status": "Active",
  "experienceYears": 5,
  "languages": [
    { "code": "uk", "proficiency": 5 },
    { "code": "en", "proficiency": 3 }
  ],
  "availability": [
    { "dayOfWeek": 1, "start": "18:00", "end": "20:00" },
    { "dayOfWeek": 3, "start": "18:00", "end": "20:00" }
  ],
  "updatedAt": { "$date": "2026-10-05T12:00:00Z" }
}
```

Бажання (`type: Want`), без поля `experienceYears`:

```json
{
  "_id": { "$oid": "670000000000000000000d04" },
  "userSkillId": 4,
  "userId": "7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02",
  "skillId": 1,
  "skillName": "Програмування C#",
  "type": "Want",
  "level": 2,
  "status": "Active",
  "languages": [{ "code": "uk", "proficiency": 5 }],
  "availability": [{ "dayOfWeek": 1, "start": "18:00", "end": "20:00" }],
  "updatedAt": { "$date": "2026-10-05T12:00:00Z" }
}
```

## Колекція matches

Знайдена пара. Містить вбудовані `offer` (і `counterOffer` для взаємних пар), а також посилання `indexRef` на запис у `userSkillIndex` (гібридний підхід: посилання плюс знімок `skillName`). Поле `userIds` продубльоване для індексу за користувачем, `expiresAt` задає TTL.

Взаємна пара (`kind: mutual`):

```json
{
  "_id": { "$oid": "670000000000000000000e01" },
  "kind": "mutual",
  "status": "Suggested",
  "score": 95,
  "userIds": [
    "3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01",
    "7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02"
  ],
  "offer": {
    "fromUserId": "3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01",
    "toUserId": "7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02",
    "skillId": 1,
    "skillName": "Програмування C#",
    "indexRef": { "$oid": "670000000000000000000d01" }
  },
  "counterOffer": {
    "fromUserId": "7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02",
    "toUserId": "3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01",
    "skillId": 2,
    "skillName": "Англійська розмовна",
    "indexRef": { "$oid": "670000000000000000000d03" }
  },
  "commonLanguages": ["uk"],
  "createdAt": { "$date": "2026-10-05T12:05:00Z" },
  "expiresAt": { "$date": "2027-01-31T00:00:00Z" }
}
```

Одностороння пара (`kind: oneWay`), без `counterOffer`:

```json
{
  "_id": { "$oid": "670000000000000000000e02" },
  "kind": "oneWay",
  "status": "Viewed",
  "score": 70,
  "userIds": [
    "7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02",
    "b19e6a33-2c5d-47f8-8d14-9a0b3e6f5c03"
  ],
  "offer": {
    "fromUserId": "b19e6a33-2c5d-47f8-8d14-9a0b3e6f5c03",
    "toUserId": "7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02",
    "skillId": 3,
    "skillName": "Гра на гітарі",
    "indexRef": { "$oid": "670000000000000000000d06" }
  },
  "commonLanguages": ["uk"],
  "createdAt": { "$date": "2026-10-05T12:06:00Z" },
  "expiresAt": { "$date": "2027-01-31T00:00:00Z" }
}
```

## Колекція skillStats

Похідна колекція: лічильники попиту й пропозиції за активними записами. `_id` дорівнює `skillId`. Перераховується з `userSkillIndex` (див. запит 8 у `db/p3/matching.queries.js`).

```json
{
  "_id": 1,
  "skillName": "Програмування C#",
  "offersCount": 1,
  "wantsCount": 1,
  "topLanguages": [{ "code": "uk", "count": 2 }, { "code": "en", "count": 1 }],
  "updatedAt": { "$date": "2026-10-05T12:10:00Z" }
}
```
