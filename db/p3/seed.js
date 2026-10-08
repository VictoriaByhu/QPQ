const database = db.getSiblingDB('QPQ_Messages');

const u1 = '3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01';
const u2 = '7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02';
const u3 = 'b19e6a33-2c5d-47f8-8d14-9a0b3e6f5c03';

const conv1 = ObjectId('670000000000000000000c01');
const conv2 = ObjectId('670000000000000000000c02');
const file1 = ObjectId('670000000000000000000b01');
const file2 = ObjectId('670000000000000000000b02');

function upsertAll(collection, documents) {
    collection.bulkWrite(
        documents.map(function (d) {
            return { updateOne: { filter: { _id: d._id }, update: { $setOnInsert: d }, upsert: true } };
        })
    );
}

upsertAll(database.conversations, [
    {
        _id: conv1,
        swapId: 2,
        swapStatus: 'Accepted',
        participants: [
            { userId: u2, role: 'Initiator', lastReadAt: new Date('2026-10-02T10:35:00Z') },
            { userId: u3, role: 'Partner', lastReadAt: new Date('2026-10-02T10:40:00Z') }
        ],
        lastMessage: {
            messageId: ObjectId('670000000000000000000a03'),
            senderId: u2,
            preview: 'Чудово! Ось план першого заняття з англійської.',
            sentAt: new Date('2026-10-02T10:30:00Z')
        },
        createdAt: new Date('2026-10-02T09:00:00Z'),
        updatedAt: null
    },
    {
        _id: conv2,
        swapId: 3,
        swapStatus: 'Completed',
        participants: [
            { userId: u3, role: 'Initiator', lastReadAt: new Date('2026-10-05T21:35:00Z') },
            { userId: u1, role: 'Partner', lastReadAt: null }
        ],
        lastMessage: {
            messageId: ObjectId('670000000000000000000a06'),
            senderId: u3,
            preview: 'Дякую за обмін, все було корисно!',
            sentAt: new Date('2026-10-05T21:30:00Z')
        },
        createdAt: new Date('2026-09-28T10:00:00Z'),
        updatedAt: new Date('2026-10-05T21:00:00Z')
    }
]);

upsertAll(database.attachments, [
    {
        _id: file1,
        conversationId: conv1,
        uploadedBy: u2,
        fileName: 'english-lesson-plan.pdf',
        url: 'https://files.example.com/swaps/2/english-lesson-plan.pdf',
        contentType: 'application/pdf',
        sizeBytes: 184320,
        uploadedAt: new Date('2026-10-02T10:29:00Z')
    },
    {
        _id: file2,
        conversationId: conv2,
        uploadedBy: u3,
        fileName: 'mockup.png',
        url: 'https://files.example.com/swaps/3/mockup.png',
        contentType: 'image/png',
        sizeBytes: 524288,
        uploadedAt: new Date('2026-09-28T10:59:00Z')
    }
]);

upsertAll(database.messages, [
    {
        _id: ObjectId('670000000000000000000a07'),
        conversationId: conv1,
        senderId: u3,
        type: 'system',
        event: 'SwapAccepted',
        details: { swapId: 2 },
        createdAt: new Date('2026-10-02T09:00:00Z'),
        isDeleted: false,
        deletedAt: null
    },
    {
        _id: ObjectId('670000000000000000000a01'),
        conversationId: conv1,
        senderId: u2,
        type: 'text',
        text: 'Привіт! Коли тобі зручно зустрітися для обміну?',
        editedAt: null,
        createdAt: new Date('2026-10-02T10:00:00Z'),
        isDeleted: false,
        deletedAt: null
    },
    {
        _id: ObjectId('670000000000000000000a02'),
        conversationId: conv1,
        senderId: u3,
        type: 'text',
        text: 'Привіт! Мені підходить четвер ввечері, 22 жовтня.',
        editedAt: null,
        createdAt: new Date('2026-10-02T10:12:00Z'),
        isDeleted: false,
        deletedAt: null
    },
    {
        _id: ObjectId('670000000000000000000a03'),
        conversationId: conv1,
        senderId: u2,
        type: 'file',
        caption: 'Чудово! Ось план першого заняття з англійської.',
        attachments: [{ attachmentId: file1, fileName: 'english-lesson-plan.pdf' }],
        createdAt: new Date('2026-10-02T10:30:00Z'),
        isDeleted: false,
        deletedAt: null
    },
    {
        _id: ObjectId('670000000000000000000a08'),
        conversationId: conv2,
        senderId: u1,
        type: 'system',
        event: 'SwapAccepted',
        details: { swapId: 3 },
        createdAt: new Date('2026-09-28T10:00:00Z'),
        isDeleted: false,
        deletedAt: null
    },
    {
        _id: ObjectId('670000000000000000000a04'),
        conversationId: conv2,
        senderId: u3,
        type: 'file',
        caption: 'Надсилаю макет у Figma, подивись, будь ласка.',
        attachments: [{ attachmentId: file2, fileName: 'mockup.png' }],
        createdAt: new Date('2026-09-28T11:00:00Z'),
        isDeleted: false,
        deletedAt: null
    },
    {
        _id: ObjectId('670000000000000000000a05'),
        conversationId: conv2,
        senderId: u1,
        type: 'text',
        text: 'Дякую, виглядає чудово! Готовий до консультації з C#.',
        editedAt: new Date('2026-09-28T11:25:00Z'),
        createdAt: new Date('2026-09-28T11:20:00Z'),
        isDeleted: false,
        deletedAt: null
    },
    {
        _id: ObjectId('670000000000000000000a09'),
        conversationId: conv2,
        senderId: u3,
        type: 'system',
        event: 'SwapCompleted',
        details: { swapId: 3 },
        createdAt: new Date('2026-10-05T21:00:00Z'),
        isDeleted: false,
        deletedAt: null
    },
    {
        _id: ObjectId('670000000000000000000a06'),
        conversationId: conv2,
        senderId: u3,
        type: 'text',
        text: 'Дякую за обмін, все було корисно!',
        editedAt: null,
        createdAt: new Date('2026-10-05T21:30:00Z'),
        isDeleted: false,
        deletedAt: null
    }
]);