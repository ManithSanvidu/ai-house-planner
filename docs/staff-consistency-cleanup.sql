-- ============================================================
-- Staff Consistency Cleanup Script
-- Run in Supabase SQL Editor (Dashboard → SQL Editor)
-- ============================================================

-- ── Step 1: View all orphan public.Users rows (no auth.users match) ──────────
-- These have a SupabaseUid that does not exist in auth.users.
SELECT
    u."Id",
    u."Email",
    u."SupabaseUid",
    r."Name" AS "Role"
FROM "Users" u
JOIN "Roles" r ON u."RoleId" = r."Id"
WHERE u."SupabaseUid" IS NULL
   OR NOT EXISTS (
       SELECT 1 FROM auth.users a WHERE a.id::text = u."SupabaseUid"
   );

-- ── Step 2: Delete the three known orphan public.Users rows ──────────────────
-- These were created by the old stub before the real Supabase call was implemented.
DELETE FROM "Users"
WHERE "Email" IN (
    'dewmi2@email.com',
    'dewmi22@email.com',
    'architecture@email.com'
)
AND (
    "SupabaseUid" IS NULL
    OR NOT EXISTS (
        SELECT 1 FROM auth.users a WHERE a.id::text = "SupabaseUid"
    )
);

-- ── Step 3: View orphan auth.users rows (no public.Users match) ──────────────
-- These auth users were created but their matching public.Users row was never saved.
SELECT
    a.id AS auth_id,
    a.email,
    a.created_at
FROM auth.users a
WHERE NOT EXISTS (
    SELECT 1 FROM "Users" u WHERE u."SupabaseUid" = a.id::text
)
ORDER BY a.created_at DESC;

-- ── Step 4: After creating a new test staff member, run this verification ─────
-- Replace <new-test-email> with the email you used.
SELECT
    a.email,
    a.id                AS auth_id,
    u."SupabaseUid"     AS app_uid,
    r."Name"            AS role,
    (a.id::text = u."SupabaseUid") AS uid_matches
FROM auth.users a
JOIN "Users" u   ON lower(a.email) = lower(u."Email")
JOIN "Roles" r   ON u."RoleId"     = r."Id"
WHERE lower(a.email) = lower('<new-test-email>');

-- Expected result:
--   auth_id  = app_uid       → true
--   uid_matches              → true
--   role                     → Constructor (or Architect)
