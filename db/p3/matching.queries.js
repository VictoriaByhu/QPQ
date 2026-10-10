// Приклади запитів і агрегацій для QPQ_Matching (Проєкт №3).
// Запуск після matching.collections.js і matching.seed.js:  mongosh db\p3\matching.queries.js
const database = db.getSiblingDB('QPQ_Matching');

const u1 = '3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01';
const u2 = '7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02';

function show(title, value) {
    print('\n=== ' + title + ' ===');
    printjson(value);
}

// 1. Документи однієї колекції мають різні набори полів: у Offer є experienceYears, у Want немає.
show('1. Набори полів у userSkillIndex за типом (гнучка схема)',
    database.userSkillIndex.aggregate([
        { $project: { type: 1, fields: { $map: { input: { $objectToArray: '$$ROOT' }, as: 'f', in: '$$f.k' } } } },
        { $group: { _id: '$type', count: { $sum: 1 }, fieldSets: { $addToSet: '$fields' } } },
        { $sort: { _id: 1 } }
    ]).toArray());

// 2. Хто активно пропонує навичку C# і хоче її вивчити (індекс ix_userSkillIndex_skill_type_active).
show('2. Активні Offer і Want для навички 1',
    database.userSkillIndex.find(
        { skillId: 1, status: 'Active' },
        { _id: 0, userId: 1, type: 1, level: 1, languages: 1 }
    ).sort({ type: 1, level: -1 }).toArray());

// 3. Пошук за вбудованим масивом: хто може навчати польською (languages.code) і коли доступний у вівторок.
show('3. Offer з польською мовою та вікном у вівторок',
    database.userSkillIndex.find(
        { type: 'Offer', status: 'Active', 'languages.code': 'pl', 'availability.dayOfWeek': 2 },
        { _id: 0, userId: 1, skillName: 1, availability: 1 }
    ).toArray());

// 4. Підбір взаємних пар на льоту: A пропонує те, що хоче B, і B пропонує те, що хоче A.
show('4. Взаємні пари за активними записами',
    database.userSkillIndex.aggregate([
        { $match: { type: 'Offer', status: 'Active' } },
        { $lookup: {
            from: 'userSkillIndex',
            let: { offerUser: '$userId', offerSkill: '$skillId' },
            pipeline: [
                { $match: { $expr: { $and: [
                    { $eq: ['$type', 'Want'] }, { $eq: ['$status', 'Active'] },
                    { $eq: ['$skillId', '$$offerSkill'] }, { $ne: ['$userId', '$$offerUser'] }
                ] } } }
            ],
            as: 'wanters'
        } },
        { $unwind: '$wanters' },
        { $lookup: {
            from: 'userSkillIndex',
            let: { a: '$userId', b: '$wanters.userId' },
            pipeline: [
                { $match: { $expr: { $and: [
                    { $eq: ['$type', 'Offer'] }, { $eq: ['$status', 'Active'] }, { $eq: ['$userId', '$$b'] }
                ] } } },
                { $lookup: {
                    from: 'userSkillIndex',
                    let: { skill: '$skillId' },
                    pipeline: [
                        { $match: { $expr: { $and: [
                            { $eq: ['$type', 'Want'] }, { $eq: ['$status', 'Active'] },
                            { $eq: ['$userId', '$$a'] }, { $eq: ['$skillId', '$$skill'] }
                        ] } } }
                    ],
                    as: 'back'
                } },
                { $match: { 'back.0': { $exists: true } } }
            ],
            as: 'reverse'
        } },
        { $match: { 'reverse.0': { $exists: true } } },
        { $project: { _id: 0, userA: '$userId', offersA: '$skillName', userB: '$wanters.userId',
                      offersB: { $first: '$reverse.skillName' } } },
        { $match: { $expr: { $lt: ['$userA', '$userB'] } } } // кожна пара лише один раз
    ]).toArray());

// 5. Список пар користувача зі стрічкою за оцінкою та пагінацією (індекс ix_matches_user_status_score).
const pageSize = 2;
const pageNumber = 1; // змініть на 2 для наступної сторінки
show('5. Пари користувача U2, сторінка ' + pageNumber,
    database.matches
        .find({ userIds: u2, status: { $in: ['Suggested', 'Viewed'] } }, { kind: 1, score: 1, status: 1, offer: 1 })
        .sort({ score: -1 })
        .skip((pageNumber - 1) * pageSize)
        .limit(pageSize)
        .toArray());

// 6. Гібридне посилання: пари з даними запису-пропозиції з userSkillIndex ($lookup за indexRef).
show('6. Пари з рівнем і мовами пропонованої навички',
    database.matches.aggregate([
        { $lookup: { from: 'userSkillIndex', localField: 'offer.indexRef', foreignField: '_id', as: 'src' } },
        { $project: { _id: 0, kind: 1, status: 1, score: 1, skill: '$offer.skillName',
                      level: { $first: '$src.level' }, languages: { $first: '$src.languages.code' } } },
        { $sort: { score: -1 } }
    ]).toArray());

// 7. Кількість пар за видом і статусом.
show('7. Пари за видом і статусом',
    database.matches.aggregate([
        { $group: { _id: { kind: '$kind', status: '$status' }, count: { $sum: 1 }, avgScore: { $avg: '$score' } } },
        { $sort: { '_id.kind': 1, '_id.status': 1 } }
    ]).toArray());

// 8. Перерахунок skillStats із userSkillIndex ($merge замінює документи за _id = skillId; потрібен MongoDB 5.2+).
// Навички без жодного активного запису в результат не потрапляють, їхні старі лічильники лишаються.
database.userSkillIndex.aggregate([
    { $match: { status: 'Active' } },
    { $group: {
        _id: '$skillId',
        skillName: { $first: '$skillName' },
        offersCount: { $sum: { $cond: [{ $eq: ['$type', 'Offer'] }, 1, 0] } },
        wantsCount: { $sum: { $cond: [{ $eq: ['$type', 'Want'] }, 1, 0] } },
        langs: { $push: '$languages.code' }
    } },
    { $addFields: { langs: { $reduce: { input: '$langs', initialValue: [], in: { $concatArrays: ['$$value', '$$this'] } } } } },
    { $addFields: { topLanguages: { $slice: [{ $sortArray: {
        input: { $map: {
            input: { $setUnion: ['$langs', []] },
            as: 'code',
            in: { code: '$$code', count: { $size: { $filter: { input: '$langs', as: 'c', cond: { $eq: ['$$c', '$$code'] } } } } }
        } },
        sortBy: { count: -1, code: 1 }
    } }, 3] } } },
    { $project: { skillName: 1, offersCount: 1, wantsCount: 1, topLanguages: 1, updatedAt: '$$NOW' } },
    { $merge: { into: 'skillStats', on: '_id', whenMatched: 'replace', whenNotMatched: 'insert' } }
]);
show('8. skillStats після перерахунку', database.skillStats.find().sort({ _id: 1 }).toArray());

// 9. Розрив між попитом і пропозицією: навички, яких хочуть більше, ніж пропонують.
show('9. Дефіцитні навички',
    database.skillStats.find({ $expr: { $gt: ['$wantsCount', '$offersCount'] } },
        { skillName: 1, offersCount: 1, wantsCount: 1 }).toArray());

// 10. Валідація $jsonSchema: Want із полем experienceYears відхиляється (очікується помилка 121).
print('\n=== 10. Перевірка валідації ===');
try {
    database.userSkillIndex.insertOne({
        userSkillId: 999, userId: u1, skillId: 1, skillName: 'Програмування C#', type: 'Want', level: 2,
        status: 'Active', languages: [], availability: [], experienceYears: 3, updatedAt: new Date()
    });
    print('ПОМИЛКА: документ вставлено.');
} catch (e) {
    print('OK, документ відхилено валідацією: ' + (e.codeName || e.message));
}
