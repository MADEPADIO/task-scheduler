# Task Scheduler

Internal scheduling and attendance tool for a church videography unit.

## 🚨 Admin Password Recovery (Emergency Only)

If you are the ONLY Admin and you forget your password, you cannot log in to click the reset button. You must manually update the database.

1. Go to https://bcrypt-generator.com/
2. Type your new password, set rounds to 11, and click "Generate".
3. Copy the hash (e.g., `$2a$11$...`).
4. Open your PostgreSQL database.
5. Run this SQL command (replace the email and the hash with your actual details):

UPDATE "Users" 
SET "PasswordHash" = 'PASTE_YOUR_HASH_HERE', 
    "MustChangePassword" = false 
WHERE "Email" = 'your-admin-email@example.com';

6. You can now log in with your new password.
