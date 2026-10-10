const database = db.getSiblingDB('QPQ_Messages');

const guidPattern = '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$';

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

ensureCollection('conversations', {
    $jsonSchema: {
        bsonType: 'object',
        required: ['swapId', 'swapStatus', 'participants', 'lastMessage', 'createdAt'],
        additionalProperties: false,
        properties: {
            _id: { bsonType: 'objectId' },
            swapId: { bsonType: 'number', minimum: 1 },
            swapStatus: { enum: ['Accepted', 'Completed', 'Cancelled'] },
            participants: {
                bsonType: 'array',
                minItems: 2,
                maxItems: 2,
                items: {
                    bsonType: 'object',
                    required: ['userId', 'role'],
                    additionalProperties: false,
                    properties: {
                        userId: { bsonType: 'string', pattern: guidPattern },
                        role: { enum: ['Initiator', 'Partner'] },
                        lastReadAt: { bsonType: ['date', 'null'] }
                    }
                }
            },
            lastMessage: {
                bsonType: ['object', 'null'],
                required: ['messageId', 'senderId', 'preview', 'sentAt'],
                additionalProperties: false,
                properties: {
                    messageId: { bsonType: 'objectId' },
                    senderId: { bsonType: 'string', pattern: guidPattern },
                    preview: { bsonType: 'string', maxLength: 200 },
                    sentAt: { bsonType: 'date' }
                }
            },
            createdAt: { bsonType: 'date' },
            updatedAt: { bsonType: ['date', 'null'] }
        }
    }
});

function messageVariant(type, extraRequired, extraProperties) {
    return {
        bsonType: 'object',
        required: ['conversationId', 'senderId', 'type', 'createdAt', 'isDeleted'].concat(extraRequired),
        additionalProperties: false,
        properties: Object.assign({
            _id: { bsonType: 'objectId' },
            conversationId: { bsonType: 'objectId' },
            senderId: { bsonType: 'string', pattern: guidPattern },
            type: { enum: [type] },
            createdAt: { bsonType: 'date' },
            isDeleted: { bsonType: 'bool' },
            deletedAt: { bsonType: ['date', 'null'] }
        }, extraProperties)
    };
}

ensureCollection('messages', {
    $jsonSchema: {
        bsonType: 'object',
        oneOf: [
            messageVariant('text', ['text'], {
                text: { bsonType: 'string', minLength: 1, maxLength: 4000 },
                editedAt: { bsonType: ['date', 'null'] }
            }),
            messageVariant('file', ['attachments'], {
                caption: { bsonType: 'string', maxLength: 1000 },
                attachments: {
                    bsonType: 'array',
                    minItems: 1,
                    maxItems: 10,
                    items: {
                        bsonType: 'object',
                        required: ['attachmentId', 'fileName'],
                        additionalProperties: false,
                        properties: {
                            attachmentId: { bsonType: 'objectId' },
                            fileName: { bsonType: 'string', minLength: 1, maxLength: 255 }
                        }
                    }
                }
            }),
            messageVariant('system', ['event', 'details'], {
                event: { enum: ['SwapAccepted', 'SwapCompleted', 'SwapCancelled'] },
                details: {
                    bsonType: 'object',
                    required: ['swapId'],
                    properties: {
                        swapId: { bsonType: 'number', minimum: 1 }
                    }
                }
            })
        ]
    }
});

ensureCollection('attachments', {
    $jsonSchema: {
        bsonType: 'object',
        required: ['conversationId', 'uploadedBy', 'fileName', 'url', 'contentType', 'sizeBytes', 'uploadedAt'],
        additionalProperties: false,
        properties: {
            _id: { bsonType: 'objectId' },
            conversationId: { bsonType: 'objectId' },
            uploadedBy: { bsonType: 'string', pattern: guidPattern },
            fileName: { bsonType: 'string', minLength: 1, maxLength: 255 },
            url: { bsonType: 'string', minLength: 1, maxLength: 500 },
            contentType: { bsonType: 'string', maxLength: 100 },
            sizeBytes: { bsonType: 'number', minimum: 0 },
            uploadedAt: { bsonType: 'date' }
        }
    }
});

database.conversations.createIndex({ swapId: 1 }, { name: 'ux_conversations_swap', unique: true });
database.conversations.createIndex({ 'participants.userId': 1 }, { name: 'ix_conversations_participant' });
database.messages.createIndex(
    { conversationId: 1, createdAt: 1 },
    { name: 'ix_messages_conversation_createdAt', partialFilterExpression: { isDeleted: false } }
);
database.messages.createIndex({ senderId: 1, createdAt: -1 }, { name: 'ix_messages_sender_createdAt' });
database.attachments.createIndex({ conversationId: 1, uploadedAt: -1 }, { name: 'ix_attachments_conversation_uploadedAt' });