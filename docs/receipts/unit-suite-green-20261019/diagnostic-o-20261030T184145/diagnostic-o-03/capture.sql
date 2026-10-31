SET NOCOUNT ON;
WAITFOR DELAY '00:00:01';
SELECT CAST(t.target_data AS nvarchar(max))
FROM sys.dm_xe_session_targets AS t
JOIN sys.dm_xe_sessions AS s ON t.event_session_address=s.address
WHERE s.name=N'qbrain_migration_diag_0bd81d670bd9407ca9db1b85b244e735' AND t.target_name=N'ring_buffer';
ALTER EVENT SESSION [qbrain_migration_diag_0bd81d670bd9407ca9db1b85b244e735] ON SERVER STATE=STOP;
ALTER EVENT SESSION [qbrain_migration_diag_0bd81d670bd9407ca9db1b85b244e735] ON SERVER STATE=START;
