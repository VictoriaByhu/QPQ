const database = db.getSiblingDB('QPQ_Matching');

const guidPattern = '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$';
const timePattern = '^([01]\\d|2[0-3]):[0-5]\\d$';

function ensureCollection(name, validator) {
    if (!database.getCollectionNames().includes(name)) {
        database.createCollection(name, {
            validator: validator,
            validationLevel: 'strict',
            validationAction: 'error'
        });
    } else {
        database.runCommand({
            collMod: name,
            validator: validator,
            validationLevel: 'strict',
            validationAction: 'error'
        });
    }
}

// userSkillIndex: локальна копія UserSkills для підбору пар (read-модель).
// Документи Offer і Want мають різні набори полів: лише Offer може мати experienceYears.
function indexVariant(type, extraProperties) {
    return {
        bsonType: 'object',
        required: ['userSkillId', 'userId', 'skillId', 'skillName', 'type', 'level', 'status',
                   'languages', 'availability', 'updatedAt'],
        additionalProperties: false,
        properties: Object.assign({
            _id: { bsonType: 'objectId' },
            userSkillId: { bsonType: 'number', minimum: 1 },
            userId: { bsonType: 'string', pattern: guidPattern },
            skillId: { bsonType: 'number', minimum: 1 },
            skillName: { bsonType: 'string', minLength: 1, maxLength: 100 },
            type: { enum: [type] },
            level: { bsonType: 'number', minimum: 1, maximum: 5 },
            status: { enum: ['Active', 'Paused', 'Archived'] },
            languages: {
                bsonType: 'array',
                maxItems: 10,
                items: {
                    bsonType: 'object',
                    required: ['code', 'proficiency'],
                    additionalProperties: false,
                    properties: {
                        code: { bsonType: 'string', minLength: 2, maxLength: 10 },
                        proficiency: { bsonType: 'number', minimum: 1, maximum: 5 }
                    }
                }
            },
            availability: {
                bsonType: 'array',
                maxItems: 21,
                items: {
                    bsonType: 'object',
                    required: ['dayOfWeek', 'start', 'end'],
                    additionalProperties: false,
                    properties: {
                        dayOfWeek: { bsonType: 'number', minimum: 1, maximum: 7 },
                        start: { bsonType: 'string', pattern: timePattern },
                        end: { bsonType: 'string', pattern: timePattern }
                    }
                }
            },
            updatedAt: { bsonType: 'date' }
        }, extraProperties)
    };
}

ensureCollection('userSkillIndex', {
    $jsonSchema: {
        bsonType: 'object',
        oneOf: [
            indexVariant('Offer', { experienceYears: { bsonType: 'number', minimum: 0, maximum: 80 } }),
            indexVariant('Want', {})
        ]
    }
});

// matches: знайдена пара. oneWay містить одну пропозицію, mutual ще й зустрічну (counterOffer).
// Посилання на userSkillIndex (indexRef) поєднані з вбудованим знімком skillName: гібридний підхід.
const offerShape = {
    bsonType: 'object',
    required: ['fromUserId', 'toUserId', 'skillId', 'skillName', 'indexRef'],
    additionalProperties: false,
    properties: {
        fromUserId: { bsonType: 'string', pattern: guidPattern },
        toUserId: { bsonType: 'string', pattern: guidPattern },
        skillId: { bsonType: 'number', minimum: 1 },
        skillName: { bsonType: 'string', minLength: 1, maxLength: 100 },
        indexRef: { bsonType: 'objectId' }
    }
};

function matchVariant(kind, extraRequired, extraProperties) {
    return {
        bsonType: 'object',
        required: ['kind', 'status', 'score', 'userIds', 'offer', 'commonLanguages', 'createdAt', 'expiresAt']
            .concat(extraRequired),
        additionalProperties: false,
        properties: Object.assign({
            _id: { bsonType: 'objectId' },
            kind: { enum: [kind] },
            status: { enum: ['Suggested', 'Viewed', 'Dismissed'] },
            score: { bsonType: 'number', minimum: 0, maximum: 100 },
            userIds: {
                bsonType: 'array',
                minItems: 2,
                maxItems: 2,
                items: { bsonType: 'string', pattern: guidPattern }
            },
            offer: offerShape,
            commonLanguages: {
                bsonType: 'array',
                maxItems: 10,
                items: { bsonType: 'string', minLength: 2, maxLength: 10 }
            },
            createdAt: { bsonType: 'date' },
            expiresAt: { bsonType: 'date' }
        }, extraProperties)
    };
}

ensureCollection('matches', {
    $jsonSchema: {
        bsonType: 'object',
        oneOf: [
            matchVariant('oneWay', [], {}),
            matchVariant('mutual', ['counterOffer'], { counterOffer: offerShape })
        ]
    }
});

// skillStats: похідна колекція (лічильники попиту й пропозиції), _id = skillId.
ensureCollection('skillStats', {
    $jsonSchema: {
        bsonType: 'object',
        required: ['_id', 'skillName', 'offersCount', 'wantsCount', 'topLanguages', 'updatedAt'],
        additionalProperties: false,
        properties: {
            _id: { bsonType: 'number', minimum: 1 },
            skillName: { bsonType: 'string', minLength: 1, maxLength: 100 },
            offersCount: { bsonType: 'number', minimum: 0 },
            wantsCount: { bsonType: 'number', minimum: 0 },
            topLanguages: {
                bsonType: 'array',
                maxItems: 3,
                items: {
                    bsonType: 'object',
                    required: ['code', 'count'],
                    additionalProperties: false,
                    properties: {
                        code: { bsonType: 'string', minLength: 2, maxLength: 10 },
                        count: { bsonType: 'number', minimum: 1 }
                    }
                }
            },
            updatedAt: { bsonType: 'date' }
        }
    }
});

database.userSkillIndex.createIndex({ userSkillId: 1 }, { name: 'ux_userSkillIndex_userSkillId', unique: true });
database.userSkillIndex.createIndex(
    { skillId: 1, type: 1 },
    { name: 'ix_userSkillIndex_skill_type_active', partialFilterExpression: { status: 'Active' } }
);
database.userSkillIndex.createIndex({ userId: 1, status: 1 }, { name: 'ix_userSkillIndex_user_status' });
database.matches.createIndex(
    { 'offer.fromUserId': 1, 'offer.toUserId': 1, 'offer.skillId': 1 },
    { name: 'ux_matches_offer', unique: true }
);
database.matches.createIndex({ userIds: 1, status: 1, score: -1 }, { name: 'ix_matches_user_status_score' });
database.matches.createIndex({ expiresAt: 1 }, { name: 'ttl_matches_expiresAt', expireAfterSeconds: 0 });
