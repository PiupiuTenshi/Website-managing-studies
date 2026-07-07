-- Schema v1 - Remote Assignment Platform
-- PostgreSQL / Supabase compatible draft

create extension if not exists "pgcrypto";

create table roles (
    id uuid primary key default gen_random_uuid(),
    name text not null unique,
    description text,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table permissions (
    id uuid primary key default gen_random_uuid(),
    name text not null unique,
    description text,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table role_permissions (
    role_id uuid not null references roles(id),
    permission_id uuid not null references permissions(id),
    created_at timestamptz not null default now(),
    primary key (role_id, permission_id)
);

create table users (
    id uuid primary key default gen_random_uuid(),
    username text unique,
    email text not null unique,
    full_name text not null,
    password_hash text not null,
    status text not null default 'Active', -- Active, Locked, Disabled
    locked_at timestamptz,
    locked_by uuid,
    lock_reason text,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    deleted_at timestamptz
);

create table user_roles (
    user_id uuid not null references users(id),
    role_id uuid not null references roles(id),
    created_at timestamptz not null default now(),
    primary key (user_id, role_id)
);

create table audit_logs (
    id uuid primary key default gen_random_uuid(),
    actor_user_id uuid references users(id),
    action text not null,
    entity_type text,
    entity_id uuid,
    ip_address inet,
    user_agent text,
    metadata jsonb not null default '{}'::jsonb,
    created_at timestamptz not null default now()
);

create table user_sessions (
    id uuid primary key default gen_random_uuid(),
    user_id uuid not null references users(id),
    role_name text not null,
    status text not null default 'Active', -- Active, Revoked, Expired
    last_used_at timestamptz not null default now(),
    expires_at timestamptz not null,
    access_token_jti uuid,
    revoked_at timestamptz,
    revoked_reason text,
    created_at timestamptz not null default now()
);

create table refresh_tokens (
    id uuid primary key default gen_random_uuid(),
    user_id uuid not null references users(id),
    session_id uuid not null references user_sessions(id),
    token_hash text not null unique,
    expires_at timestamptz not null,
    used_at timestamptz,
    revoked_at timestamptz,
    replaced_by_token_id uuid,
    created_at timestamptz not null default now()
);

create table grade_levels (
    id uuid primary key default gen_random_uuid(),
    name text not null unique, -- Grade 6 ... Grade 12
    sort_order int not null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table subjects (
    id uuid primary key default gen_random_uuid(),
    name text not null,
    code text not null unique,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table class_rooms (
    id uuid primary key default gen_random_uuid(),
    name text not null,
    grade_level_id uuid not null references grade_levels(id),
    manager_id uuid references users(id),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    deleted_at timestamptz
);

create table class_enrollments (
    id uuid primary key default gen_random_uuid(),
    class_room_id uuid not null references class_rooms(id),
    student_id uuid not null references users(id),
    status text not null default 'Active',
    created_at timestamptz not null default now(),
    deleted_at timestamptz,
    unique(class_room_id, student_id)
);

create table parent_student_links (
    id uuid primary key default gen_random_uuid(),
    parent_id uuid not null references users(id),
    student_id uuid not null references users(id),
    relationship text,
    status text not null default 'Active',
    created_at timestamptz not null default now(),
    deleted_at timestamptz,
    unique(parent_id, student_id)
);

create table assignments (
    id uuid primary key default gen_random_uuid(),
    title text not null,
    description text,
    content_json jsonb not null default '{}'::jsonb,
    subject_id uuid references subjects(id),
    grade_level_id uuid references grade_levels(id),
    status text not null default 'Draft',
    publish_at timestamptz,
    due_at timestamptz,
    allow_late_submission boolean not null default false,
    created_by uuid not null references users(id),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    deleted_at timestamptz
);

create table assignment_targets (
    id uuid primary key default gen_random_uuid(),
    assignment_id uuid not null references assignments(id),
    class_room_id uuid references class_rooms(id),
    student_id uuid references users(id),
    created_at timestamptz not null default now(),
    check (class_room_id is not null or student_id is not null)
);

create table assignment_files (
    id uuid primary key default gen_random_uuid(),
    assignment_id uuid not null references assignments(id),
    file_name text not null,
    storage_key text not null,
    content_type text,
    size_bytes bigint,
    created_at timestamptz not null default now()
);

create table assignment_reminder_rules (
    id uuid primary key default gen_random_uuid(),
    assignment_id uuid not null references assignments(id),
    reminder_type text not null, -- BeforeDue, AfterDue, ParentAlert
    offset_minutes int not null,
    channel text not null, -- InApp, Email, Both
    enabled boolean not null default true,
    created_at timestamptz not null default now()
);

create table submissions (
    id uuid primary key default gen_random_uuid(),
    assignment_id uuid not null references assignments(id),
    student_id uuid not null references users(id),
    status text not null default 'Draft',
    answer_json jsonb not null default '{}'::jsonb,
    started_at timestamptz,
    submitted_at timestamptz,
    is_late boolean not null default false,
    current_score numeric(5,2),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    deleted_at timestamptz,
    unique(assignment_id, student_id)
);

create table submission_files (
    id uuid primary key default gen_random_uuid(),
    submission_id uuid not null references submissions(id),
    file_name text not null,
    storage_key text not null,
    content_type text,
    size_bytes bigint,
    created_at timestamptz not null default now()
);

create table submission_comments (
    id uuid primary key default gen_random_uuid(),
    submission_id uuid not null references submissions(id),
    author_id uuid not null references users(id),
    comment_text text not null,
    visibility text not null default 'StudentAndTeacher',
    created_at timestamptz not null default now(),
    deleted_at timestamptz
);

create table grades (
    id uuid primary key default gen_random_uuid(),
    submission_id uuid not null references submissions(id),
    score numeric(5,2) not null check(score >= 0 and score <= 100),
    feedback text,
    graded_by uuid not null references users(id),
    graded_at timestamptz not null default now(),
    created_at timestamptz not null default now()
);

create table teacher_solutions (
    id uuid primary key default gen_random_uuid(),
    assignment_id uuid not null references assignments(id),
    content_json jsonb not null default '{}'::jsonb,
    created_by uuid not null references users(id),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    deleted_at timestamptz
);

create table resource_collections (
    id uuid primary key default gen_random_uuid(),
    title text not null,
    grade_level_id uuid references grade_levels(id),
    subject_id uuid references subjects(id),
    source_name text,
    license_note text not null,
    created_by uuid references users(id),
    created_at timestamptz not null default now(),
    deleted_at timestamptz
);

create table learning_resources (
    id uuid primary key default gen_random_uuid(),
    collection_id uuid references resource_collections(id),
    title text not null,
    language text not null default 'vi',
    resource_type text not null, -- pdf, docx, video, link, page
    storage_key text,
    source_url text,
    allow_download boolean not null default false,
    created_at timestamptz not null default now(),
    deleted_at timestamptz
);

create table resource_pages (
    id uuid primary key default gen_random_uuid(),
    resource_id uuid not null references learning_resources(id),
    page_number int not null,
    storage_key text,
    text_content text,
    created_at timestamptz not null default now(),
    unique(resource_id, page_number)
);

create table resource_access_logs (
    id uuid primary key default gen_random_uuid(),
    resource_id uuid not null references learning_resources(id),
    page_id uuid references resource_pages(id),
    user_id uuid not null references users(id),
    action text not null, -- ViewPage, Download
    created_at timestamptz not null default now()
);

create table chat_rooms (
    id uuid primary key default gen_random_uuid(),
    room_type text not null, -- Class, Assignment, Submission, ParentTeacher
    assignment_id uuid references assignments(id),
    class_room_id uuid references class_rooms(id),
    submission_id uuid references submissions(id),
    created_at timestamptz not null default now()
);

create table chat_messages (
    id uuid primary key default gen_random_uuid(),
    room_id uuid not null references chat_rooms(id),
    sender_id uuid not null references users(id),
    message_type text not null default 'Text',
    content text,
    file_storage_key text,
    created_at timestamptz not null default now(),
    edited_at timestamptz,
    deleted_at timestamptz
);

create table notifications (
    id uuid primary key default gen_random_uuid(),
    user_id uuid not null references users(id),
    notification_type text not null,
    title text not null,
    body text not null,
    data_json jsonb not null default '{}'::jsonb,
    read_at timestamptz,
    created_at timestamptz not null default now()
);

create table email_outbox (
    id uuid primary key default gen_random_uuid(),
    to_email text not null,
    subject text not null,
    body_html text not null,
    status text not null default 'Pending', -- Pending, Sent, Failed
    retry_count int not null default 0,
    last_error text,
    scheduled_at timestamptz not null default now(),
    sent_at timestamptz,
    created_at timestamptz not null default now()
);

insert into roles (name, description)
values
    ('Admin', 'System administrator'),
    ('Manager', 'Class and assignment manager'),
    ('Student', 'Student account'),
    ('Parent', 'Parent account')
on conflict (name) do nothing;

insert into permissions (name, description)
values
    ('accounts.manage', 'Manage accounts'),
    ('accounts.lock', 'Lock and unlock accounts'),
    ('assignments.manage', 'Create and manage assignments'),
    ('submissions.create', 'Create assignment submissions'),
    ('grades.view', 'View allowed grades'),
    ('reports.child.view', 'View linked child reports')
on conflict (name) do nothing;

insert into role_permissions (role_id, permission_id)
select r.id, p.id
from roles r
cross join permissions p
where r.name = 'Admin'
on conflict (role_id, permission_id) do nothing;

insert into role_permissions (role_id, permission_id)
select r.id, p.id
from roles r
join permissions p on p.name in ('assignments.manage', 'grades.view')
where r.name = 'Manager'
on conflict (role_id, permission_id) do nothing;

insert into role_permissions (role_id, permission_id)
select r.id, p.id
from roles r
join permissions p on p.name in ('submissions.create', 'grades.view')
where r.name = 'Student'
on conflict (role_id, permission_id) do nothing;

insert into role_permissions (role_id, permission_id)
select r.id, p.id
from roles r
join permissions p on p.name in ('reports.child.view', 'grades.view')
where r.name = 'Parent'
on conflict (role_id, permission_id) do nothing;

create index idx_assignments_status_due_at on assignments(status, due_at);
create index idx_assignment_targets_class on assignment_targets(class_room_id);
create index idx_assignment_targets_student on assignment_targets(student_id);
create index idx_submissions_assignment_student on submissions(assignment_id, student_id);
create index idx_submissions_status on submissions(status);
create index idx_email_outbox_status_scheduled on email_outbox(status, scheduled_at);
create index idx_notifications_user_read on notifications(user_id, read_at);
create index idx_resource_pages_resource_number on resource_pages(resource_id, page_number);
create index idx_audit_logs_actor_created on audit_logs(actor_user_id, created_at);
create index idx_audit_logs_entity on audit_logs(entity_type, entity_id);
create index idx_users_username on users(username);
create index idx_user_sessions_user_status on user_sessions(user_id, status);
create index idx_refresh_tokens_session on refresh_tokens(session_id);
