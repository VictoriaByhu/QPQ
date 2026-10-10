const database = db.getSiblingDB('QPQ_Matching');

// Користувачі ті самі, що в усіх контекстах QPQ (їх видає Identity).
const u1 = '3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01';
const u2 = '7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02';
const u3 = 'b19e6a33-2c5d-47f8-8d14-9a0b3e6f5c03';
const u4 = 'e8d05b44-9f7a-4c21-a6b3-1d2e4f7a8b04';

const names = {
    1: 'Програмування C#',
    2: 'Англійська розмовна',
    3: 'Гра на гітарі',
    4: 'UI-дизайн',
    5: 'Фотографія',
    6: 'Приготування випічки'
};

function upsertAll(collection, documents) {
    collection.bulkWrite(
        documents.map(function (d) {
            return { updateOne: { filter: { _id: d._id }, update: { $setOnInsert: d }, upsert: true } };
        })
    );
}

function slot(dayOfWeek, start, end) {
    return { dayOfWeek: dayOfWeek, start: start, end: end };
}

function lang(code, proficiency) {
    return { code: code, proficiency: proficiency };
}

// Копія записів UserSkills (db/p1/userskills.seed.sql): userSkillId збігається з UserSkills.Id.
const indexDocs = [
    { _id: ObjectId('670000000000000000000d01'), userSkillId: 1, userId: u1, skillId: 1, type: 'Offer', level: 4, status: 'Active',
      experienceYears: 5, languages: [lang('uk', 5), lang('en', 3)], availability: [slot(1, '18:00', '20:00'), slot(3, '18:00', '20:00')] },
    { _id: ObjectId('670000000000000000000d02'), userSkillId: 2, userId: u1, skillId: 2, type: 'Want', level: 2, status: 'Active',
      languages: [lang('uk', 5)], availability: [slot(6, '10:00', '12:00')] },
    { _id: ObjectId('670000000000000000000d03'), userSkillId: 3, userId: u2, skillId: 2, type: 'Offer', level: 5, status: 'Active',
      experienceYears: 8, languages: [lang('uk', 5), lang('en', 5), lang('pl', 3)], availability: [slot(2, '17:00', '19:00'), slot(4, '17:00', '19:00')] },
    { _id: ObjectId('670000000000000000000d04'), userSkillId: 4, userId: u2, skillId: 1, type: 'Want', level: 2, status: 'Active',
      languages: [lang('uk', 5)], availability: [slot(1, '18:00', '20:00')] },
    { _id: ObjectId('670000000000000000000d05'), userSkillId: 5, userId: u2, skillId: 3, type: 'Want', level: 1, status: 'Active',
      languages: [lang('uk', 5)], availability: [slot(4, '17:00', '19:00')] },
    { _id: ObjectId('670000000000000000000d06'), userSkillId: 6, userId: u3, skillId: 3, type: 'Offer', level: 4, status: 'Active',
      experienceYears: 6, languages: [lang('uk', 5)], availability: [slot(5, '16:00', '18:00')] },
    { _id: ObjectId('670000000000000000000d07'), userSkillId: 7, userId: u3, skillId: 4, type: 'Offer', level: 3, status: 'Active',
      experienceYears: 3, languages: [lang('uk', 5), lang('en', 4)], availability: [slot(7, '12:00', '14:00')] },
    { _id: ObjectId('670000000000000000000d08'), userSkillId: 8, userId: u3, skillId: 1, type: 'Want', level: 2, status: 'Paused',
      languages: [lang('uk', 5)], availability: [slot(3, '19:00', '21:00')] },
    { _id: ObjectId('670000000000000000000d09'), userSkillId: 9, userId: u4, skillId: 5, type: 'Offer', level: 4, status: 'Active',
      experienceYears: 4, languages: [lang('uk', 5), lang('en', 3)], availability: [slot(6, '12:00', '14:00')] },
    { _id: ObjectId('670000000000000000000d0a'), userSkillId: 10, userId: u4, skillId: 1, type: 'Want', level: 1, status: 'Archived',
      languages: [lang('uk', 5)], availability: [] }
].map(function (d) {
    return Object.assign({ skillName: names[d.skillId], updatedAt: new Date('2026-10-05T12:00:00Z') }, d);
});

upsertAll(database.userSkillIndex, indexDocs);

// Знайдені пари. skillName у matches є знімком на момент підбору і не оновлюється (пари тимчасові, TTL).
upsertAll(database.matches, [
    {
        _id: ObjectId('670000000000000000000e01'),
        kind: 'mutual',
        status: 'Suggested',
        score: 95,
        userIds: [u1, u2],
        offer: { fromUserId: u1, toUserId: u2, skillId: 1, skillName: names[1], indexRef: ObjectId('670000000000000000000d01') },
        counterOffer: { fromUserId: u2, toUserId: u1, skillId: 2, skillName: names[2], indexRef: ObjectId('670000000000000000000d03') },
        commonLanguages: ['uk'],
        createdAt: new Date('2026-10-05T12:05:00Z'),
        expiresAt: new Date('2027-01-31T00:00:00Z')
    },
    {
        _id: ObjectId('670000000000000000000e02'),
        kind: 'oneWay',
        status: 'Viewed',
        score: 70,
        userIds: [u2, u3],
        offer: { fromUserId: u3, toUserId: u2, skillId: 3, skillName: names[3], indexRef: ObjectId('670000000000000000000d06') },
        commonLanguages: ['uk'],
        createdAt: new Date('2026-10-05T12:06:00Z'),
        expiresAt: new Date('2027-01-31T00:00:00Z')
    },
    {
        _id: ObjectId('670000000000000000000e03'),
        kind: 'oneWay',
        status: 'Dismissed',
        score: 55,
        userIds: [u1, u3],
        offer: { fromUserId: u1, toUserId: u3, skillId: 1, skillName: names[1], indexRef: ObjectId('670000000000000000000d01') },
        commonLanguages: ['uk'],
        createdAt: new Date('2026-10-04T09:00:00Z'),
        expiresAt: new Date('2027-01-31T00:00:00Z')
    }
]);

// Похідні лічильники рахуються лише за активними записами (повторне виконання дає той самий результат).
const active = indexDocs.filter(function (d) { return d.status === 'Active'; });
const statsDocs = Object.keys(names).map(function (key) {
    const skillId = Number(key);
    const rows = active.filter(function (d) { return d.skillId === skillId; });
    const counts = {};
    rows.forEach(function (d) {
        d.languages.forEach(function (l) { counts[l.code] = (counts[l.code] || 0) + 1; });
    });
    const topLanguages = Object.keys(counts)
        .map(function (code) { return { code: code, count: counts[code] }; })
        .sort(function (a, b) { return b.count - a.count || (a.code < b.code ? -1 : 1); })
        .slice(0, 3);
    return {
        _id: skillId,
        skillName: names[skillId],
        offersCount: rows.filter(function (d) { return d.type === 'Offer'; }).length,
        wantsCount: rows.filter(function (d) { return d.type === 'Want'; }).length,
        topLanguages: topLanguages,
        updatedAt: new Date('2026-10-05T12:10:00Z')
    };
});

upsertAll(database.skillStats, statsDocs);
