-- Phase 8 Schema Additions: Realtime Chat

CREATE TABLE IF NOT EXISTS chat_rooms (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    type VARCHAR(50) NOT NULL, -- e.g. 'ClassRoom', 'Assignment', 'Direct'
    reference_id UUID, -- class_room_id or assignment_id based on type
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Index for quickly finding chat room by reference (e.g. finding the chat room for an assignment)
CREATE INDEX IF NOT EXISTS idx_chat_rooms_reference ON chat_rooms(reference_id, type);

CREATE TABLE IF NOT EXISTS chat_participants (
    chat_room_id UUID NOT NULL REFERENCES chat_rooms(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    joined_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_read_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (chat_room_id, user_id)
);

CREATE TABLE IF NOT EXISTS chat_messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    chat_room_id UUID NOT NULL REFERENCES chat_rooms(id) ON DELETE CASCADE,
    sender_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    content TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Index for fetching messages for a chat room efficiently
CREATE INDEX IF NOT EXISTS idx_chat_messages_room ON chat_messages(chat_room_id, created_at DESC);
